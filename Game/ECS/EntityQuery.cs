using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace IronStar.ECS
{
	public class EntityQuery<TQueryResult> : IEnumerable<TQueryResult>
	{
		readonly GameState gs;

		public EntityQuery( GameState gs )
		{
			this.gs = gs;
		}

		public IEnumerator<TQueryResult> GetEnumerator()
		{
			throw new NotImplementedException();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			throw new NotImplementedException();
		}
	}
}
