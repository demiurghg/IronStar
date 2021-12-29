using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion.Core.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using BEPUphysics;
using BEPUphysics.Character;
using BEPUCharacterController = BEPUphysics.Character.CharacterController;
using Fusion.Core.IniParser.Model;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.NarrowPhaseSystems.Pairs;
using RigidTransform = BEPUutilities.RigidTransform;
using IronStar.ECS;
using IronStar.Gameplay;
using BEPUutilities.Threading;

namespace IronStar.ECSPhysics 
{
	public class CharacterTrackingSystem : ISystem
	{
		bool record = false;
		Aspect playerAspect = new Aspect().Include<PlayerComponent,CharacterController,Transform,UserCommandComponent>();

		List<Tuple<Vector3,Color>>			points	=	new List<Tuple<Vector3,Color>>();
		List<Tuple<Vector3,Vector3,Color>>	lines	=	new List<Tuple<Vector3,Vector3,Color>>();

		public void Add( IGameState gs, Entity e ) {}

		public Aspect GetAspect() { return Aspect.Empty; }

		public void Remove( IGameState gs, Entity e ) {}

		public void Update( IGameState gs, GameTime gameTime )
		{
			var kb	=	gs.Game.Keyboard;
			var rs	=	gs.Game.RenderSystem;
			var dr	=	rs.RenderWorld.Debug;

			if (kb.IsKeyDown(Keys.T)) record = true;
			if (kb.IsKeyDown(Keys.U)) record = false;
			if (kb.IsKeyDown(Keys.Delete)) { points.Clear(); lines.Clear(); }

			if (record)
			{
				RenderSystem.SkipDebugRendering = false;

				foreach ( var player in gs.QueryEntities(playerAspect) )
				{
					var t	=	player.GetComponent<Transform>();
					var uc	=	player.GetComponent<UserCommandComponent>();

					points.Add( Tuple.Create( t.Position, Color.Yellow ) );
					//points.Add( Tuple.Create( t.CurrentPosition + Vector3.Up, Color.Red ) );
				}
			}

			foreach ( var point in points )
			{
				dr.DrawPoint( point.Item1, 0.25f, point.Item2 );
			}
		}
	}
}
