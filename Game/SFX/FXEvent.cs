using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using Fusion.Core.Utils;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using System.IO;

namespace IronStar.SFX 
{
	public class FXEvent 
	{
		public string FXName;

		/// <summary>
		/// FX Event source position.
		/// </summary>
		public Vector3 Origin;

		/// <summary>
		/// FX rotation
		/// </summary>
		public Quaternion Rotation;

		/// <summary>
		/// FX velocity
		/// </summary>
		public Vector3 Velocity;

		/// <summary>
		/// FX overall scale
		/// </summary>
		public float Scale;

		/// <summary>
		/// Indicates that given FX must be played in player's view/weapon space
		/// </summary>
		public bool IsFpv = false;

		/// <summary>
		/// Index of the joint to attach FX.
		/// </summary>
		public int JointId = -1;



		public FXEvent ()
		{
		}


		public Matrix TransformMatrix
		{
			get 
			{
				return Matrix.Scaling( Scale ) * Matrix.RotationQuaternion( Rotation ) * Matrix.Translation( Origin );
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="fxType"></param>
		/// <param name="position"></param>
		/// <param name="target"></param>
		/// <param name="orient"></param>
		public FXEvent ( string name, Vector3 origin, Vector3 velocity, Quaternion rotation )
		{
			this.FXName		=	name;
			this.Origin		=	origin;
			this.Velocity	=	velocity;
			this.Rotation	=	rotation;

			this.Scale		=	1;
			this.JointId	=	-1;
			this.IsFpv		=	false;
		}
	}
}
