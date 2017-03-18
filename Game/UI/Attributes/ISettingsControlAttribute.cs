using Fusion.Engine.Frames;
using Fusion.Engine.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.UI.Attributes
{
    public interface ISettingsControlAttribute
    {
        Frame GetFrame(FrameProcessor fp, string name, SpriteFont font);
    }
}
