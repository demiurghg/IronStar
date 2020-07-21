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

	public class FuncShooter : Entity {
		
		static Random rand = new Random();

		readonly bool trigger;
		readonly bool once;
		bool enabled;

		int activationCount = 0;

		public FuncShooter( uint id, short clsid, GameWorld world, FuncShooterFactory factory ) : base(id, clsid, world, factory)
		{
			trigger	=	factory.Trigger;
			once	=	factory.Once;
			enabled	=	factory.Start;


			var weapon	=	world.SpawnItem( factory.Weapon, id ) as Weapon;
			ItemID		=	weapon==null ? 0 : weapon.ID;
		}


		public override void Activate( Entity activator )
		{
			if (once && activationCount>0) {
				return;
			}

			var weapon = World.Items.GetItem(ItemID);

			activationCount ++;

			if (trigger) {
				Log.Verbose("FuncShooter: attack");
				weapon?.Attack( this );
			} else {
				enabled = !enabled;
				Log.Verbose("FuncShooter: toggle enabled");
			}
		}


		public override void Update( GameTime gameTime )
		{
			base.Update(gameTime);

			int msec = gameTime.Milliseconds;

			var weapon = World.Items.GetItem(ItemID);

			//	update
			if (!trigger) {
				if (enabled) {
					weapon?.Attack( this );
				}
			}
		}


		public override void Kill()
		{
			base.Kill();
		}


		public override Vector3 GetActualPOV()
		{
			return Position;
		}


		public override Vector3 GetVisiblePOV()
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


		public override void SpawnECS( ECS.GameState gs )
		{
			Log.Warning("SpawnECS -- {0}", GetType().Name);
		}

		public override Entity Spawn( uint id, short clsid, GameWorld world )
		{
			return new FuncShooter( id, clsid, world, this );
		}
	}
}
