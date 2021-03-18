namespace osu_rx.Core.Relax.Accuracy
{
    public class HitObjectTimings
    {
        public int StartOffset { get; set; }
        public int HoldTime { get; set; }

        public HitObjectTimings(int startOffset = 0, int holdTime = 0)
        {
            StartOffset = startOffset;
            HoldTime = holdTime;
        }
    }
}
