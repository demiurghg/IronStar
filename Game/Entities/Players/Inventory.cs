using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.Items;
using Fusion.Core.Mathematics;

namespace IronStar.Entities.Players {
	public class Inventory : Dictionary<string,int> {

		public bool AddItem ( string name, int count, int maxCount = int.MaxValue )
		{
			if (ContainsKey(name)) {

				if (this[name]>=maxCount) {
					return false;
				}

				this[name] = MathUtil.Clamp( this[name] + count, 0, maxCount );

				return true;

			} else {

				Add( name, Math.Min( count, maxCount ) );

				return true;

			}
		}


		public bool TryTakeItem ( string name, int count )
		{
			if (!ContainsKey(name)) {

				return false;

			} else {

				if (this[name] >= count) {
					this[name] = this[name] - count;
					return true;
				} else {
					return false;
				}
			}


		}

	}
}
