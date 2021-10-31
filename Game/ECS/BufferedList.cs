using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BEPUphysics.EntityStateManagement;
using Fusion.Core.Extensions;
using IronStar.ECS;
using RigidTransform = BEPUutilities.RigidTransform;
using System.Collections;
using BEPUphysics;
using BEPUEntity = BEPUphysics.Entities.Entity;
using BEPUCharacterController = BEPUphysics.Character.CharacterController;
using Fusion.Core.Mathematics;
using System.Collections.Concurrent;
using Fusion.Core;

namespace IronStar.ECS
{
	class BufferedList<T>
	{
		public BufferedList( int capacity )
		{
			writeBuffer	=	new List<T>(capacity);
			readBuffer	=	new List<T>(capacity);
		}

		List<T>		writeBuffer;
		List<T>		readBuffer;
		TimeSpan	writeTimestamp	=	TimeSpan.Zero;
		TimeSpan	readTimestamp	=	TimeSpan.Zero;
		object		flipLock		=	new object();


		public void Add( T item )
		{
			writeBuffer.Add( item );
		}

		
		public void Flip( GameTime gameTime )
		{
			lock (flipLock)
			{
				writeTimestamp	=	gameTime.Current;
				Misc.Swap( ref writeTimestamp,	ref readTimestamp );
				Misc.Swap( ref writeBuffer,		ref readBuffer );

				writeBuffer.Clear();
			}
		}

		
		public void Interpolate( GameState gs, GameTime gameTime, Action<T,float> interpolate, double timestep )
		{
			lock (flipLock)
			{
				double timestamp	=	readTimestamp.TotalSeconds;
				double time			=	gameTime.Current.TotalSeconds;

				float alpha = MathUtil.Clamp( (float)((time - timestamp)/timestep), 0, 1 );

				foreach ( var lerpData in readBuffer )
				{
					interpolate( lerpData, alpha );
				}
			}
		}
	}
}
