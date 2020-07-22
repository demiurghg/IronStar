using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Engine.Graphics;
using IronStar.ECS;

namespace IronStar.Gameplay
{
	public class CameraSystem : ISystem
	{
		public void Update( GameState gs, GameTime gameTime )
		{
			var	e	=	gs.QueryEntities<PlayerController,Transform>().FirstOrDefault();

			if (e!=null) SetupPlayerCamera(gs, e);
		}


		void SetupPlayerCamera( GameState gs, Entity e )
		{
			var t	=	e.GetComponent<Transform>();

			var	rs	=	gs.GetService<RenderSystem>();
			var rw	=	rs.RenderWorld;
			var sw	=	gs.Game.SoundSystem;
			var vp	=	gs.Game.RenderSystem.DisplayBounds;

			var aspect	=	(vp.Width) / (float)vp.Height;

			rw.Camera		.SetView( Matrix.Invert( t.TransformMatrix ) );
			rw.WeaponCamera	.SetView( Matrix.Invert( t.TransformMatrix ) );

			rw.Camera		.SetPerspectiveFov( MathUtil.Rad(120),  0.125f/2.0f, 12288, aspect );
			rw.WeaponCamera	.SetPerspectiveFov( MathUtil.Rad(75),	0.125f/2.0f, 6144, aspect );
		}
	}
}
