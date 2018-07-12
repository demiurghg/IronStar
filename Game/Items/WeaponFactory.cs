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
using Fusion.Core.Shell;
using System.IO;
using BEPUphysics;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.EntityStateManagement;
using BEPUphysics.PositionUpdating;
using Fusion.Core.IniParser.Model;
using System.ComponentModel;
using Fusion.Core.Shell;

namespace IronStar.Items {

	public class WeaponFactory : ItemFactory {

		[AECategory("Shooting")]
		public bool BeamWeapon { get; set; }
		
		[AECategory("Shooting")]
		public float BeamLength { get; set; }
		
		[AECategory("Shooting")]
		[AEClassname("entities")]
		public string Projectile { get; set; } = "";

		[AECategory("Shooting")]
		public int Damage { get; set; }

		[AECategory("Shooting")]
		public float Impulse { get; set; }

		[AECategory("Shooting")]
		public int ProjectileCount { 
			get { return projectileCount; }
			set { projectileCount = MathUtil.Clamp(value, 1, 100); }
		}
		int projectileCount = 1;

		[AECategory("Shooting")]
		public float AngularSpread { get; set; }

		[AECategory("Shooting")]
		public int Cooldown {
			get { return cooldown; }
			set { cooldown = MathUtil.Clamp(value, 0, 10000); }
		}
		int cooldown = 1;

		[AECategory("Beam")]
		[AEClassname("fx")]
		public string HitFX { get; set; } = "";

		[AECategory("Ammo")]
		[AEClassname("items")]
		public string AmmoItem { get; set; } = "";



		public override Item Spawn( short clsid, GameWorld world )
		{
			return new Weapon( clsid, world, this );
		}
	}
}
