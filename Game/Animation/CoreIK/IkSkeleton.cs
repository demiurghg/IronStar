using System;
using System.Collections.Generic;
using System.Linq;
using Fusion.Core.Mathematics;
using Fusion.Engine.Graphics;

namespace CoreIK {
	public class IkSkeleton {

		protected	Model			model;
		protected	SkinningData	skinningData;
		protected	DebugRender		debugRender, dr;
		protected	Matrix[]		bones;
		public		Matrix[]		globalBones;
		public		Matrix[]		Bones { get { return bones; } }

		public		SkinningData	SkinningData { get { return skinningData; } }

		public		DebugRender		DebugRender { get { return dr; } }

		/// <summary>
		/// </summary>
		/// <param name="humanModel"></param>
		/// <param name="debugRender"></param>
		/// <returns></returns>
		public IkSkeleton ( Model humanModel, DebugRender debugRender )
		{
			this.model				=	humanModel;
			skinningData			=	(SkinningData)humanModel.Tag;
			dr = this.debugRender	=	debugRender;

			bones		=	new Matrix[skinningData.Bones.Count];
			globalBones	=	new Matrix[skinningData.Bones.Count];
			for (int i=0; i<bones.Length; i++) {
				bones[i] = Matrix.Identity;
				globalBones[i] = skinningData.Bones[i].BindPose;
			}
		}


		/// <summary>
		/// returns global vector connecting two specified bones
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <returns></returns>
		public Vector3 GlobalBoneToBoneVector ( string from, string to )
		{
			var p0 = skinningData.Bones[ from ].BindPosition;
			var p1 = skinningData.Bones[ to ].BindPosition;
			return p1 - p0;
		}


		/// <summary>
		/// Extracts single childed ikBone from skeleton.
		/// </summary>
		/// <param name="boneName">Bone name to extract</param>
		/// <param name="childBoneName">Child bone name, which indicate local forward bone direction </param>
		/// <param name="globalBoneUp">Local up vector of the bone in global coordiantes</param>
		/// <returns></returns>
		public IkBone	ExtractIkBone	( string boneName, string childBoneName, Vector3 globalBoneUp )
		{
			var index		=	skinningData.Bones.IndexOf( boneName );
			var localUp		=	Vector3.TransformNormal( globalBoneUp, skinningData.Bones[ boneName ].BindPoseInv );
			var localFwd	=	skinningData.Bones[ childBoneName ].LocalBindPose.TranslationVector;
			var localOrigin	=	skinningData.Bones[ boneName ].LocalBindPose.TranslationVector;
			var length		=	localFwd.Length();

			return new IkBone( index, localFwd, localUp, localOrigin, length );
		}


		/// <summary>
		/// Extracts zero childed ikBone from skeleton.
		/// </summary>
		/// <param name="boneName">Bone name to extract</param>
		/// <param name="globalBoneFwd">Local forward vector of the bone in global coordiantes </param>
		/// <param name="globalBoneUp">Local up vector of the bone in global coordiantes</param>
		/// <returns></returns>
		public IkBone	ExtractIkBone	( string boneName, Vector3 globalBoneFwd, Vector3 globalBoneUp )
		{
			var index		=	skinningData.Bones.IndexOf( boneName );
			var localUp		=	Vector3.TransformNormal( globalBoneUp, skinningData.Bones[ boneName ].BindPoseInv );
			var localFwd	=	Vector3.TransformNormal( globalBoneFwd, skinningData.Bones[ boneName ].BindPoseInv );
			var localOrigin	=	skinningData.Bones[ boneName ].LocalBindPose.TranslationVector;
			var length		=	localFwd.Length();

			return new IkBone( index, localFwd, localUp, localOrigin, length );
		}


		/// <summary>
		/// Produces matricies for hardware skinning and stores them in bones
		/// </summary>
		/// <returns></returns>
		public void GenerateSkinningTransforms () 
		{
			for (int i=0; i<bones.Length; i++) {
				bones[i] = skinningData.Bones[i].BindPoseInv * globalBones[i];
			}
		}


