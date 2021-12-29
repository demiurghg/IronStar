using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using BEPUphysics.EntityStateManagement;
using System.Diagnostics;
using System.IO;
using Fusion;

namespace IronStar.ECS
{
	public class Transform : IComponent
	{
		public Vector3		Position		=	Vector3.Zero;
		public Quaternion	Rotation		=	Quaternion.Identity;
		public float		Scaling			=	1.0f;
		public Vector3		LinearVelocity	=	Vector3.Zero;
		public Vector3		AngularVelocity	=	Vector3.Zero;

		public Transform ()
		{
		}

		public Transform ( Vector3 p, Quaternion r )
		{
			Position	=	p;
			Rotation	=	r;
			Scaling		=	1;
		}

		public Transform ( Matrix t )
		{
			Vector3 s;
			t.Decompose( out s, out Rotation, out Position );
			Scaling = s.X;
		}

		public Transform ( Vector3 p, Quaternion r, Vector3 velocity )
		{
			Position		=	p;
			Rotation		=	r;
			Scaling			=	1;
			LinearVelocity	=	velocity;
		}

		public Transform ( Vector3 p, Quaternion r, float scaling, Vector3 linearVelocity, Vector3 angularVelocity )
		{
			Position		=	p;
			Rotation		=	r;
			Scaling			=	scaling;
			LinearVelocity	=	linearVelocity;
			AngularVelocity	=	angularVelocity;
		}

		public Transform ( Vector3 p, Quaternion r, float s )
		{
			Position	=	p;
			Rotation	=	r;
			Scaling		=	s;
		}

		public void Move( Vector3 position, Quaternion rotation, Vector3 linearVelocity, Vector3 angularVelocity )
		{
			this.Position			=	position;
			this.Rotation			=	rotation;
			this.LinearVelocity		=	linearVelocity;
			this.AngularVelocity	=	angularVelocity;
		}

		public void Move( MotionState motionState )
		{
			this.Position			=	MathConverter.Convert( motionState.Position );
			this.Rotation			=	MathConverter.Convert( motionState.Orientation );
			this.LinearVelocity		=	MathConverter.Convert( motionState.LinearVelocity );
			this.AngularVelocity	=	MathConverter.Convert( motionState.AngularVelocity );
		}

		public void Teleport( Vector3 position, Quaternion rotation, Vector3 linearVelocity, Vector3 angularVelocity )
		{
			this.Position			=	position;
			this.Rotation			=	rotation;
			this.LinearVelocity		=	linearVelocity;
			this.AngularVelocity	=	angularVelocity;
		}

		public Matrix TransformMatrix 
		{
			get 
			{ 
				return Matrix.Scaling( Scaling ) * Matrix.RotationQuaternion( Rotation ) * Matrix.Translation( Position ); 
			}
			set 
			{
				Vector3 s;
				value.Decompose( out s, out Rotation, out Position );
				Scaling = s.X;
			}
		}

		/*-----------------------------------------------------------------------------------------
		 *	Check transform :
		-----------------------------------------------------------------------------------------*/

		bool IsBad(float f)
		{
			return float.IsNaN(f) || float.IsInfinity(f);
		}

		bool IsBad(Vector3 v)
		{
			return IsBad(v.X) || IsBad(v.Y) || IsBad(v.Z);
		}

		bool IsBad(Quaternion q)
		{
			return IsBad(q.X) || IsBad(q.Y) || IsBad(q.Z) || IsBad(q.W) || q==Quaternion.Zero;
		}

