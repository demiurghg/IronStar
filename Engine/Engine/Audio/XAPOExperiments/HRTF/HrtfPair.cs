namespace Fusion.Engine.Audio.XAPOExperiments.HRTF
{
    public class HrtfPair
    {
        public float[] leftChannel { get; private set; }
        public float[] rightChannel { get; private set; }
        
        public HrtfPair(float[] left, float[] right)
        {
            leftChannel = left;
            rightChannel = right;
        }
    }
}
