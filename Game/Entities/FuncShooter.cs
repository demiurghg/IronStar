using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion.Core.Extensions;
using IronStar.Core;
using IronStar.SFX;
using System.ComponentModel;
using Fusion.Core.Shell;
using Fusion.Core;
using Fusion;
using IronStar.Items;
using IronStar.Entities.Players;

namespace IronStar.Entities {

	public class FuncShooter : Entity, IShooter {
		
		static Random rand = new Random();

		readonly Weapon weapon;
		readonly bool trigger;
		readonly bool once;
		bool enabled;

		int activationCount = 0;

		public FuncShooter( uint id, short clsid, GameWorld world, FuncShooterFactory factory ) : base(id, clsid, world, factory)
		{
			weapon	=	world.SpawnItem( factory.Weapon, id ) as Weapon;
			trigger	=	factory.Trigger;
			once	=	factory.Once;
			enabled	=	factory.Start;
		}


		public override void Activate( Entity activator )
		{
			if (once && activationCount>0) {
				return;
			}

			if (weapon==null) {
				return;
			}

			activationCount ++;

			if (trigger) {
				weapon?.Attack( this, this );
			} else {
				enabled = !enabled;
				Log.Verbose("FuncShooter: toggle enabled");
			}
		}


		public override void Update( GameTime gameTime )
		{
			int msec = gameTime.Milliseconds;

			//	update
			if (!trigger) {
				if (enabled) {
					weapon?.Attack( this, this );
				}
			}
		}


		public override void Kill()
		{
			base.Kill();
		}


		public Vector3 GetActualPOV()
		{
			return Position;
		}


		public Vector3 GetVisiblePOV()
		{
			return GetActualPOV();
		}
	}



	/// <summary>
	/// 
	/// </summary>
	public class FuncShooterFactory : EntityFactory {

		[AEClassname("items")]
		public string Weapon { get; set; } = "";

		public bool Trigger { get; set; }

		public bool Once { get; set; }

		public bool Start { get; set; }



		public override Entity Spawn( uint id, short clsid, GameWorld world )
		{
			return new FuncShooter( id, clsid, world, this );
		}
	}
}
