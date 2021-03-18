using osu;
using osu.Enums;
using osu_rx.Configuration;
using SimpleDependencyInjection;
using System;
using System.Diagnostics;
using System.Threading;

namespace osu_rx.Core.Timewarp
{
    public class Timewarp
    {
        public bool IsRunning { get; private set; }

        //TODO: i couldn't create signature for this one :(
        private readonly int[] audioRateOffsets = new int[]
        {
            0x00034268,
            0x8,
            0x10,
            0xC,
            0x40
        };

        private const double defaultRate = 1147;

        private OsuManager osuManager;
        private ConfigManager configManager;
        private UIntPtr audioRateAddress = UIntPtr.Zero;
        private bool shouldStop;
        private double initialRate;

        public Timewarp()
        {
            osuManager = DependencyContainer.Get<OsuManager>();
            configManager = DependencyContainer.Get<ConfigManager>();
        }

        public void Start()
        {
            shouldStop = false;
            initialRate = osuManager.Player.HitObjectManager.CurrentMods.HasFlag(Mods.DoubleTime) ? 1.5 : osuManager.Player.HitObjectManager.CurrentMods.HasFlag(Mods.HalfTime) ? 0.75 : 1;
            refresh();

            while (!shouldStop && osuManager.CanPlay)
            {
                setRate(configManager.TimewarpRate);

                Thread.Sleep(1);
            }

            setRate(shouldStop ? initialRate : 1, false);
        }

        public void Stop() => shouldStop = true;

        private void refresh()
        {
            foreach (ProcessModule module in osuManager.OsuProcess.Process.Modules)
            {
                if (module.ModuleName == "bass.dll")
                {
                    audioRateAddress = (UIntPtr)module.BaseAddress.ToInt32();
                    break;
                }
            }

            for (int i = 0; i < audioRateOffsets.Length; i++)
            {
                audioRateAddress += audioRateOffsets[i];

                if (i != audioRateOffsets.Length - 1)
                    audioRateAddress = (UIntPtr)osuManager.OsuProcess.ReadUInt32(audioRateAddress);
            }
        }

        private void setRate(double rate, bool bypass = true)
        {
            //nightcore hack
            rate /= osuManager.Player.HitObjectManager.CurrentMods.HasFlag(Mods.Nightcore) ? 1.5 : 1;

            if (osuManager.OsuProcess.ReadDouble(audioRateAddress) != rate)
            {
                osuManager.OsuProcess.WriteMemory(audioRateAddress, BitConverter.GetBytes(rate), sizeof(double));
                osuManager.OsuProcess.WriteMemory(audioRateAddress + 0x8, BitConverter.GetBytes(rate * defaultRate), sizeof(double));
            }

            //bypassing audio checks
            if (bypass)
                osuManager.Player.AudioCheckCount = int.MinValue;
        }
    }
}