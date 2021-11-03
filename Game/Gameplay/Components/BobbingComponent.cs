using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.ECS;

namespace IronStar.Gameplay.Components
{
	public class BobbingComponent : IComponent
	{
		public BobbingComponent() : this(0,0,0,0)
		{
		}

		public BobbingComponent( float yaw, float pitch, float roll, float up )
		{
			BobYaw		=	yaw;
			BobPitch	=	pitch;
			BobRoll		=	roll;
			BobUp		=	up;
		}
		
		public float BobYaw		;
		public float BobPitch	;
		public float BobRoll	;
		public float BobUp		;

		public void Save( GameState gs, BinaryWriter writer )
		{
			writer.Write( BobYaw	);
			writer.Write( BobPitch	);
			writer.Write( BobRoll	);
			writer.Write( BobUp		);
		}

		public void Load( GameState gs, BinaryReader reader )
		{
			BobYaw		=	reader.ReadSingle();
			BobPitch	=	reader.ReadSingle();
			BobRoll		=	reader.ReadSingle();
			BobUp		=	reader.ReadSingle();
		}

		public IComponent Clone()
		{
			return new BobbingComponent( BobYaw, BobPitch, BobRoll, BobUp );
		}

		public IComponent Interpolate( IComponent previous, float factor )
		{
			var prev	=	(BobbingComponent)previous;
			var yaw		=	MathUtil.Lerp( prev.BobYaw	, BobYaw	, factor );
			var pitch	=	MathUtil.Lerp( prev.BobPitch, BobPitch	, factor );
			var roll	=	MathUtil.Lerp( prev.BobRoll	, BobRoll	, factor );
			var up		=	MathUtil.Lerp( prev.BobUp	, BobUp		, factor );

			return new BobbingComponent( yaw, pitch, roll, up );
		}
	}
}
