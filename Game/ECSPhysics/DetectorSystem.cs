using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Fusion.Core;
using Fusion.Core.Mathematics;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.DataStructures;
using IronStar.ECS;
using IronStar.ECSPhysics;
using IronStar.Gameplay;
using IronStar.Gameplay.Systems;
using IronStar.AI;

namespace IronStar.ECSPhysics 
{
	public class DetectorSystem : ProcessingSystem<DetectorVolume,DetectorComponent,Transform>
	{
		readonly PhysicsCore physics;

		readonly Aspect playerAspect	=	new Aspect().Include<PlayerComponent>();
		readonly Aspect monsterAspect	=	new Aspect().Include<AIComponent>();


		public DetectorSystem ( PhysicsCore physics )
		{
			this.physics	=	physics;
		}


		protected override DetectorVolume Create( Entity entity, DetectorComponent detector, Transform transform )
		{
			var detectorVolume	=	new DetectorVolume( CreateCube( transform.TransformMatrix, detector.LocalBounds ) );
			detectorVolume.Tag	=	entity;

			physics.Add( detectorVolume );

			detectorVolume.EntityBeganTouching += DetectorVolume_EntityBeganTouching;
			detectorVolume.EntityStoppedTouching +=DetectorVolume_EntityStoppedTouching;

			return detectorVolume;
		}

		protected override void Destroy( Entity entity, DetectorVolume resource )
		{
			physics.Remove( resource );
		}


		protected override void Process( Entity entity, GameTime gameTime, DetectorVolume resource, DetectorComponent component1, Transform component2 )
		{
			// #TODO #PHYSICS -- update detector volume position
		}


		private void HandleTouchEvent( DetectorVolume volume, bool beginTouch, BEPUphysics.Entities.Entity toucher )
		{
			var detectorEntity	= volume.Tag as Entity;
			var activatorEntity = toucher.Tag as Entity;

			//Log.Debug("DETECT : {0} : {1}", beginTouch, activatorEntity );
			var acceptMonster	=	monsterAspect.Accept(activatorEntity);
			var acceptPlayer	=	playerAspect.Accept(activatorEntity);

			if (acceptMonster || acceptPlayer)
			{
				var detector	=	detectorEntity.GetComponent<DetectorComponent>();
				var target		=	detectorEntity.GetComponent<DetectorComponent>().Target;

				if ((detector.DetectMonsters && acceptMonster) || (detector.DetectPlayer && acceptPlayer))
				{
					if (beginTouch)
					{
						activatorEntity.gs.Trigger( target, detectorEntity, activatorEntity );
						detector.Touchers.Add( activatorEntity );
					}
					else
					{
						detector.Touchers.Remove( activatorEntity );
					}
				}
			}
		}


		private void DetectorVolume_EntityBeganTouching( DetectorVolume volume, BEPUphysics.Entities.Entity toucher )
		{
			HandleTouchEvent( volume, true, toucher );
		}

		private void DetectorVolume_EntityStoppedTouching( DetectorVolume volume, BEPUphysics.Entities.Entity toucher )
		{
			HandleTouchEvent( volume, false, toucher );
		}


		TriangleMesh CreateCube ( Matrix transform, BoundingBox box )
		{
			float px = box.Maximum.X;
			float py = box.Maximum.Y;
			float pz = box.Maximum.Z;

			float nx = box.Minimum.X;
			float ny = box.Minimum.Y;
			float nz = box.Minimum.Z;

			var m	=	transform;

			var verts = new[] {
				Vector3.TransformCoordinate( new Vector3( nx, ny, pz ), m ),
				Vector3.TransformCoordinate( new Vector3( px, ny, pz ), m ),
				Vector3.TransformCoordinate( new Vector3( nx, py, pz ), m ),
				Vector3.TransformCoordinate( new Vector3( px, py, pz ), m ),
				Vector3.TransformCoordinate( new Vector3( nx, py, nz ), m ),
				Vector3.TransformCoordinate( new Vector3( px, py, nz ), m ),
				Vector3.TransformCoordinate( new Vector3( nx, ny, nz ), m ),
				Vector3.TransformCoordinate( new Vector3( px, ny, nz ), m ),
			};

			var verts2 = verts.Select( v => MathConverter.Convert( v ) ).ToArray();

			var inds = new [] {
				0, 1, 2,	2, 1, 3,
				2, 3, 4,	4, 3, 5,
				4, 5, 6,	6, 5, 7,
				6, 7, 0,	0, 7, 1,
				1, 7, 3,	3, 7, 5,
				6, 0, 4,	4, 0, 2,
			};

			return new TriangleMesh( new StaticMeshData( verts2, inds ) );
		}
	}
}
