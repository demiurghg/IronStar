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
	[Flags]
	public enum StepEvent
	{
		None		=	0x000,
		Jumped		=	0x001,
		Landed		=	0x002,
		LeftStep	=	0x004,
		RightStep	=	0x008,
		Crouched	=	0x010,
		Standed		=	0x020,
		RecoilLight	=	0x040,
		RecoilHeavy	=	0x080,
	}

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

		public WeaponState	WeaponState;

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

			writer.Write( (int)WeaponState	);

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

			WeaponState			=	(WeaponState)reader.ReadInt32();

			Counter				=	reader.ReadInt32();
			StepTimer			=	reader.ReadSingle();
			StepFraction		=	reader.ReadSingle();
			HasTraction			=	reader.ReadBoolean();
			IsCrouching			=	reader.ReadBoolean();

			GroundVelocity		=	reader.Read<Vector3>();
			FallVelocity		=	reader.ReadSingle();
			LocalAcceleration	=	reader.Read<Vector3>();
		}

		public static StepEvent DetectEvents( StepComponent next, StepComponent prev )
		{
			StepEvent events = StepEvent.None;

			if (prev!=null && next!=null)
			{
				if (!next.HasTraction && prev.HasTraction) events |= StepEvent.Jumped;
				if (next.HasTraction && !prev.HasTraction) events |= StepEvent.Landed;

				if (!next.IsCrouching && prev.IsCrouching) events |= StepEvent.Standed;
				if (next.IsCrouching && !prev.IsCrouching) events |= StepEvent.Crouched;

				if (next.Counter!=prev.Counter)
				{
					if (MathUtil.IsEven(next.Counter)) events |= StepEvent.RightStep;
					if (MathUtil.IsOdd (next.Counter)) events |= StepEvent.LeftStep;
				}

				if (next.WeaponState!=prev.WeaponState) 
				{
					if (next.WeaponState==WeaponState.Cooldown || next.WeaponState==WeaponState.Cooldown2)
					{
						events	=	StepEvent.RecoilLight;
					}
				}
			}

			return events;
		}

		public IComponent Clone()
		{
			return (IComponent)MemberwiseClone();
		}

		public IComponent Interpolate( IComponent previous, float dt, float factor )
		{
			return Clone();
		}
	}
}
