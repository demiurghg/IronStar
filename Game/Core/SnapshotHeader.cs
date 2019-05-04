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

	public class SnapshotHeader : IStorable {

		public Vector3 SunDirection;
		public float Turbidity;
		public float FogDistance;
		public Color4 FogColor;
		public float Gravity;
		public float SunIntensity;
		public float SkyIntensity;


		public void Write( BinaryWriter writer )
		{
			writer.Write( SunDirection );
			writer.Write( Turbidity );
			writer.Write( FogDistance );
			writer.Write( Gravity );
			writer.Write( SunIntensity );
			writer.Write( SkyIntensity );
			writer.Write( FogColor );
		}


		public void Read( BinaryReader reader, float lerpFactor )
		{
			SunDirection	=	reader.Read<Vector3>();
			Turbidity		=	reader.ReadSingle();
			FogDistance		=	reader.ReadSingle();
			Gravity			=	reader.ReadSingle();
			SunIntensity	=	reader.ReadSingle();
			SkyIntensity	=	reader.ReadSingle();
			FogColor		=	reader.Read<Color4>();
		}
	}

}
