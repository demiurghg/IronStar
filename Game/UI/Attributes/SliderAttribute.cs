using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Engine.Frames;
using Fusion.Engine.Graphics;
using IronStar.UI.Controls;

namespace IronStar.UI.Attributes
{
    public class SliderAttribute : ControlAttribute, ISettingsControlAttribute
    {
        public float MinValue { get; set; }
        public float MaxValue { get; set; }
        public string Text { get; set; }
        public SliderAttribute(int order, string name, string text)
        {
            this.Order = order;
            this.Name = name;
            this.Text = text;
        }


        public Frame GetFrame(FrameProcessor fp, string name, SpriteFont font)
        {
            return new Slider(fp, name, Text, font, MinValue, MaxValue);
        }
    }
}
