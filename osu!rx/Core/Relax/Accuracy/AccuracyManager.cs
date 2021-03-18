using osu;
using osu.Enums;
using osu.Memory.Objects.Player.Beatmaps;
using osu.Memory.Objects.Player.Beatmaps.Objects;
using osu_rx.Configuration;
using osu_rx.Helpers;
using SimpleDependencyInjection;
using System;
using System.Numerics;

namespace osu_rx.Core.Relax.Accuracy
{
    public class AccuracyManager
    {
        private OsuManager osuManager;
        private ConfigManager configManager;

        private OsuBeatmap beatmap;

        private int hitWindow50;
        private int hitWindow100;
        private int hitWindow300;

        private float audioRate;

        //hittimings
        private int minOffset;
        private int maxOffset;
        private int minAlternateOffset;
        private int maxAlternateOffset;

        //hitscan
        private float hitObjectRadius;
        private float missRadius;

        private bool canMiss;
        private int lastHitScanIndex;
        private Vector2? lastOnNotePosition;

        private Random random = new Random();

        public AccuracyManager()
        {
            osuManager = DependencyContainer.Get<OsuManager>();
            configManager = DependencyContainer.Get<ConfigManager>();
        }

        public void Reset(OsuBeatmap beatmap)
        {
            this.beatmap = beatmap;

            hitWindow50 = osuManager.HitWindow50(beatmap.OverallDifficulty);
            hitWindow100 = osuManager.HitWindow100(beatmap.OverallDifficulty);
            hitWindow300 = osuManager.HitWindow300(beatmap.OverallDifficulty);

            var mods = osuManager.Player.HitObjectManager.CurrentMods;
            audioRate = mods.HasFlag(Mods.HalfTime) ? 0.75f : mods.HasFlag(Mods.DoubleTime) ? 1.5f : 1;

            minOffset = calculateTimingOffset(configManager.HitTimingsMinOffset);
            maxOffset = calculateTimingOffset(configManager.HitTimingsMaxOffset);
            minAlternateOffset = calculateTimingOffset(configManager.HitTimingsAlternateMinOffset);
            maxAlternateOffset = calculateTimingOffset(configManager.HitTimingsAlternateMaxOffset);

            hitObjectRadius = osuManager.HitObjectRadius(beatmap.CircleSize);
            missRadius = configManager.HitScanMissRadius;

            canMiss = false;
            lastHitScanIndex = -1;
            lastOnNotePosition = null;
        }

        public HitObjectTimings GetHitObjectTimings(int index, bool alternating, bool doubleDelay)
        {
            var result = new HitObjectTimings();

            int startOffsetMin = (int)((alternating ? minAlternateOffset : minOffset) * (doubleDelay ? configManager.HitTimingsDoubleDelayFactor : 1f));
            int startOffsetMax = (int)((alternating ? maxAlternateOffset : maxOffset) * (doubleDelay ? configManager.HitTimingsDoubleDelayFactor : 1f));

            result.StartOffset = MathHelper.Clamp(random.Next(startOffsetMin, startOffsetMax), -hitWindow50, hitWindow50);

            if (beatmap.HitObjects[index] is OsuSlider)
            {
                int sliderDuration = beatmap.HitObjects[index].EndTime - beatmap.HitObjects[index].StartTime;
                int maxHoldTime = (int)(configManager.HitTimingsMaxSliderHoldTime * audioRate);
                int holdTime = random.Next(configManager.HitTimingsMinSliderHoldTime, maxHoldTime);

                result.HoldTime = MathHelper.Clamp(holdTime, sliderDuration >= 72 ? -26 : sliderDuration / 2 - 10, maxHoldTime);
            }
            else
            {
                int minHoldTime = (int)(configManager.HitTimingsMinHoldTime * audioRate);
                int maxHoldTime = (int)(configManager.HitTimingsMaxHoldTime * audioRate);
                int holdTime = random.Next(minHoldTime, maxHoldTime);

                result.HoldTime = MathHelper.Clamp(holdTime, 0, maxHoldTime);
            }

            return result;
        }

