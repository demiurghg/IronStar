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
using IronStar.Entities;

namespace IronStar {

	public class PlayerState : IStorable{

		public readonly static PlayerState NullState = new PlayerState();

		public short		Health		;
		public short		Armor		;

		public short		ViewModel	;

		public WeaponType	Weapon1		;
		public WeaponType	Weapon2		;

		public short		WeaponAmmo1	;
		public short		WeaponAmmo2	;



		/// <summary>
		/// 
		/// </summary>
		/// <param name="writer"></param>
		public void Write ( BinaryWriter writer )
		{
			writer.Write( Health		);
			writer.Write( Armor			);
			writer.Write( ViewModel		);
			writer.Write( (byte)Weapon1	);
			writer.Write( (byte)Weapon2	);
			writer.Write( WeaponAmmo1	);
			writer.Write( WeaponAmmo2	);
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="writer"></param>
		public void Read ( BinaryReader reader, float lerpFactor )
		{
			Health		=	reader.ReadInt16();
			Armor		=	reader.ReadInt16();
			ViewModel	=	reader.ReadInt16();
			Weapon1		=	(WeaponType)reader.ReadByte();
			Weapon2		=	(WeaponType)reader.ReadByte();
			WeaponAmmo1	=	reader.ReadInt16();
			WeaponAmmo2	=	reader.ReadInt16();
		}


		
	}
}
