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

namespace IronStar.Gameplay
{
	public class CameraSystem : ISystem
	{
		public Aspect GetAspect()
		{
			return Aspect.Empty();
		}


		public void Update( GameState gs, GameTime gameTime )
		{
			var	players	=	gs.QueryEntities<PlayerController,Transform,UserCommand2>();

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
			var uc	=	e.GetComponent<UserCommand2>();

			var	rs	=	gs.GetService<RenderSystem>();
			var rw	=	rs.RenderWorld;
			var sw	=	gs.Game.SoundSystem;
			var vp	=	gs.Game.RenderSystem.DisplayBounds;

			var aspect	=	(vp.Width) / (float)vp.Height;

			var camMatrix	=	uc.RotationMatrix;
			var cameraPos	=	t.Position + Vector3.Up * 5.5f;
			var cameraFwd	=	camMatrix.Forward;
			var cameraUp	=	camMatrix.Up;

			rw.Camera		.LookAt( cameraPos, cameraPos + cameraFwd, cameraUp );
			rw.WeaponCamera	.LookAt( cameraPos, cameraPos + cameraFwd, cameraUp );

			rw.Camera		.SetPerspectiveFov( MathUtil.Rad(90),  0.125f/2.0f, 12288, aspect );
			rw.WeaponCamera	.SetPerspectiveFov( MathUtil.Rad(75),	0.125f/2.0f, 6144, aspect );
		}
	}
}
