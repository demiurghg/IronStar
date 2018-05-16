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
using Fusion.Core.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using IronStar.Core;
using BEPUphysics;
using BEPUphysics.Character;
using Fusion.Core.IniParser.Model;


namespace IronStar.Entities {
	public class CharacterArmor {

		/// <summary>
		/// 
		/// </summary>
		public int Armor { get; private set; }

		/// <summary>
		/// 
		/// </summary>
		public int MaxArmor { get; private set; }


		/// <summary>
		/// 
		/// </summary>
		/// <param name="initialHealth"></param>
		/// <param name="maxHealth"></param>
		public CharacterArmor ( CharacterFactory factory )
		{
			this.Armor		=	factory.MaxArmor;
			this.MaxArmor	=	factory.MaxArmor;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="damage"></param>
		public void Damage ( int damage, out int armorPenetrationDamage )
		{
			armorPenetrationDamage = 0;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="elapsedTime"></param>
		public void Update ( float elapsedTime )
		{
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="amount"></param>
		/// <param name="overflow"></param>
		/// <returns></returns>
		public bool GiveArmor ( int amount, bool overflow )
		{
			if (Armor>=MaxArmor) {
				return false;
			}

			Armor += amount;

			if (!overflow) {
				Armor = Math.Min( MaxArmor, Armor );
			}

			return true;
		}
	}
}
