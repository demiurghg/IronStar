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


	[EntityAction( "EXPLODE_BOX" )]
	public class ExplodeBoxAction : EntityAction
	{
		public override void Execute( GameState gs, Entity target )
		{
			float explosionTime = MathUtil.Random.NextFloat(0.05f, 0.1f);
			target.AddComponent( new FXComponent("boxBurning", true) );
			target.AddComponent( new ProjectileComponent(0, 7.5f, explosionTime, "boxExplosion", 100, 100) );
		}
	}


	[EntityFactory("BOXEXPLOSIVE")]
	public class BoxExplosiveFactory : BoxFactory
	{
		public BoxExplosiveFactory():
		base( 3, 2.25f, 2.25f, 10, 3, "scenes\\boxes\\box_low.fbx" ) {}

		public override Entity Spawn( GameState gs )
		{
			var e = base.Spawn( gs );

			e.AddComponent( new HealthComponent(2, 0, "EXPLODE_BOX") );

			return e;
		}
	}


	[EntityFactory("GIBLET")]
	public class GibletFactory : BoxFactory
	{
		public GibletFactory():
		base( 1, 1, 1, 0.3f, 2, "scenes\\boxes\\box_low.fbx" ) {}

		public override Entity Spawn( GameState gs )
		{
			var e = base.Spawn( gs );

			//e.AddComponent( new HealthComponent(2, 0, "EXPLODE_BOX") );
			e.AddComponent( new FXComponent("bloodTrail", false) );
			e.GetComponent<DynamicBox>().Group = CollisionGroup.PickupGroup;

			return e;
		}
	}
}
