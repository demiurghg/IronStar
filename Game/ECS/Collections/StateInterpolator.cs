using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace IronStar.ECS.Collections
{
	public class StateInterpolator<T>
	{
		TimeSpan timestamp;
		TimeSpan timestep;
		readonly object lockObj = new object();
		readonly T[] array = new T[3];
		int index = 0;

		public bool HasData { get { return index > 0; } }

		public void FeedAndFlip ( TimeSpan timestamp, TimeSpan timestep, T data )
		{
			lock (lockObj)
			{
				this.timestamp		=	timestamp;
				this.timestep		=	timestep;
				array[ index % 3 ]	=	data;
				index++;
			}
		}

		public T Interpolate( TimeSpan time, Func<T,T,float,T> interpolate )
		{
			lock (lockObj)
			{
				if (!HasData)
				{
					throw new InvalidOperationException("No data is feed yet");
				}

				double	ftimestamp	=	timestamp.TotalSeconds;
				double	ftimestep	=	timestep.TotalSeconds;
				double	ftime		=	time.TotalSeconds;

				float	factor		=	MathUtil.Clamp( (float)((ftime - ftimestamp)/ftimestep), 0, 1 );

				var prevIndex = (index - 2) % 3;
				var currIndex = (index - 1) % 3;

				if (index==1)
				{
					//	only one item is recorded
					return array[ currIndex ];
				}

				return interpolate( array[ prevIndex ], array[ currIndex ], factor );
			}
			
		}
	}
}