		public void CheckTransform( string prefix, Entity e )
		{
			if (false
				|| IsBad(Position)
				|| IsBad(Rotation)
				|| IsBad(Scaling)
				|| IsBad(LinearVelocity)
				|| IsBad(AngularVelocity)
			)
			{
				Log.Warning("Bad {0} transform : {1}", prefix, e?.ToString() );
				Log.Warning("  Position        = " + (Position).ToString() );
				Log.Warning("  Rotation        = " + (Rotation).ToString() );
				Log.Warning("  Scaling         = " + (Scaling).ToString() );
				Log.Warning("  LinearVelocity  = " + (LinearVelocity).ToString() );
				Log.Warning("  AngularVelocity = " + (AngularVelocity).ToString() );
			}
				
			//if (IsBad( Scaling		)) Log.Warning("Bad transform {0}: {1} {2} = {3}", id, prefix, nameof(Scaling), Scaling ); 

			//if (IsBad( Position.X	)) Log.Warning("Bad transform {0}: {1} {2} = {3}", id, prefix, nameof(Position.X), Position.X ); 
			//if (IsBad( Position.Y	)) Log.Warning("Bad transform {0}: {1} {2} = {3}", id, prefix, nameof(Position.Y), Position.Y ); 
			//if (IsBad( Position.Z	)) Log.Warning("Bad transform {0}: {1} {2} = {3}", id, prefix, nameof(Position.Z), Position.Z ); 

			//if (IsBad( LinearVelocity.X	)) Log.Warning("Bad transform {0}: {1} {2} = {3}", id, prefix, nameof(LinearVelocity.X), LinearVelocity.X ); 
			//if (IsBad( LinearVelocity.Y	)) Log.Warning("Bad transform {0}: {1} {2} = {3}", id, prefix, nameof(LinearVelocity.Y), LinearVelocity.Y ); 
			//if (IsBad( LinearVelocity.Z	)) Log.Warning("Bad transform {0}: {1} {2} = {3}", id, prefix, nameof(LinearVelocity.Z), LinearVelocity.Z ); 

			//if (IsBad( AngularVelocity.X	)) Log.Warning("Bad transform {0}: {1} {2} = {3}", id, prefix, nameof(AngularVelocity.X), AngularVelocity.X ); 
			//if (IsBad( AngularVelocity.Y	)) Log.Warning("Bad transform {0}: {1} {2} = {3}", id, prefix, nameof(AngularVelocity.Y), AngularVelocity.Y ); 
			//if (IsBad( AngularVelocity.Z	)) Log.Warning("Bad transform {0}: {1} {2} = {3}", id, prefix, nameof(AngularVelocity.Z), AngularVelocity.Z ); 

			//if (IsBad( Rotation.X	)) Log.Warning("Bad transform {0}: {1} {2} = {3}", id, prefix, nameof(Rotation.X), Rotation.X ); 
			//if (IsBad( Rotation.Y	)) Log.Warning("Bad transform {0}: {1} {2} = {3}", id, prefix, nameof(Rotation.Y), Rotation.Y ); 
			//if (IsBad( Rotation.Z	)) Log.Warning("Bad transform {0}: {1} {2} = {3}", id, prefix, nameof(Rotation.Z), Rotation.Z ); 
			//if (IsBad( Rotation.W	)) Log.Warning("Bad transform {0}: {1} {2} = {3}", id, prefix, nameof(Rotation.W), Rotation.W ); 

			//if ( Rotation==Quaternion.Zero ) Log.Warning("Bad transform {0}: {1} {2} = {3}", id, prefix, nameof(Rotation), Rotation ); 
		}

		/*-----------------------------------------------------------------------------------------
		 *	IComponent implementation :
		-----------------------------------------------------------------------------------------*/

		public void Save( GameState gs, BinaryWriter writer )
		{
			writer.Write( Position			);
			writer.Write( Rotation			);
			writer.Write( Scaling			);
			writer.Write( LinearVelocity	);
			writer.Write( AngularVelocity	);
		}

		public void Load( GameState gs, BinaryReader reader )
		{
			Position		=	reader.ReadVector3();
			Rotation		=	reader.ReadQuaternion();
			Scaling			=	reader.ReadSingle();
			LinearVelocity	=	reader.ReadVector3();
			AngularVelocity	=	reader.ReadVector3();
		}

		public IComponent Clone()
		{
			return (IComponent)MemberwiseClone();
		}


		Vector3 Vector3SafeLerp( Vector3 a, Vector3 b, float factor )
		{
			return Vector3.Lerp( a, b, factor );

			const float eps = 1 / 512.0f;
			if (   MathUtil.WithinEpsilon( a.X, b.X, eps )
				&& MathUtil.WithinEpsilon( a.Y, b.Y, eps )
				&& MathUtil.WithinEpsilon( a.Z, b.Z, eps ) )
			{
				return b;
			}
			else
			{
				return Vector3.Lerp( a, b, factor );
			}
		}


		public IComponent Interpolate( IComponent previous, float dt, float factor )
		{
			var prev	=	(Transform)previous;
			
			var lv	= LinearVelocity;
			var av	= AngularVelocity;

			Vector3 p;
			Quaternion r;
			float s;

			if (prev!=null)
			{
				p	= Vector3SafeLerp ( prev.Position,	Position,	factor );
				r	= Quaternion.Slerp( prev.Rotation,	Rotation,	factor );
				s	= MathUtil	.Lerp ( prev.Scaling,	Scaling,	factor );
			}
			else
			{
				p	=	Position - ((1 - factor)*dt) * LinearVelocity;
				r	=	Rotation;
				s	=	Scaling;
			}

			return new Transform()
			{
				Position		=	p,
				Rotation		=	r,
				Scaling			=	s,
				LinearVelocity	=	lv,
				AngularVelocity	=	av,
			};
		}
	}
}
