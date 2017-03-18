using Fusion.Core.Mathematics;
using Fusion.Engine.Frames;
using Fusion.Engine.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Engine.Common;

namespace IronStar.UI.Controls
{
    public class DescriptionLabel : Frame
    {
        public string MainDescription
        {
            get
            {
                return mainDesc;
            }
            set
            {
                mainDesc = value;
                Text = mainDesc + "    " + addDesc;
                TextChanged();
            }
        }
        private string mainDesc;

        public string AdditionalDescription
        {
            get
            {
                return addDesc;
            }
            set
            {
                addDesc = value;
                Text = mainDesc + "    " + addDesc;
                TextChanged();
            }
        }

        private string addDesc;

        public DescriptionLabel(FrameProcessor fp, SpriteFont font, string mainDesc) : base(fp)
        {
            this.Font = font;
            this.MainDescription = mainDesc;
        }

        private void TextChanged()
        {
            var r = Font.MeasureString(Text);
            Width = r.Width;
            Height = r.Height;
        }
    }
}
