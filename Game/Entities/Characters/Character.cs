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
using Fusion.Engine.Common;
using Fusion.Engine.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using IronStar.Core;
using BEPUphysics;
using Fusion.Core.IniParser.Model;


namespace IronStar.Entities {
	public partial class Character : EntityController {

		readonly CharacterController	controller;
		readonly CharacterHealth		health;
		readonly CharacterArmor			armor;
		readonly CharacterInventory		inventory;

		public CharacterController	Controller	{ get { return controller	; } }
		public CharacterHealth		Health		{ get { return health		; } }
		public CharacterArmor		Armor		{ get { return armor		; } }
		public CharacterInventory	Inventory	{ get { return inventory	; } }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="game"></param>
		/// <param name="space"></param>
		public Character ( Entity entity, GameWorld world, CharacterFactory factory ) : base(entity,world)
		{
			controller	=	new CharacterController( entity, world, factory );
			health		=	new CharacterHealth( factory );
			armor		=	new CharacterArmor( factory );
			inventory	=	new CharacterInventory( entity, world, factory );
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
		public override bool Damage ( uint targetID, uint attackerID, short damage, Vector3 kickImpulse, Vector3 kickPoint, DamageType damageType )
		{
			var c = controller;
			var e = Entity;

			int penetration;

			controller.Damage( kickImpulse, kickPoint );
			armor.Damage( damage, out penetration );
			health.Damage( penetration );

			return false;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		public override void Update ( float elapsedTime )
		{
			controller.Update( elapsedTime );
			armor.Update( elapsedTime );
			health.Update( elapsedTime );
			inventory.Update( elapsedTime );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="snapshotHeader"></param>
		public void UpdateHud ( SnapshotHeader snapshotHeader )
		{
			snapshotHeader.HudState[ (int)HudElement.Health ]	=	(short)health.Health;
			snapshotHeader.HudState[ (int)HudElement.Armor	]	=	(short)armor.Armor;

			inventory.UpdateHud( snapshotHeader );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		public override void Killed ()
		{
			controller.Killed();
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="action"></param>
		public override void Action( UserAction action )
		{
			switch (action) {
				case UserAction.Attack:			inventory.AttackWeapon(true); break;
				case UserAction.SwitchWeapon:	inventory.SwitchWeapon(); break;
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="action"></param>
		public override void CancelAction( UserAction action )
		{
			switch (action) {
				case UserAction.Attack:			inventory.AttackWeapon(false); break;
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		/// <param name="moveVector"></param>
		public override void Move( float forward, float right, float up )
		{
			controller.Move( forward, right, up );
		}
	}
}
