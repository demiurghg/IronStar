using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.UI.Attributes
{
    public class StartLabelAttribute : ControlAttribute
    {
        public string Text { get; private set; }
        public StartLabelAttribute(int order, string name, string text)
        {
            this.Name = name;
            this.Order = order;
            this.Text = text;
        }
    }
}
