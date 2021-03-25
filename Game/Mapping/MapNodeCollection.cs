using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.Mapping 
{
	public class MapNodeCollection : List<MapNode> 
	{
		public MapNodeCollection()
		{
		}

		public MapNodeCollection( IEnumerable<MapNode> nodes ) : base(nodes)
		{
		}
	}
}
