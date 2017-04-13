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
using IronStar.Entities;

namespace IronStar.Items {

	public partial class Weapon : Item {

		bool	rqAttack;
		bool	rqReload;
		bool	rqActivation;
		int		rqNextWeapon;

		Timer	timer = new Timer();

		Entity		playerEntity;
		Character	playerCharacter;

		static readonly IState	stInactive		=	new Inactive	();
		static readonly IState	stReady			=	new Ready		();
		static readonly IState	stWarmup		=	new Warmup		();
		static readonly IState	stCooldown		=	new Cooldown	();
		static readonly IState	stReloading		=	new Reloading	();
		static readonly IState	stEmpty			=	new Empty		();
		static readonly IState	stActivation	=	new Activation	();
		static readonly IState	stDeactivation	=	new Deactivation();

		readonly WeaponFactory factory;
		IState	state;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="factory"></param>
		public Weapon( string name, WeaponFactory factory ) : base( name )
		{
			state			=	stInactive;
			this.factory	=	factory;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public override Entity Drop()
		{
			Log.Warning("WEAPON DROP");

			playerEntity	= null;
			playerCharacter = null;

			return null;
		}

		

		/// <summary>
		/// 
		/// </summary>
		/// <param name="player"></param>
		/// <returns></returns>
		public override bool Pickup( Entity player )
		{
			Log.Warning("WEAPON PICKUP");

			playerEntity	=	player;
			playerCharacter	=	player?.Controller as Character;

			if (playerCharacter==null) {
				Log.Warning("Attempt to pick item by non-character entity");
				playerEntity	=	null;
				return false;
			}

			playerCharacter.Inventory.AddItem( this );
			playerCharacter.Inventory.SwitchToWeapon(ID);

			return true;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="elsapsedTime"></param>
		public override void Update( float elapsedTime )
		{
			int dt = (int)(elapsedTime * 1000);
			state.Update( this, dt );
		}



		/// <summary>
		/// 
		/// </summary>
		void FireProjectile ()
		{
			Log.Warning("** FIRE **");
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="state"></param>
		void SetState ( IState state )
		{
			Log.Warning("...enter weapon state : {0}", state.GetType().Name );

			this.state = state;
			this.state.Enter( this );
		}


		/*-----------------------------------------------------------------------------------------
		 * 
		 *	External commands :
		 * 
		-----------------------------------------------------------------------------------------*/

		public void Attack()
		{
			rqAttack	=	true;
		}


		public void Activate ()
		{
			rqActivation = true;
		}


		public void Reload ()
		{
			rqReload = true;
		}


		public void Switch ( int nextWeaponID )
		{	
			if (nextWeaponID==ID) {
				//	TODO : check fast A-B-A switch
				rqNextWeapon	=	0;
			} else {
				rqNextWeapon	=	nextWeaponID;
			}
		}
	}
}

