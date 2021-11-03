using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using IronStar.ECS;

namespace IronStar.Gameplay.Components
{
	public class StepComponent : IComponent
	{
		public bool		Jumped;
		public bool		Landed;
		public bool		LeftStep;
		public bool		RightStep;
		public bool		Crouched;
		public bool		Standed;
		public bool		RecoilLight;
		public bool		RecoilHeavy;

		public int		Counter;
		public float	StepTimer;
		public float	StepFraction;
		public bool		HasTraction;
		public bool		IsCrouching;

		public Vector3	GroundVelocity;
		public float	FallVelocity;
		public Vector3	LocalAcceleration;

		public bool		IsWalkingOrRunning { get { return HasTraction && GroundVelocity.Length() > 0.125f; } }

		/*-----------------------------------------------------------------------------------------
		 *	IComponent implementation :
		-----------------------------------------------------------------------------------------*/

		public void Save( GameState gs, BinaryWriter writer )
		{
			writer.Write( Jumped			);	
			writer.Write( Landed			);	
			writer.Write( LeftStep			);
			writer.Write( RightStep			);
			writer.Write( Crouched			);
			writer.Write( Standed			);	
			writer.Write( RecoilLight		);	
			writer.Write( RecoilHeavy		);	

			writer.Write( Counter			);	
			writer.Write( StepTimer			);
			writer.Write( StepFraction		);
			writer.Write( HasTraction		);	
			writer.Write( IsCrouching		);	

			writer.Write( GroundVelocity	);	
			writer.Write( FallVelocity		);
			writer.Write( LocalAcceleration	);
		}

		public void Load( GameState gs, BinaryReader reader )
		{
			Jumped				=	reader.ReadBoolean();
			Landed				=	reader.ReadBoolean();
			LeftStep			=	reader.ReadBoolean();
			RightStep			=	reader.ReadBoolean();
			Crouched			=	reader.ReadBoolean();
			Standed				=	reader.ReadBoolean();
			RecoilLight			=	reader.ReadBoolean();
			RecoilHeavy			=	reader.ReadBoolean();

			Counter				=	reader.ReadInt32();
			StepTimer			=	reader.ReadSingle();
			StepFraction		=	reader.ReadSingle();
			HasTraction			=	reader.ReadBoolean();
			IsCrouching			=	reader.ReadBoolean();

			GroundVelocity		=	reader.Read<Vector3>();
			FallVelocity		=	reader.ReadSingle();
			LocalAcceleration	=	reader.Read<Vector3>();
		}

		public IComponent Clone()
		{
			return (IComponent)MemberwiseClone();
		}

		public IComponent Interpolate( IComponent previous, float factor )
		{
			return Clone();
		}
	}
}
