using Fusion.Engine.Frames;
using Fusion.Engine.Graphics;
using IronStar.UI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.UI.Attributes
{
    public class NavigationButtonAttribute : ControlAttribute
    {
        public string Text { get; private set; }
        public NavigationButtonAttribute(int order, string name, string text)
        {
            this.Order = order;
            this.Text = text;
            this.Name = name;
        }
    }
}
