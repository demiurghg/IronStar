using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;

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

		public short WeaponModel;
		public float WeaponAnimFrame;

		public Color4 AmbientLevel;

		public void ClearHud ()
		{
			WeaponModel		=	0;
			WeaponAnimFrame	=	0;

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
			writer.Write( WeaponAnimFrame );

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
			WeaponAnimFrame	=	reader.ReadSingle();

			for ( int i=0; i<HudState.Length; i++ ) {
				HudState[i]	=	reader.ReadInt16();
			}
		}
	}

}
