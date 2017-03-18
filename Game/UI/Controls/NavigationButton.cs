using Fusion.Core.Mathematics;
using Fusion.Engine.Frames;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.UI.Controls
{
    public class NavigationButton : Frame
    {

        public Color HoverColor { get; set; }
        public Color ClickColor { get; set; }
        public Color DefaultBackColor { get; set; }

        public NavigationButton(FrameProcessor fp) : base(fp)
        {
            Initialize();
        }

        public NavigationButton(FrameProcessor ui, string text, Color backColor, Color hoverColor, Color ClickColor) : base(ui, 0, 0, 0, 0, text, backColor)
        {
            Initialize();
        }

        private void Initialize()
        {
            DefaultBackColor = BackColor;
            this.StatusChanged += (sender, args) =>
            {
                switch (args.Status)
                {
                    case FrameStatus.Hovered:
                        RunTransition("ForeColor", HoverColor, 0, 300);
                        break;
                    case FrameStatus.None:
                        RunTransition("ForeColor", DefaultBackColor, 0, 300);
                        break;
                    case FrameStatus.Pushed:
                        RunTransition("ForeColor", ClickColor, 0, 300);
                        break;
                }
            };
        }
    }
}
