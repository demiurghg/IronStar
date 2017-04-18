using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using IronStar.Items;

namespace IronStar.Core {

	public enum HudElement {
		Crosshair,
		
		Health,
		Armor, 

		Weapon,
		WeaponAmmo,

		Max,
	}


	public class SnapshotHeader : IStorable {

		public Vector3 SunDirection;
		public float Turbidity;
		public float FogDensity;
		public float Gravity;
		public float SunIntensity;

		public short		WeaponModel;
		public WeaponState	WeaponState;

		public Color4 AmbientLevel;

		public void ClearHud ()
		{
			WeaponModel		=	0;
			WeaponState		=	WeaponState.Idle;

			for ( int i=0; i<HudState.Length; i++ ) {
				HudState[i]	=	0;
			}
		}

		public readonly short[] HudState = new short[(int)HudElement.Max];


		public void Write( BinaryWriter writer )
		{
			writer.Write( SunDirection );
			writer.Write( Turbidity );
			writer.Write( FogDensity );
			writer.Write( Gravity );
			writer.Write( SunIntensity );

			writer.Write( AmbientLevel );

			writer.Write( WeaponModel );
			writer.Write( (byte)WeaponState );

			for ( int i=0; i<HudState.Length; i++ ) {
				writer.Write( HudState[i] );
			}
		}


		public void Read( BinaryReader reader, float lerpFactor )
		{
			SunDirection	=	reader.Read<Vector3>();
			Turbidity		=	reader.ReadSingle();
			FogDensity		=	reader.ReadSingle();
			Gravity			=	reader.ReadSingle();
			SunIntensity	=	reader.ReadSingle();

			AmbientLevel	=	reader.Read<Color4>();

			WeaponModel		=	reader.ReadInt16();
			WeaponState		=	(WeaponState)reader.ReadByte();

			for ( int i=0; i<HudState.Length; i++ ) {
				HudState[i]	=	reader.ReadInt16();
			}
		}
	}

}
