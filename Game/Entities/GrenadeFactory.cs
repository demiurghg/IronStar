using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion.Core.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using Fusion.Core.Extensions;
using IronStar.Core;
using BEPUphysics;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.EntityStateManagement;
using BEPUphysics.PositionUpdating;
using Fusion.Core.IniParser.Model;
using System.ComponentModel;
using Fusion.Core.Shell;

namespace IronStar.Entities {

	public class GrenadeFactory : EntityFactory {

		[AECategory("Physics")]
		[Description("Projectile velocity (m/sec)")]
		public float Velocity { get; set; } = 10;	

		[AECategory("Physics")]
		[Description("Capsule length")]
		public float ShapeLength { get; set; } = 0.1f;	

		[AECategory("Physics")]
		[Description("Capsule radius")]
		public float ShapeRadius { get; set; } = 0.1f;	

		[AECategory("Physics")]
		[Description("Capsule radius")]
		public float Mass { get; set; } = 1f;	

		[AECategory("Damage")]
		[Description("Projectile damage impulse")]
		public float ExplosionImpulse	{ get; set; } = 0;

		[AECategory("Damage")]
		[Description("Hit damage")]
		public short ExplosionDamage { get; set; } = 0;

		[AECategory("Damage")]
		[Description("Grenade life-time in seconds")]
		public float DetonationTime { get; set; } = 10;

		[AECategory("Damage")]
		[Description("Hit radius in meters")]
		public float ExplosionRadius { get; set; } = 0;


		[AECategory("Visual Effects")]
		[AEClassname("models")]
		public string	Model	{ get; set; } = "";

		[AECategory("Visual Effects")]
		public string	ExplosionFX	{ get; set; } = "";

		[AECategory("Visual Effects")]
		public string	TrailFX		{ get; set; } = "";


		public override EntityController Spawn( Entity entity, GameWorld world )
		{
			return new Grenade( entity, world, this );
		}
	}
}
