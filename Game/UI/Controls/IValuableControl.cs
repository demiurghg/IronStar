using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.UI.Controls
{
    public interface IValuableControl
    {
        float Value { get; set; }
        event EventHandler ValueChanged;
    }
}
