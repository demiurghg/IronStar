using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Engine.Graphics;
using IronStar.ECS;
using IronStar.ECSPhysics;

namespace IronStar.Gameplay
{
	public class CameraSystem : ISystem
	{
		public Aspect GetAspect()
		{
			return Aspect.Empty;
		}


		public void Add( GameState gs, Entity e ) {}
		public void Remove( GameState gs, Entity e ) {}


		public void Update( GameState gs, GameTime gameTime )
		{
			var	players	=	gs.QueryEntities<PlayerComponent,Transform,UserCommandComponent,CharacterController>();

			if (players.Count()>1)
			{
				Log.Warning("CameraSystem.Update -- multiple players detected");
			}

			foreach ( var player in players)
			{
				SetupPlayerCamera(gs, player);
			}
		}


		void SetupPlayerCamera( GameState gs, Entity e )
		{
			var t	=	e.GetComponent<Transform>();
			var v	=	e.GetComponent<Velocity>();
			var uc	=	e.GetComponent<UserCommandComponent>();
			var ch	=	e.GetComponent<CharacterController>();

			var	rs	=	gs.GetService<RenderSystem>();
			var rw	=	rs.RenderWorld;
			var sw	=	gs.Game.SoundSystem;
			var vp	=	gs.Game.RenderSystem.DisplayBounds;

			var aspect	=	(vp.Width) / (float)vp.Height;

			var camMatrix	=	uc.RotationMatrix;
			var cameraPos	=	t.Position + ch.PovOffset;
			var cameraFwd	=	camMatrix.Forward;
			var cameraUp	=	camMatrix.Up;
			var velocity	=	v==null ? Vector3.Zero : v.Linear;

			rw.Camera		.LookAt( cameraPos, cameraPos + cameraFwd, cameraUp );
			rw.WeaponCamera	.LookAt( cameraPos, cameraPos + cameraFwd, cameraUp );

			rw.Camera		.SetPerspectiveFov( MathUtil.Rad(90),  0.125f/2.0f, 12288, aspect );
			rw.WeaponCamera	.SetPerspectiveFov( MathUtil.Rad(75),	0.125f/2.0f, 6144, aspect );

			sw.SetListener( cameraPos, cameraFwd, cameraUp, velocity );
		}
	}
}
