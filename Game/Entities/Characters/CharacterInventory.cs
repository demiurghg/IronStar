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
	public class CharacterInventory {

		public int Grenades { get; private set; }
		public int Ammo1 { get; private set; }
		public int Ammo2 { get; private set; }

		public WeaponType Weapon1 { get; private set; }
		public WeaponType Weapon2 { get; private set; }



		/// <summary>
		/// 
		/// </summary>
		/// <param name="factory"></param>
		public CharacterInventory ( CharacterFactory factory )
		{
		}

	}
}
