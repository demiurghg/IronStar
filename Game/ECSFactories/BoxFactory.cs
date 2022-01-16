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
using IronStar.SFX;
using Fusion.Core.Extensions;

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

		public override void Construct( Entity e, IGameState gs )
		{
			base.Construct( e, gs );

			e.AddComponent( new RenderModel( model, Matrix.Scaling( scale ), Color.White, 5, RMFlags.None ) );
			e.AddComponent( new DynamicBox( width, height, depth, mass ) );
		}
	}


	[EntityFactory("BOXEXPLOSIVE")]
	public class BoxExplosiveFactory : BoxFactory
	{
		public BoxExplosiveFactory():
		base( 3, 2.25f, 2.25f, 5, 3, "scenes\\boxes\\box_low.fbx" ) {}

		public override void Construct( Entity e, IGameState gs )
		{
			float timeout = MathUtil.Random.NextFloat(0.2f, 0.3f);

			base.Construct( e, gs );

			e.AddComponent( new HealthComponent(2, 0) );
			e.AddComponent( new ExplosiveComponent(	timeout, 100,12,300, "boxBurning", "boxExplosion") ); 
			e.AddComponent( new MaterialComponent( MaterialType.Metal ) );
		}
	}


	[EntityFactory("GIBLET")]
	public class GibletFactory : BoxFactory
	{
		public GibletFactory():
		base( 1, 1, 1, 0.3f, 2, "scenes\\boxes\\box_low.fbx" ) {}

		public override void Construct( Entity e, IGameState gs )
		{
			base.Construct( e, gs );

			//e.AddComponent( new HealthComponent(2, 0, "EXPLODE_BOX") );
			e.AddComponent( new FXComponent("bloodTrail", false) );
			e.GetComponent<DynamicBox>().Group = CollisionGroup.PickupGroup;
			e.AddComponent( new MaterialComponent(MaterialType.Flesh) );
		}
	}
}
