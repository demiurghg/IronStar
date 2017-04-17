using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Engine.Audio.XAPOExperiments.HRTF
{
    public enum Channel
    {
        Left,
        Right
    }

    internal class ConvolutionFile
    {
        public int Elevation { get; set; }
        public int Angle { get; set; }
        public string Path { get; set; }
        public Channel Channel { get; set; }
    }
}
