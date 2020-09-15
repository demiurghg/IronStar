using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using Fusion.Engine.Graphics;
using IronStar.ECS;
using IronStar.ECSPhysics;
using IronStar.SFX2;
using Native.NRecast;
using System.ComponentModel;
using Fusion.Core.Extensions;
using IronStar.BTCore;
using IronStar.BTCore.Actions;
using IronStar.BTCore.Decorators;
using IronStar.AI.Actions;
using IronStar.Gameplay;

namespace IronStar.AI
{
	class PerceptionSystem : StatelessSystem<BehaviorComponent>
	{
		public bool Enabled = true;
		readonly PhysicsCore physics;

		public PerceptionSystem(PhysicsCore physics)
		{
			this.physics	=	physics;
		}


		protected override void Process( Entity entity, GameTime gameTime, BehaviorComponent behavior )
		{
			Vector3 pov;
			BoundingFrustum frustum;
			BoundingSphere sphere;

			if (Enabled)
			{
				var dr		=	entity.gs.GetService<RenderSystem>().RenderWorld.Debug;
				var player	=	entity.gs.GetPlayer();


				if (GetSensoricBoundingVolumes(entity, out pov, out frustum, out sphere))
				{
					bool hasLos = false;
					bool visibility = false;
					
					if (player!=null)
					{
						var playerPos	=	player.GetPOV();
						hasLos			=	physics.HasLineOfSight( pov, playerPos, entity, player );
						visibility		=	hasLos && ( frustum.Contains( playerPos ) == ContainmentType.Contains );
					}

					if (visibility)
					{
						behavior.LastSeenTarget	=	player;
					}

					entity.GetBlackboard().SetEntry( "TargetEntity", visibility ? player : null );


					var color	=	visibility ? Color.Red : Color.Lime;

					dr.DrawFrustum( frustum, color, 0.02f, 2 );
					dr.DrawRing( Matrix.Translation(sphere.Center), sphere.Radius, Color.Green, 32, 2, 1 );
				}
			}
		}


		/*-----------------------------------------------------------------------------------------------
		 *	Utilities :
		-----------------------------------------------------------------------------------------------*/

		bool GetSensoricBoundingVolumes( Entity npcEntity, out Vector3 pov, out BoundingFrustum frustum, out BoundingSphere sphere )
		{
			frustum	=	new BoundingFrustum();
			sphere	=	new BoundingSphere();
			pov		=	new Vector3();

			var cc			=	npcEntity.GetComponent<CharacterController>();
			var transform	=	npcEntity.GetComponent<Transform>();
			var behavior	=	npcEntity.GetComponent<BehaviorComponent>();
			var uc			=	npcEntity.GetComponent<UserCommandComponent>();

			if (cc==null || transform==null || transform==null || uc==null )
			{
				return false;
			}

			var origin		=	transform.Position + cc.PovOffset;
			var rotation	=	uc.Rotation;
			var head		=	uc.RotationMatrix * Matrix.Translation( origin );
			var view		=	Matrix.Invert( head );

			var proj		=	Matrix.PerspectiveFovRH( MathUtil.DegreesToRadians( behavior.VisibilityFov ), 2, 0.01f, behavior.VisibilityRange );

			pov				=	origin;
			frustum			=	new BoundingFrustum( view * proj );
			sphere			=	new BoundingSphere( transform.Position, behavior.HearingRange );

			return true;
		}
	}
}
