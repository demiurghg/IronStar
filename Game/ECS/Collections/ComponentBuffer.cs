using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Extensions;

namespace IronStar.ECS
{
	class ComponentBuffer
	{
		Dictionary<uint, IComponent>	updating;
		Dictionary<uint, IComponent>	present;
		Dictionary<uint, IComponent>	previous;
		Dictionary<uint, IComponent>	lerped;
		object flipLock = new object();

		public ComponentBuffer()
		{
			updating	=	new Dictionary<uint, IComponent>();
			present		=	new Dictionary<uint, IComponent>();
			previous	=	new Dictionary<uint, IComponent>();
			lerped		=	new Dictionary<uint, IComponent>();
		}

		public int Count { get { return updating.Count; } }

		public void Add( uint id, IComponent c )
		{
			updating.Add( id, c );
		}

		public void Remove( uint id )
		{
			updating.Remove( id );
		}

		public void Clear()
		{
			updating.Clear();
		}

		public bool TryGetValue( uint id, out IComponent component )
		{
			return updating.TryGetValue(id, out component);
		}

		public void CommitChanges()
		{
			lock (flipLock)
			{
				Misc.Swap( ref present, ref previous );
				present.Clear();

				foreach ( var keyValue in updating )
				{
					present.Add( keyValue.Key, keyValue.Value.Clone() );
				}
			}
		}


		public void Interpolate()
		{ 
			lock (flipLock)
			{
				lerped.Clear();

				foreach ( var keyValue in present )
				{
					IComponent prev = null;

					if (previous.TryGetValue( keyValue.Key, out prev))
					{
						lerped.Add( keyValue.Key, keyValue.Value.Interpolate(prev, 0.5f) );
					}
					else
					{
						lerped.Add( keyValue.Key, keyValue.Value );
					}
				}
			}
		}
	}
}
