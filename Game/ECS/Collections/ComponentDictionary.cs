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
	public class ComponentTuple
	{
		public ComponentTuple( IComponent current ) { Current = current; }
		public bool			Updated		=	false;
		public IComponent	Current		=	null;
		public IComponent	Previous	=	null;
		public IComponent	Lerped		=	null;
	}

	/// <summary>
	/// #TODO #ECS #REFACTOR -- move interpolation to separate class
	/// </summary>
	class ComponentDictionary : Dictionary<uint,ComponentTuple>
	{
		public ComponentDictionary()
		{
		}

		public void Add( uint id, IComponent c )
		{
			Add( id, new ComponentTuple(c) );
		}

		
		public bool TryGetValue( uint id, out IComponent component )
		{
			ComponentTuple tuple;
			component = null;

			if (TryGetValue(id, out tuple))
			{
				component = tuple.Lerped ?? tuple.Current;
				return true;
			}
			
			return false;
		}


		public void Interpolate( TimeSpan timestamp, TimeSpan timestep, TimeSpan time )
		{ 
			double	ftimestamp	=	timestamp.TotalSeconds;
			double	ftimestep	=	timestep.TotalSeconds;
			double	ftime		=	time.TotalSeconds;

			float	factor		=	MathUtil.Clamp( (float)((ftime - ftimestamp)/ftimestep), 0, 1 );

			foreach ( var keyValue in this )
			{
				var tuple = keyValue.Value;

				if (tuple.Previous!=null)
				{
					tuple.Lerped = tuple.Current.Interpolate( tuple.Previous, factor );
				}
				else
				{
					tuple.Lerped = tuple.Current;
				}
			}
		}
	}
}