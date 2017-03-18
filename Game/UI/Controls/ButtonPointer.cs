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
    public class ButtonPointer : Frame
    {

        public bool IsAppeared { get; set; }
        public int AppearedTime { get; private set; }
        public int MoveTime { get; private set; }
        public ButtonPointer(FrameProcessor fp, string name, Color backColor, int appearedTime, int moveTime) : base(fp)
        {
            this.Name = name;
            this.BackColor = backColor;
            this.IsAppeared = false;
            this.AppearedTime = appearedTime;
            this.MoveTime = moveTime;
        }
    }
}
