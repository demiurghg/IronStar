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
using Fusion.Core;
using System.IO;
using BEPUphysics;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.EntityStateManagement;
using BEPUphysics.PositionUpdating;
using Fusion.Core.IniParser.Model;
using System.ComponentModel;

namespace IronStar.Items {

	public class WeaponFactory : ItemFactory {

		[Category("Shooting Properties")]
		[Description("Damage per projectile")]
		public int Damage { get; set; } = 100;

		[Category("Shooting Properties")]
		[Description("Projectile classname")]
		public string Projectile { get; set; } = "";

		[Category("Shooting Properties")]
		[Description("Number of shot projectiles")]
		public int ProjectileQuantity { get; set; } = 1;

		[Category("Shooting Properties")]
		[Description("Number of shot projectiles")]
		public int AmmoCapacity { get; set; } = 100;



		[Category("Shooting Properties")]
		[Description("Tangent vertical spread addition")]
		public float VSpread { get; set; } = 0;

		[Category("Shooting Properties")]
		[Description("Tangent horizontal spread addition")]
		public float HSpread { get; set; } = 0;

		[Category("Shooting Properties")]
		[Description("Indicates that first bullet must be perfectly aimed")]
		public bool PerfectFirstRound { get; set; } = false;



		[Category("Shooting Timing")]
		[Description("Idle animation period")]
		public int IdlePeriod { get; set; } = 500;

		[Category("Shooting Timing")]
		[Description("Time to take weapon up")]
		public int ActivationPeriod { get; set; } = 200;

		[Category("Shooting Timing")]
		[Description("Time to put weapon down")]
		public int DeactivationPeriod { get; set; } = 200;

		[Category("Shooting Timing")]
		[Description("Delay between triggering attack and actual shot")]
		public int WarmupPeriod { get; set; } = 0;

		[Category("Shooting Timing")]
		[Description("Delay between actual shot and readiness for next shot")]
		public int CooldownPeriod { get; set; } = 500;


		
		[Category("View Model")]
		public string ViewModel { get; set; } = "";

		[Category("View Model")]
		public AnimRegion IdleAnimation { get; set; } = new AnimRegion();

		[Category("View Model")]
		public AnimRegion WarmupAnimation { get; set; } = new AnimRegion();

		[Category("View Model")]
		public AnimRegion CooldownAnimation { get; set; } = new AnimRegion();

		[Category("View Model")]
		public AnimRegion ActivationAnimation { get; set; } = new AnimRegion();

		[Category("View Model")]
		public AnimRegion DeactivationAnimation { get; set; } = new AnimRegion();


		
		[Category("Shooting FX")]
		public string MuzzleFX { get; set; } = "";

		[Category("Shooting FX")]
		public string TraceFX { get; set; } = "";

		[Category("Shooting FX")]
		public string HitFX { get; set; } = "";

		[Category("Shooting FX")]
		public string EmptyFX { get; set; } = "";

		public override Item Spawn( GameWorld world )
		{
			return new Weapon( Name, world, this );
		}
	}
}