		/// <summary>
		/// Strictly checks that specified parent and child
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="child"></param>
		/// <returns></returns>
		public void CheckBoneHierarchy ( string parent, string child )
		{
			ModelBone p = model.Bones[parent];
			ModelBone c = model.Bones[child];
			if (c.Parent!=p) {
				throw new Exception( "model must have following bone hierarchy: " + child + "->" + parent );
			}
		}

		public virtual void Draw(Matrix view, Matrix projection)
		{
			//Vector3[] positions = new Vector3[Bones.Count()];
			//Quaternion[] orients = new Quaternion[Bones.Count()];

			//for (int i = 0; i < Bones.Count(); i++)
			//{
			//	positions[i] = Bones[i].TranslationVector;
			//	orients[i] = Quaternion.CreateFromRotationMatrix(Bones[i]);
			//}

			//foreach (ModelMesh mesh in model.Meshes)
			//{
			//	foreach (Effect effect in mesh.Effects)
			//	{
			//		//effect.Parameters["Bones"].SetValue(humanSkeleton.Bones);
			//		effect.Parameters["View"].SetValue(view);
			//		effect.Parameters["Projection"].SetValue(projection);
			//		effect.Parameters["Bones_pos"].SetValue(positions);
			//		effect.Parameters["Bones_rot"].SetValue(orients);

			//	}
			//	mesh.Draw();
			//}
		}


		/// <summary>
		/// Transforms all downscending children using their local bind transforms
		/// </summary>
		/// <param name="parentBone">starting bone for recursive descending</param>
		/// <param name="parentTransform">global transform of parent bone</param>
		public void TransformChildren ( string parentBone, Matrix parentTransform ) 
		{
			int parentIdx = skinningData.Bones.IndexOf( parentBone );
			foreach (var bone in skinningData.Bones.Where( b=> b.ParentIdx==parentIdx )) {

				globalBones[ bone.Index ] = bone.LocalBindPose * parentTransform;
				TransformChildren( bone.Name, globalBones[ bone.Index ] );
			}
		}


		public void TransformBone ( string boneName, Matrix transform )
		{
			int idx = skinningData.Bones.IndexOf( boneName );
			globalBones[ idx ] = transform;
		}


		/// <summary>
		/// Exact solution for two-bone IK system
		/// </summary>
		/// <param name="origin"></param>
		/// <param name="target"></param>
		/// <param name="bendDir"></param>
		/// <param name="boneLengthA"></param>
		/// <param name="boneLengthB"></param>
		/// <param name="bendPos"></param>
		/// <param name="hitPos"></param>
		/// <returns></returns>
		public void SolveTwoBones ( Vector3 origin, Vector3 target, Vector3 initBendDir, float boneLengthA, float boneLengthB, 
									 out Vector3 bendPos, out Vector3 hitPos, out Vector3 bendDir, float softness=1 )
		{
			dr.DrawVector( origin, initBendDir, Color.White, 0.25f );

			Vector3 targetDir = target - origin;
			Vector3 normLegDir = targetDir;
			normLegDir.Normalize();

			initBendDir.Normalize();

			float c = targetDir.Length();
			float a = boneLengthA * softness;
			float b = boneLengthB * softness;

			//c = MathUtil.Clamp( c, 0, (a+b) * softness );

            Vector3 t = Vector3.Cross(normLegDir, initBendDir);
            t.Normalize();
            bendDir	= Vector3.Cross(t, normLegDir);
            bendDir.Normalize();

			if (c>(a+b)) {
				bendPos = origin + a * normLegDir;
				hitPos = origin + (a+b) * normLegDir;
			} else {
				float p  = 0.5f * (a+b+c);
				float s  = (float)Math.Sqrt( p * (p-a) * (p-b) * (p-c) ); // Heron's formula of triangle area
				float d  = 2*s / c;
				float ap = (float)Math.Sqrt( a*a - d*d );
				bendPos = origin + ap * normLegDir + bendDir * d;
				hitPos = target;
			}

			Color half = new Color(1,1,1, 0.5f);
			Color full = Color.White;
			dr.DrawPoint( origin, 0.2f, full );
			dr.DrawPoint( target, 0.2f, full );
			dr.DrawLine( origin, target, half );
			dr.DrawVector( origin, bendDir, full, 0.5f );

			dr.DrawPoint( bendPos, 0.1f, full );
			dr.DrawPoint( hitPos, 0.1f, full );
			dr.DrawLine( origin, bendPos, half );
			dr.DrawLine( bendPos, hitPos, half );
			//dr.DrawL
		}


