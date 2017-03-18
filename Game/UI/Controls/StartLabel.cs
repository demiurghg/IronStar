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
    public class StartLabel : Label
    {

        public StartLabel(FrameProcessor fp, string name) : base(fp)
        {
            this.Name = name;
        }

        protected override void Update(GameTime gameTime)
        {
            //TODO : remove
            if (ForeColor.A == 255)
            {
                RunTransition("ForeColor", new Color(255, 255, 255, 120), 0, 900);
            } else if (ForeColor.A == 120)
            {
                RunTransition("ForeColor", new Color(255, 255, 255, 255), 0, 900);
            }
            base.Update(gameTime);
        }
    }
}
