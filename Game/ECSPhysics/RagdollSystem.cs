using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
			return new RagdollController( physics, rm.LoadScene(entity.gs) );
		}

		protected override void Destroy( Entity entity, RagdollController resource )
		{
			//throw new NotImplementedException();
		}

		protected override void Process( Entity entity, GameTime gameTime, RagdollController controller, RagdollComponent ragdoll, RenderModel rm, BoneComponent bones )
		{
			controller.DrawDebug( entity.gs.Game.RenderSystem.RenderWorld.Debug.Async );
		}
	}
}
