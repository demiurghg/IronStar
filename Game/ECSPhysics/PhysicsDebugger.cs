using System;
using System.Collections.Generic;
using BEPUphysics;
using System.Linq;
using BEPUVector3 = BEPUutilities.Vector3;
using IronStar.SFX;
using Fusion.Engine.Graphics.Scenes;
using Fusion.Core.Mathematics;
using BEPUphysics.NarrowPhaseSystems.Pairs;
using BEPUphysics.CollisionRuleManagement;
using IronStar.ECS;
using Fusion.Core;
using Fusion;
using IronStar.Gameplay;
using Fusion.Core.Extensions;
using BEPUCollisionGroup = BEPUphysics.CollisionRuleManagement.CollisionGroup;
using BEPUutilities.Threading;
using System.Threading;
using System.Diagnostics;
using RigidTransform = BEPUutilities.RigidTransform;
using System.Collections.Concurrent;
using BEPUphysics.EntityStateManagement;
using BEPUrender.Lines;
using BEPUrender.Models;
using Fusion.Engine.Graphics;

namespace IronStar.ECSPhysics
{
	public partial class PhysicsDebugger : DisposableBase, ISystem
	{
		readonly PhysicsCore physics;

		readonly SimulationIslandDrawer islandDrawer;
		readonly ContactDrawer			contactDrawer;
		readonly BoundingBoxDrawer		bboxDrawer;
		readonly LineDrawer				lineDrawer;

		readonly ModelDrawer			modelDrawer;

		readonly DebugRender			debugRender;

		public PhysicsDebugger (PhysicsCore physics)
		{
			this.physics	=	physics;

			physics.ObjectAdded+=Physics_ObjectAdded;
			physics.ObjectRemoved+=Physics_ObjectRemoved;

			debugRender		=	physics.Game.RenderSystem.RenderWorld.Debug.Async;

			islandDrawer	=	new SimulationIslandDrawer();
			contactDrawer	=	new ContactDrawer();
			bboxDrawer		=	new BoundingBoxDrawer();
			lineDrawer		=	new LineDrawer();

			modelDrawer		=	new ModelDrawer(debugRender);
		}

		private void Physics_ObjectAdded( object sender, PhysicsCore.SpaceObjectArgs e )
		{
			if (PhysicsCore.UseDebugDraw)
			{
				lineDrawer.Add( e.SpaceObject );
				modelDrawer.Add( e.SpaceObject );
			}
		}

		private void Physics_ObjectRemoved( object sender, PhysicsCore.SpaceObjectArgs e )
		{
			if (PhysicsCore.UseDebugDraw)
			{
				lineDrawer.Remove( e.SpaceObject );
				modelDrawer.Remove( e.SpaceObject );
			}
		}

		public Aspect GetAspect(){ return Aspect.Empty; }
		public void Add( IGameState gs, Entity e ) {}
		public void Remove( IGameState gs, Entity e ) {}


		protected override void Dispose( bool disposing )
		{
			if (disposing)
			{
				lineDrawer.Clear();
				modelDrawer.Clear();
			}

			base.Dispose( disposing );
		}


		public void Update( IGameState gs, GameTime gameTime )
		{													
			if (RenderSystem.SkipDebugRendering)
			{
				return;
			}

			//	#TODO #PHYSICS -- restore deleted models
			if (PhysicsCore.UseDebugDraw)
			{
				islandDrawer	.Draw( debugRender, physics.Space );
				contactDrawer	.Draw( debugRender, physics.Space );
				bboxDrawer		.Draw( debugRender, physics.Space );

				lineDrawer		.Draw( debugRender );
				modelDrawer		.Draw( debugRender );
			}
			else
			{
				lineDrawer	.Clear();
				modelDrawer	.Clear();
			}
		}
	}
}
