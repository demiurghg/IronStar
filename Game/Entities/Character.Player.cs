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
using BEPUphysics.Character;
using Fusion.Core.IniParser.Model;


namespace IronStar.Entities {
	public partial class Character : EntityController {

		readonly PlayerState playerState = new PlayerState();

		public PlayerState PlayerState {
			get {
				return playerState;
			}
		}



		void UpdatePlayerState ()
		{
			playerState.Health		=	Health;
			playerState.Armor		=	Armor;
			playerState.Weapon1		=	Weapon1;
			playerState.Weapon2		=	Weapon2;
			playerState.WeaponAmmo1	=	WeaponAmmo1;
			playerState.WeaponAmmo2	=	WeaponAmmo2;
		}
	}
}
