using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using IronStar.Animation;
using IronStar.ECS;
using IronStar.SFX2;

namespace IronStar.ECSPhysics
{
	public class RagdollSystem : ProcessingSystem<RagdollController, Transform, RagdollComponent, RenderModel, BoneComponent>, ITransformFeeder
	{
		readonly PhysicsCore physics;

		public RagdollSystem(PhysicsCore physics)
		{
			this.physics	=	physics;
		}

		protected override RagdollController Create( Entity entity, Transform transform, RagdollComponent ragdoll, RenderModel rm, BoneComponent bones )
		{
			if (rm.ComputeScale()!=1) 
			{
				Log.Warning("Ragdoll does not support scaled meshes");
			}
			
			var ragdollController = new RagdollController( physics, rm.LoadScene(entity.gs) );

			ragdollController.LoadAnimatedTransforms( transform, bones );
			ragdollController.ApplyInitialImpulse( entity.GetComponent<ImpulseComponent>() );

			return ragdollController;
		}

		protected override void Destroy( Entity entity, RagdollController resource )
		{
			resource.Destroy();
		}

		protected override void Process( Entity entity, GameTime gameTime, RagdollController controller, Transform transform, RagdollComponent ragdoll, RenderModel rm, BoneComponent bones )
		{
		}


		void StoreSimulatedTransforms( Entity entity, GameTime gameTime, RagdollController controller, Transform transform, RagdollComponent ragdoll, RenderModel rm, BoneComponent bones )
		{
			controller.ApplyTransforms( transform, bones );
		}


		public void FeedTransform( IGameState gs, GameTime gameTime )
		{
			ForEach( gs, gameTime, StoreSimulatedTransforms );
		}

	}
}