		/// <summary>
		/// Aims local -Z axis along forward-vector and local Y along up-vector
		/// </summary>
		/// <param name="forward">Global forward-vector</param>
		/// <param name="up">Global up-vector</param>
		/// <returns></returns>
		public static Matrix	AimBasis ( Vector3 forward, Vector3 up, Vector3 position )
        {
			Matrix result = new Matrix();
            Vector3 right;
            
            // Normalize forward vector
            forward.Normalize();
            
            // Calculate right vector 
            Vector3.Cross(ref forward, ref up, out right);
            right.Normalize();
            
            // Recalculate up vector
            Vector3.Cross(ref right, ref forward, out up);
            Vector3.Normalize(ref up, out up);

            result.M11 = right.X;
            result.M12 = right.Y;
            result.M13 = right.Z;
            
            result.M21 = up.X;
            result.M22 = up.Y;
            result.M23 = up.Z;
            
            result.M31 = -forward.X;
            result.M32 = -forward.Y;
            result.M33 = -forward.Z;

			result.M44 = 1;

			result.TranslationVector = position;

			return result;
        }


		/// <summary>
		/// Aims spicified local forward- and up-vector 
		/// along global forward- and up-vectors
		/// </summary>
		/// <param name="localFwd"></param>
		/// <param name="localUp"></param>
		/// <param name="globalFwd"></param>
		/// <param name="globalUp"></param>
		/// <param name="globalPosition"></param>
		/// <returns></returns>
		public static Matrix	AimCustomBasis ( Vector3 localFwd, Vector3 localUp, Vector3 globalFwd, Vector3 globalUp, Vector3 globalPosition )
		{
			Matrix local	= AimBasis( localFwd, localUp, Vector3.Zero );
			Matrix target	= AimBasis( globalFwd, globalUp, Vector3.Zero );
			Matrix result	= Matrix.Transpose(local) * target;
			result.TranslationVector = globalPosition;
			return result;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="localFwd"></param>
		/// <param name="localUp"></param>
		/// <param name="globalFwd"></param>
		/// <param name="globalUp"></param>
		/// <param name="globalPosition"></param>
		/// <param name="postXForm"></param>
		/// <returns></returns>
		public static Matrix	AimCustomBasis ( Vector3 localFwd, Vector3 localUp, Vector3 globalFwd, Vector3 globalUp, Vector3 globalPosition, Matrix postXForm )
		{
			Matrix local	= AimBasis( localFwd, localUp, Vector3.Zero );
			Matrix target	= AimBasis( globalFwd, globalUp, Vector3.Zero );
			Matrix result	= Matrix.Transpose(local) * postXForm * target;
			result.TranslationVector = globalPosition;
			return result;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="axis"></param>
		/// <param name="point"></param>
		/// <param name="center"></param>
		/// <param name="radius"></param>
		/// <param name="solution"></param>
		/// <returns></returns>
		public static bool	RailConstraintMaxDist ( Vector3 axis, Vector3 point, Vector3 center, float radius, out Vector3 solution, out float offset )
		{
			solution = point;
			offset	 = 0;
			axis.Normalize();

			var r	=	radius;
			var	cp	=	point - center;
			var l2	=	cp.LengthSquared();
			var h	=	Vector3.Dot( axis, cp );	//	cp projection length
			var d2	=	l2 - h*h;

			if (l2 < r * r ) {
				return true;
			}

			if ( d2 > r * r ) {
				return false;
			}

			var	hs	=	(float)Math.Sqrt( r*r - d2 );
			offset	=	Math.Abs(h) - hs;

			offset	*=	Math.Sign( -h );

			solution	=	point + offset * axis;

			return true;
		}
	}
}
	