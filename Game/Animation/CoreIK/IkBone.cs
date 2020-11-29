using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fusion.Core.Mathematics;

namespace CoreIK {
	/// <summary>
	/// Represents single- or zero-childed bone.
	/// Provides simple aiming routines.
	/// Straining routines operate with	dimensionless units.
	/// </summary>
	public class IkBone {

		public int		Index;			///	Index of the bone in skinning data
		public Vector3	LocalFwd;		///	
		public Vector3	LocalUp;
		public Vector3	LocalOrigin;
		public float	Length;

		public Matrix	GlobalMatrix { get; private set; }

		
		public IkBone ( int index, Vector3 localFwd, Vector3 localUp, Vector3 localOrigin, float length )
		{
			GlobalMatrix		=	new Matrix();
			this.Index			=	index;
			this.LocalFwd		=	localFwd;
			this.LocalUp		=	localUp;
			this.LocalOrigin	=	localOrigin;
			this.Length			=	length;
		}

		
		public Matrix	Aim	( Vector3 origin, Vector3 fwd, Vector3 up, IkSkeleton skeleton=null ) 
		{
			GlobalMatrix = IkSkeleton.AimCustomBasis( LocalFwd, LocalUp, fwd, up, origin );
			if (skeleton!=null) {
				skeleton.globalBones[ Index ] = GlobalMatrix;
			}
			return GlobalMatrix;
		}


		public void SetGlobalMatrix ( Matrix globalMatrix, IkSkeleton skeleton ) 
		{
			GlobalMatrix = globalMatrix;
			skeleton.globalBones[ Index ] = GlobalMatrix;
		}


		public Matrix	Aim	( Vector3 origin, Vector3 fwd, Vector3 up, Matrix postTransform, IkSkeleton skeleton=null ) 
		{
			GlobalMatrix = IkSkeleton.AimCustomBasis( LocalFwd, LocalUp, fwd, up, origin, postTransform );

			if (skeleton!=null) {
				skeleton.globalBones[ Index ] = GlobalMatrix;
			}
			return GlobalMatrix;
		}


		public Vector3	GetGlobalPoint ( Vector3 localPoint ) 
		{
			return Vector3.Transform( localPoint, GlobalMatrix );
		}


		public Vector3	GetGlobalVector ( Vector3 localVector )
		{
			return Vector3.TransformNormal( localVector, GlobalMatrix );
		}


		private Vector3 totalForce;
		private Vector3 totalTorque;


		float Sigmoid ( float x ) {
			return x / (1 + Math.Abs(x));
		}

		
		public void AddStrain ( Vector3 force, Vector3 globalPoint ) {
			var torque	=	Vector3.Cross( globalPoint - GlobalMatrix.TranslationVector, force );
			totalForce	+=	force;
			totalTorque	+=	torque;
		}

		
		public Matrix SolveStraining ( Matrix initialBasis, float linearPlasticity, float angularPlasticity ) {
			float	torqueMagn	=	totalTorque.Length();
			Vector3 torqueAxis	=	torqueMagn==0 ? Vector3.ForwardRH : totalTorque / torqueMagn;

			Matrix	matrix		=	Matrix.RotationAxis( torqueAxis, MathUtil.PiOverTwo * Sigmoid( torqueMagn * angularPlasticity ) );
			matrix.TranslationVector	=	totalForce * linearPlasticity;	

			totalForce			=	Vector3.Zero;
			totalTorque			=	Vector3.Zero;
			GlobalMatrix		=	matrix * initialBasis;
			return GlobalMatrix;
		}

	}
}
