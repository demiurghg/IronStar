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

	public enum PainLevel {
		NoPain,
		LightPain,
		MediumPain,
		SeverePain,
		Death,
		Shreds,
	}



	public class CharacterHealth {

		/// <summary>
		/// 
		/// </summary>
		public int Health { get; private set; }

		/// <summary>
		/// 
		/// </summary>
		public int MaxHealth { get; private set; }


		/// <summary>
		/// 
		/// </summary>
		/// <param name="initialHealth"></param>
		/// <param name="maxHealth"></param>
		public CharacterHealth ( CharacterFactory factory )
		{
			this.Health		=	factory.MaxHealth;
			this.MaxHealth	=	factory.MaxHealth;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="damage"></param>
		public PainLevel Damage ( int damage )
		{
			return PainLevel.NoPain;
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
		public bool GiveHealth ( int amount, bool overflow )
		{
			if (Health>=MaxHealth) {
				return false;
			}

			Health += amount;

			if (!overflow) {
				Health = Math.Min( MaxHealth, Health );
			}

			return true;
		}
	}
}
