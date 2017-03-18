using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.UI.Attributes
{
    public class LogoAttribute : ControlAttribute
    {
        public string ImageName { get; private set; }

        public LogoAttribute(int order,string name, string ImageName)
        {
            this.Order = order;
            this.Name = name;
            this.ImageName = name;
        }
    }
}
