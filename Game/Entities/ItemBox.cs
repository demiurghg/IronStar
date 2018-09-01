using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Engine.Common;
using Fusion.Core.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using IronStar.Core;
using Fusion.Core.IniParser.Model;
using BEPUphysics;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.EntityStateManagement;
using BEPUphysics.PositionUpdating;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.NarrowPhaseSystems.Pairs;
using IronStar.SFX;
using System.Runtime.CompilerServices;
using IronStar.Physics;
using IronStar.Entities.Players;
//using BEPUphysics.


namespace IronStar.Entities {

	public class ItemBox : Entity {

		readonly DynamicBox box;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="game"></param>
		/// <param name="space"></param>
		public ItemBox ( uint id, short clsid, GameWorld world, ItemBoxFactory factory ) : base( id, clsid, world, factory )
		{
			var width		=	factory.Width;
			var height		=	factory.Height;
			var depth		=	factory.Depth;
			var mass		=	factory.Mass;
			var model		=	factory.Model;

			var itemNames	=	factory.Item.Split(new[] {' ',';',','});

			foreach ( var itemName in itemNames ) {
				world.SpawnItem( itemName, ID );
			}

			box				=	new DynamicBox( this, world, width, height, depth, mass, true );

			this.Model		=	world.Atoms[ model ];
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="targetID"></param>
		/// <param name="attackerID"></param>
		/// <param name="damage"></param>
		/// <param name="kickImpulse"></param>
		/// <param name="kickPoint"></param>
		/// <param name="damageType"></param>
		public override void Damage( Entity attacker, int damage, DamageType damageType, Vector3 kickImpulse, Vector3 kickPoint )
		{
			box.Kick( kickImpulse, kickPoint );
		}



		public override void Kill()
		{
			base.Kill();
			box.Destroy();
		}


		public override bool AllowUse {
			get {
				return true;
			}
		}


		public override void Use( Entity user )
		{
			Log.Message("Box used");
		}


		public override void Update ( GameTime gameTime )
		{
			base.Update(gameTime);

			this.Position			=	box.Position; 
			this.Rotation			=	box.Orientation; 
			this.LinearVelocity		=	box.LinearVelocity;
			this.AngularVelocity	=	box.AngularVelocity;
		}


		public override void Touch( Entity other, Vector3 touchPoint )
		{
			var player = other as Player;

			if (player!=null) {

				bool accepted = false;

				foreach ( var item in World.Items.GetOwnedItems(ID) ) {
					accepted |= item.Pickup(player);
				}

				if (accepted) {
					World.Kill(ID);
				}
			}
		}


		public override void Teleport( Vector3 position, Quaternion orient )
		{
			base.Teleport( position, orient );
			box.Teleport( position, orient );
		}
	}
}
