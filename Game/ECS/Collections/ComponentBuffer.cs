using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Extensions;
using Fusion.Core.Mathematics;

namespace IronStar.ECS
{
	/// <summary>
	/// #TODO #ECS #REFACTOR -- move interpolation to separate class
	/// </summary>
	class ComponentBuffer
	{
		Dictionary<uint, IComponent>	updating;
		Dictionary<uint, IComponent>	present;
		Dictionary<uint, IComponent>	previous;
		Dictionary<uint, IComponent>	lerped;
		object flipLock = new object();
		TimeSpan timestamp;
		TimeSpan timestep;

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

		
		public void CommitChanges( TimeSpan timestamp, TimeSpan timestep )
		{
			lock (flipLock)
			{
				this.timestamp	=	timestamp;
				this.timestep	=	timestep;

				Misc.Swap( ref present, ref previous );
				present.Clear();

				foreach ( var keyValue in updating )
				{
					present.Add( keyValue.Key, keyValue.Value.Clone() );
				}
			}
		}


		public bool TryGetInterpolatedValue( uint id, out IComponent component )
		{
			return lerped.TryGetValue( id, out component );
		}


		public void Interpolate( TimeSpan time )
		{ 
			lock (flipLock)
			{
				double	ftimestamp	=	timestamp.TotalSeconds;
				double	ftimestep	=	timestep.TotalSeconds;
				double	ftime		=	time.TotalSeconds;

				float	factor		=	MathUtil.Clamp( (float)((ftime - ftimestamp)/ftimestep), 0, 1 );

				lerped.Clear();

				foreach ( var keyValue in present )
				{
					IComponent prev = null;

					if (previous.TryGetValue( keyValue.Key, out prev))
					{
						lerped.Add( keyValue.Key, keyValue.Value.Interpolate(prev, factor) );
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
