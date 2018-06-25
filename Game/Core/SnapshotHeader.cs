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
		public float FogDistance;
		public float FogHeight;
		public Color4 FogColor;
		public float Gravity;
		public float SunIntensity;

		public short		WeaponModel;

		public Color4 AmbientLevel;

		public void ClearHud ()
		{
			WeaponModel		=	0;

			for ( int i=0; i<HudState.Length; i++ ) {
				HudState[i]	=	0;
			}
		}

		public readonly short[] HudState = new short[(int)HudElement.Max];


		public void Write( BinaryWriter writer )
		{
			writer.Write( SunDirection );
			writer.Write( Turbidity );
			writer.Write( FogDistance );
			writer.Write( FogHeight );
			writer.Write( Gravity );
			writer.Write( SunIntensity );

			writer.Write( FogColor );
			writer.Write( AmbientLevel );

			writer.Write( WeaponModel );

			for ( int i=0; i<HudState.Length; i++ ) {
				writer.Write( HudState[i] );
			}
		}


		public void Read( BinaryReader reader, float lerpFactor )
		{
			SunDirection	=	reader.Read<Vector3>();
			Turbidity		=	reader.ReadSingle();
			FogDistance		=	reader.ReadSingle();
			FogHeight		=	reader.ReadSingle();
			Gravity			=	reader.ReadSingle();
			SunIntensity	=	reader.ReadSingle();

			FogColor		=	reader.Read<Color4>();
			AmbientLevel	=	reader.Read<Color4>();

			WeaponModel		=	reader.ReadInt16();

			for ( int i=0; i<HudState.Length; i++ ) {
				HudState[i]	=	reader.ReadInt16();
			}
		}
	}

}
