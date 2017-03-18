using Fusion.Engine.Frames;
using Fusion.Engine.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.UI.Controls
{
    public class Slider : Frame, IValuableControl
    {
        /// <summary>
        /// Settings
        /// </summary>
        private float labelOffset = 0.022f;
        private float lineOffset = 0.474f;
        private float lineSizeX = 0.50f;
        private float lineSizeY = 0.10f;

        private float sliderSizeX = 0.013f;
        private float sliderSizeY = 0.41f;


        private float sliderOffsetX;

        public float MinValue { get; set; }
        public float MaxValue { get; set; }
        public float Value
        {
            get
            {
                return value;
            }
            set
            {
                this.value = value;
                ValueChanged?.Invoke(this, EventArgs.Empty);
                slider.X = (int)(line.X + line.Width * Value / MaxValue);
            }
        }


        private float value;

        private Image line;
        private Image slider;
        private Label label;

        public event EventHandler ValueChanged;

        public Slider(FrameProcessor fp, string name, string text, SpriteFont font, float minValue, float maxValue) : base(fp)
        {
            this.Name = name;
            this.Font = font;
            MinValue = minValue;
            MaxValue = maxValue;


            Initialize(fp, text);
        }
        private void Initialize(FrameProcessor fp, string text)
        {
            line = new Image(fp);
            slider = new Image(fp);

            slider.Ghost = true;
            line.Ghost = true;

            label = new Label(fp);
            label.Text = text;
            label.Font = Font;

            line.Image = fp.Game.Content.Load<DiscTexture>(@"ui\sliderLine");
            line.ImageMode = FrameImageMode.Stretched;
            slider.Image = fp.Game.Content.Load<DiscTexture>(@"ui\slider");
            slider.ImageMode = FrameImageMode.Stretched;

            Resize += OnResize;
            Click += MouseClick;
            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseUp += OnMouseUp;


            Add(label);
            Add(line);
            Add(slider);
        }


        private void OnResize(object sender, ResizeEventArgs e)
        {
            var r = Font.MeasureString(label.Text);

            label.Width = r.Width;
            label.Height = r.Height;

            label.X = (int)(Width * labelOffset);
            label.Y = (Height - label.Height) / 2;


            line.Width = (int)(Width * lineSizeX);
            line.Height = (int)(Height * lineSizeY);

            line.X = (int)(Width * lineOffset);
            line.Y = (Height - line.Height) / 2;

            slider.Width = (int)(Width * sliderSizeX);
            slider.Height = (int)(sliderSizeY * Height);

            slider.X = line.X + (int)(line.Width * value / MaxValue);
            slider.Y = (Height - slider.Height) / 2;
        }

        private bool isSliderMoving = false;
        private bool ignoreMouse = false;

        private void OnMouseDown(object sender, MouseEventArgs args)
        {
            float value = (args.X - line.X) / (float)line.Width;
            if (value >= 0 && value <= 1)
            {
                slider.X = args.X;
            }
            isSliderMoving = true;
        }

        private void OnMouseMove(object sender, MouseEventArgs args)
        {
            if (isSliderMoving)
            {
                slider.X = args.X;
                if (slider.X < line.X)
                {
                    ignoreMouse = true;
                    slider.X = line.X;
                }
                if (slider.X > line.X + line.Width)
                {
                    ignoreMouse = true;
                    slider.X = line.X + line.Width;
                }
            }
        }

        
        private void OnMouseUp(object sender, MouseEventArgs args)
        {
            isSliderMoving = false;
            float value = (slider.X - line.X) / (float)line.Width;
            if (value >= 0 && value <= 1)
            {
                Value = value * MaxValue;
            }
        }



        private void MouseClick(object sender, MouseEventArgs args)
        {
            float value = (args.X - line.X) / (float)line.Width;
            if (value >= 0 && value <= 1)
            {
                slider.X = args.X;
                Value = value * MaxValue;
            }
        }

    }
}
