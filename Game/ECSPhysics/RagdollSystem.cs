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
	public class RagdollSystem : ProcessingSystem<RagdollController, RagdollComponent, RenderModel, BoneComponent>
	{
		readonly PhysicsCore physics;

		public RagdollSystem(PhysicsCore physics)
		{
			this.physics	=	physics;
		}

		protected override RagdollController Create( Entity entity, RagdollComponent ragdoll, RenderModel rm, BoneComponent bones )
		{
			if (rm.ComputeScale()!=1) Log.Warning("Ragdoll does not support scaled meshes");
			return new RagdollController( physics, rm.LoadScene(entity.gs) );
		}

		protected override void Destroy( Entity entity, RagdollController resource )
		{
			resource.Destroy();
		}

		protected override void Process( Entity entity, GameTime gameTime, RagdollController controller, RagdollComponent ragdoll, RenderModel rm, BoneComponent bones )
		{
			var transform = entity.GetComponent<Transform>();
			if (transform!=null)
			{
				controller.ApplyTransforms( transform, bones );
			}
			//controller.DrawDebug( entity.gs.Game.RenderSystem.RenderWorld.Debug.Async );
		}
	}
}
