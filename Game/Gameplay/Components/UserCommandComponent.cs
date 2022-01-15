using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.ECS;
using IronStar.Gameplay.Weaponry;

namespace IronStar.Gameplay
{
	public class UserCommandComponent : IComponent
	{
		public float Yaw  ;
		public float Pitch;
		public float Roll ;

		public float Move;
		public float Strafe;

		public UserAction Action;

		public WeaponType Weapon;

		public float DYaw;
		public float DPitch;

		public bool IsMoving { get { return Math.Abs(Move)>0.1f || Math.Abs(Strafe)>0.1f; } }
		public bool IsRunning { get { return MovementVector.Length() > 0.5f; } }
		public bool IsForward { get { return Move>=0; } }
		public bool IsStunned { get { return Action.HasFlag(UserAction.GestureStun); } }

		public void UpdateFromUserCommand ( float yaw, float pitch, float move, float strafe, UserAction action )
		{
			ResetControl();

			Yaw		=	yaw;
			Pitch	=	pitch;
			Roll	=	0;
			Action	=	action;

			Move	=	move;
			Strafe	=	strafe;

			if (action.HasFlag( UserAction.Weapon1 )) Weapon = WeaponType.Machinegun	;
			if (action.HasFlag( UserAction.Weapon2 )) Weapon = WeaponType.Machinegun2	;
			if (action.HasFlag( UserAction.Weapon3 )) Weapon = WeaponType.Shotgun		;
			if (action.HasFlag( UserAction.Weapon4 )) Weapon = WeaponType.Plasmagun		;
			if (action.HasFlag( UserAction.Weapon5 )) Weapon = WeaponType.RocketLauncher;
			if (action.HasFlag( UserAction.Weapon6 )) Weapon = WeaponType.Machinegun	;
			if (action.HasFlag( UserAction.Weapon7 )) Weapon = WeaponType.Railgun		;
			if (action.HasFlag( UserAction.Weapon8 )) Weapon = WeaponType.Machinegun	;
		}


		public void Save( GameState gs, BinaryWriter writer )
		{
			writer.Write( Yaw			);
			writer.Write( Pitch			);
			writer.Write( Roll			);
			writer.Write( Move			);
			writer.Write( Strafe		);
			writer.Write( (int)Action	);
			writer.Write( (int)Weapon	);
			writer.Write( DYaw			);
			writer.Write( DPitch		);
		}


		public void Load( GameState gs, BinaryReader reader )
		{
			Yaw		=	reader.ReadSingle();
			Pitch	=	reader.ReadSingle();
			Roll	=	reader.ReadSingle();
			Move	=	reader.ReadSingle();
			Strafe	=	reader.ReadSingle();
			Action	=	(UserAction)reader.ReadInt32();
			Weapon	=	(WeaponType)reader.ReadInt32();
			DYaw	=	reader.ReadSingle();
			DPitch	=	reader.ReadSingle();
		}


		public IComponent Clone()
		{
			return (IComponent)MemberwiseClone();
		}

		public IComponent Interpolate( IComponent previous, float dt, float factor )
		{
			return Clone();
		}

		public void ResetControl()
		{
			Weapon	=	WeaponType.None;
			Action	=	UserAction.None;
			Move	=	0;
			Strafe	=	0;
		}

		public void SetAnglesFromQuaternion( Quaternion q )
		{
			var m = Matrix.RotationQuaternion(q);
			float yaw, pitch, roll;
			m.ToAngles( out yaw, out pitch, out roll );

			Yaw		=	yaw;
			Pitch	=	pitch;
			Roll	=	roll;

			if (float.IsNaN(Yaw)	|| float.IsInfinity(Yaw)	) Yaw	= 0;
			if (float.IsNaN(Pitch)	|| float.IsInfinity(Pitch)	) Pitch	= 0;
			if (float.IsNaN(Roll)	|| float.IsInfinity(Roll)	) Roll	= 0;
		}

		private Quaternion Rotation
		{
			get { return Quaternion.RotationYawPitchRoll( Yaw, Pitch, Roll ); }
		}

		public Matrix RotationMatrix 
		{
			get { return Matrix.RotationQuaternion( Rotation ); }
		}

		public Vector3 MovementVector
		{
			get 
			{ 
				var m = Matrix.RotationYawPitchRoll( Yaw, 0, 0 );

				return m.Forward * Move 
					+ m.Right * Strafe;
			}
		}


		public void ComputeMoveAndStrafe( Vector3 originPoint, Vector3 targetPoint, float factor )
		{
			var matrix			=	Matrix.RotationYawPitchRoll( Yaw, 0, 0 );
			var moveVector		=	matrix.Forward;
			var strafeVector	=	matrix.Right;
			var direction		=	Vector3.Normalize( targetPoint - originPoint );
			
			Move	=	Vector3.Dot( moveVector, direction ) * factor;
			Strafe	=	Vector3.Dot( strafeVector, direction ) * factor;
		}

		
		public float RotateTo( Vector3 originPoint, Vector3 targetPoint, float maxYawRate, float maxPitchRate )
		{
			if (originPoint==targetPoint) 
			{
				return 0;
			}
			
			var dir				=	( targetPoint - originPoint ).Normalized();
			var desiredYaw		=	(float)Math.Atan2( -dir.X, -dir.Z );
			var desiredPitch	=	(float)Math.Asin( dir.Y );

			var shortestYaw		=	MathUtil.ShortestAngle( Yaw,	desiredYaw, maxYawRate );
			var shortestPitch	=	MathUtil.ShortestAngle( Pitch, desiredPitch, maxPitchRate );

			Yaw		=	Yaw		+ shortestYaw;
			Pitch	=	Pitch	+ shortestPitch;

			return Math.Abs( shortestYaw ) + Math.Abs( shortestPitch );
		}
	}
}
