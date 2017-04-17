namespace Fusion.Engine.Audio.XAPOExperiments.HRTF
{
    public static class Utils
    {
        public static float[] ToFloatArray(this short[] source)
        {
            float[] floatArr = new float[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                var f = ((float)source[i]) / ((float)short.MaxValue);
                if (f > 1) f = 1;
                if (f < -1) f = -1;
                floatArr[i] = f;
            }
            return floatArr;
        }
    }
}
