using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.ECS;

namespace IronStar.AI
{
	public class AITargetCollection : ICollection<AITarget>
	{
		readonly List<AITarget> list;

		public int Count { get	{ return list.Count; } }

		public bool IsReadOnly { get	{ return false;	} }

		public AITargetCollection()
		{
			list = new List<AITarget>(10);
		}


		public bool HasEntity( Entity entity )
		{
			foreach ( var target in this )
			{
				if (target.Entity==entity)
				{
					return true;
				}
			}

			return false;
		}


		public int EraseEntity( Entity entity )
		{
			return list.RemoveAll( target => target.Entity==entity );
		}

		public void EraseDeadAndForgotten()
		{
			list.RemoveAll( tt => tt.ForgettingTimer.IsElapsed || !AIUtils.IsAlive(tt.Entity) );
		}

		public void Add( AITarget item )
		{
			list.Add( item );
		}

		public void Clear()
		{
			list.Clear();
		}

		public bool Contains( AITarget item )
		{
			return list.Contains( item );
		}

		public void CopyTo( AITarget[] array, int arrayIndex )
		{
			list.CopyTo( array, arrayIndex );
		}

		public bool Remove( AITarget item )
		{
			return list.Remove( item );
		}

		public IEnumerator<AITarget> GetEnumerator()
		{
			return ( (ICollection<AITarget>)list ).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ( (ICollection<AITarget>)list ).GetEnumerator();
		}
	}
}
