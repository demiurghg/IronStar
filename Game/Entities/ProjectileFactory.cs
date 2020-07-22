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

	public class ProjectileFactory : EntityFactory {

		[AECategory("Projectile")]
		[Description("Projectile velocity (m/sec)")]
		public float Velocity { get; set; } = 10;	

		[AECategory("Projectile")]
		[Description("Projectile damage impulse")]
		public float Impulse	{ get; set; } = 0;

		[AECategory("Projectile")]
		[Description("Hit damage")]
		public short Damage { get; set; } = 0;

		[AECategory("Projectile")]
		[Description("Projectile life-time in seconds")]
		public float LifeTime { get; set; } = 10;

		[AECategory("Projectile")]
		[Description("Hit radius in meters")]
		public float Radius { get; set; } = 0;


		[AECategory("Visual Effects")]
		[AEClassname("fx")]
		public string	ExplosionFX	{ get; set; } = "";

		[AECategory("Visual Effects")]
		[AEClassname("fx")]
		public string	TrailFX		{ get; set; } = "";


		public override ECS.Entity SpawnECS( ECS.GameState gs )
		{
			Log.Warning("SpawnECS -- {0}", GetType().Name);
			return null;
		}


		public override Entity Spawn( uint id, short clsid, GameWorld world )
		{
			return new Projectile( id, clsid, world, this );
		}
	}
}
