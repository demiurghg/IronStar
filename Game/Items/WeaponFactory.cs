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
using Fusion.Engine.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using Fusion.Core.Extensions;
using IronStar.Core;
using Fusion.Engine.Storage;
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
		public int Damage { get; set; } = 100;

		[Category("Shooting Properties")]
		public string Projectile { get; set; } = "";

		[Category("Shooting Properties")]
		public int Quantity { get; set; } = 1;

		[Category("Shooting Properties")]
		public string Ammo { get; set; } = "";

		[Category("Shooting Properties")]
		public int ConsumeAmmo { get; set; } = 1;

		[Category("Shooting Properties")]
		public int WarmupPeriod { get; set; } = 0;

		[Category("Shooting Properties")]
		public int CooldownPeriod { get; set; } = 500;

		[Category("Shooting Properties")]
		public float VSpread { get; set; } = 0;

		[Category("Shooting Properties")]
		public float HSpread { get; set; } = 0;

		[Category("Shooting Properties")]
		public bool PerfectFirstRound { get; set; } = false;

		
		[Category("Shooting FX")]
		public string MuzzleFX { get; set; } = "";

		[Category("Shooting FX")]
		public string TraceFX { get; set; } = "";

		[Category("Shooting FX")]
		public string HitFX { get; set; } = "";

	}


	[ContentLoader( typeof( WeaponFactory ) )]
	public sealed class WeaponFactoryLoader : ContentLoader {

		public override object Load( ContentManager content, Stream stream, Type requestedType, string assetPath, IStorage storage )
		{
			return Misc.LoadObjectFromXml( typeof(WeaponFactory), stream, null );
		}
	}
}