        private int calculateTimingOffset(int percentage)
        {
            float multiplier = Math.Abs(percentage) / 100f;

            int hitWindowStartTime = multiplier <= 1 ? 0 : multiplier <= 2 ? hitWindow300 + 1 : hitWindow100 + 1;
            int hitWindowEndTime = multiplier <= 1 ? hitWindow300 : multiplier <= 2 ? hitWindow100 : hitWindow50;
            int hitWindowTime = hitWindowEndTime - hitWindowStartTime;

            if (multiplier != 0 && multiplier % 1 == 0) //kinda dirty
                multiplier = 1;
            else
                multiplier %= 1;

            return (int)(hitWindowStartTime + (hitWindowTime * multiplier)) * (percentage < 0 ? -1 : 1);
        }

        public HitScanResult GetHitScanResult(int index)
        {
            var hitObject = beatmap.HitObjects[index];
            var nextHitObject = index + 1 < beatmap.HitObjects.Count ? beatmap.HitObjects[index + 1] : null;

            if (!configManager.EnableHitScan || hitObject is OsuSpinner)
                return HitScanResult.CanHit;

            if (lastHitScanIndex != index)
            {
                canMiss = configManager.HitScanMissChance != 0 && random.Next(1, 101) <= configManager.HitScanMissChance;
                lastHitScanIndex = index;
                lastOnNotePosition = null;
            }

            Vector2 hitObjectPosition = hitObject is OsuSlider ? (hitObject as OsuSlider).PositionAtTime(osuManager.CurrentTime) : hitObject.Position;
            Vector2 nextHitObjectPosition = nextHitObject != null ? nextHitObject.Position : Vector2.Zero;

            Vector2 cursorPosition = osuManager.WindowManager.ScreenToPlayfield(osuManager.Player.Ruleset.MousePosition);

            float distanceToObject = Vector2.Distance(cursorPosition, hitObjectPosition);
            float distanceToLastPos = Vector2.Distance(cursorPosition, lastOnNotePosition ?? Vector2.Zero);

            if (osuManager.CurrentTime > hitObject.EndTime + hitWindow50)
            {
                if (configManager.HitScanMissAfterHitWindow50)
                    if (distanceToObject <= hitObjectRadius + missRadius && !intersectsWithOtherHitObjects(index + 1))
                        return HitScanResult.ShouldHit;

                return HitScanResult.MoveToNextObject;
            }

            if (configManager.EnableHitScanPrediction)
            {
                if (distanceToObject > hitObjectRadius * configManager.HitScanPredictionRadiusScale && distanceToObject <= hitObjectRadius)
                {
                    if (lastOnNotePosition.HasValue && nextHitObject != null)
                    {
                        double directionAngle = MathHelper.GetAngle(lastOnNotePosition.Value, cursorPosition, nextHitObjectPosition);
                        if (directionAngle <= configManager.HitScanPredictionDirectionAngleTolerance || distanceToLastPos <= configManager.HitScanPredictionMaxDistance)
                            return HitScanResult.ShouldHit;
                    }
                }

                if (distanceToObject <= hitObjectRadius * configManager.HitScanPredictionRadiusScale)
                    lastOnNotePosition = cursorPosition;
                else
                    lastOnNotePosition = null;
            }

            if (distanceToObject <= hitObjectRadius)
                return HitScanResult.CanHit;

            if (canMiss && distanceToObject <= hitObjectRadius + missRadius && !intersectsWithOtherHitObjects(index + 1))
                return HitScanResult.CanHit;

            return HitScanResult.Wait;
        }

        private bool intersectsWithOtherHitObjects(int startIndex)
        {
            int time = osuManager.CurrentTime;
            double preEmpt = osuManager.DifficultyRange(beatmap.ApproachRate, 1800, 1200, 450);

            Vector2 cursorPosition = osuManager.WindowManager.ScreenToPlayfield(osuManager.Player.Ruleset.MousePosition);

            for (int i = startIndex; i < beatmap.HitObjects.Count; i++)
            {
                var hitObject = beatmap.HitObjects[i];

                double startTime = hitObject.StartTime - preEmpt;
                if (startTime > time)
                    break;

                float distanceToObject = Vector2.Distance(cursorPosition, hitObject.Position);
                if (distanceToObject <= hitObjectRadius)
                    return true;
            }

            return false;
        }
    }
}