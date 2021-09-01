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

namespace IronStar.ECSPhysics
{
	class MotionStateBuffer
	{
		readonly object lockObj	=	new object();

		readonly List<Entry> entries;

		double timeStamp = -1; // means no data

		class Entry
		{
			public Entry( ISpaceObject spaceObj )
			{
				SpaceObject		=	spaceObj;
				var motionState	=	GetMotionState( spaceObj );
				PreviousState	=	motionState;
				CurrentState	=	motionState;
			}
			public readonly ISpaceObject SpaceObject;
			public MotionState PreviousState;
			public MotionState CurrentState;
		}


		public MotionStateBuffer()
		{
			entries	=	new List<Entry>();
		}


		public void Add( ISpaceObject spaceObj )
		{
			lock (lockObj)
			{
				entries.Add( new Entry( spaceObj ) );
			}
		}



		public void Remove( ISpaceObject spaceObj )
		{
			lock (lockObj)
			{
				entries.RemoveAll( entry => entry.SpaceObject == spaceObj );
			}
		}


		public void UpdateSimulationResults( double time )
		{
			lock (lockObj)
			{
				timeStamp = time;
				
				foreach ( var entry in entries )
				{
					entry.PreviousState	=	entry.CurrentState;
					entry.CurrentState	=	GetMotionState( entry.SpaceObject );
				}
			}
		}


		public void InterpolateMotionStates( Dictionary<ISpaceObject,MotionState> destination, double time, float dt )
		{
			destination.Clear();

			lock (lockObj)
			{
				var alpha	=	( timeStamp < 0 ) ? 1.0f : MathUtil.Clamp((float)((time - timeStamp) / dt), 0, 1 );

				foreach ( var entry in entries )
				{
					destination.Add( entry.SpaceObject, LerpState( entry.PreviousState, entry.CurrentState, alpha ) );
				}
			}
		}


		static MotionState LerpState( MotionState a, MotionState b, float alpha )
		{
			var r = new MotionState();

			r.Position			=	BEPUutilities.Vector3.Lerp		( a.Position,			b.Position,			alpha ); 
			r.Orientation		=	BEPUutilities.Quaternion.Slerp	( a.Orientation,		b.Orientation,		alpha ); 
			r.LinearVelocity	=	BEPUutilities.Vector3.Lerp		( a.LinearVelocity,		b.LinearVelocity,	alpha ); 
			r.AngularVelocity	=	BEPUutilities.Vector3.Lerp		( a.AngularVelocity,	b.AngularVelocity,	alpha ); 

			return r;
		}


		static MotionState GetMotionState( ISpaceObject obj )
		{
			var physEntity = obj as BEPUEntity;
			if (physEntity!=null)
			{
				return physEntity.MotionState;
			}

			var projCtrl = obj as ProjectileController;
			if (projCtrl!=null)
			{
				return projCtrl.MotionState;
			}

			var charCtrl = obj as BEPUCharacterController;
			if (charCtrl!=null)
			{
				return charCtrl.Body.MotionState;
			}

			return default(MotionState);
		}
	}
}
