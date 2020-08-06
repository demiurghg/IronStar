using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.ECS;
using IronStar.ECSPhysics;
using IronStar.Gameplay;
using IronStar.Gameplay.Components;
using IronStar.SFX2;

namespace IronStar.ECSFactories
{
	public class BoxFactory : EntityFactory
	{
		readonly float	width	=	1f;
		readonly float	height	=	1f;
		readonly float	depth	=	1f;
		readonly float	mass	=	10;

		readonly float	scale	=	1.0f;
		readonly string model	=	"";

		protected BoxFactory( float w, float h, float d, float mass, float scale, string model )
		{
			this.width	=	w;
			this.height	=	h;
			this.depth	=	d;
			this.mass	=	mass;
			this.scale	=	scale;
			this.model	=	model;
		}

		public override Entity Spawn( GameState gs )
		{
			var e = gs.Spawn();

			e.AddComponent( new RenderModel( model, Matrix.Scaling( scale ), Color.White, 5, RMFlags.None ) );
			e.AddComponent( new DynamicBox( width, height, depth, mass ) );

			e.AddComponent( new Transform() );
			e.AddComponent( new Velocity() );

			return e;
		}
	}


	[EntityFactory("BOXEXPLOSIVE")]
	public class BoxExplosiveFactory : BoxFactory
	{
		public BoxExplosiveFactory():
		base( 3, 2.25f, 2.25f, 10, 3, "scenes\\boxes\\box_low.fbx" ) {}
	}
}
