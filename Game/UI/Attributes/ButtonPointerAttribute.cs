using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.UI.Attributes
{
    public class ButtonPointerAttribute : ControlAttribute
    {
        public ButtonPointerAttribute(int order, string name)
        {
            this.Name = name;
            this.Order = order;
        }
    }
}
