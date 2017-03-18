using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.UI.Attributes
{
    public class DescriptionLabelAttribute : ControlAttribute
    {
        public string Text { get; private set; }
        public DescriptionLabelAttribute(int order, string name, string text)
        {
            this.Name = name;
            this.Order = order;
            this.Text = text;
        }
    }
}
