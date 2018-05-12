using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Fusion.Core.Mathematics;
using IronStar.Core;
using Fusion.Engine.Graphics;
using IronStar.SFX;
using Fusion.Core.Shell;
using Fusion.Engine.Common;
using Fusion.Engine.Common;

namespace IronStar.Mapping {
	public abstract class MapNode {

		/// <summary>
		/// Indicates that map object or entity should be updated without
		/// </summary>
		protected bool dirty = true;

		/// <summary>
		/// Indicates that map object must be fully recreated
		/// </summary>
		protected bool hardDirty = true;


		/// <summary>
		/// 
		/// </summary>
		public MapNode ()
		{
		}



		[AECategory("Display")]
		public bool Visible { get; set; } = true;

		[AECategory("Display")]
		public bool Frozen { get; set; }

		float translateX  = 0;
		float translateY  = 0;
		float translateZ  = 0;
		float rotateYaw   = 0;
		float rotatePitch = 0;
		float rotateRoll  = 0;

		[AECategory("Transform")]
		[AEDisplayName("Translate X")]
		public float TranslateX { 
			get { return translateX; }
			set {
				if (translateX!=value) {
					dirty = true;
					translateX = value;
				}
			}
		}

		[AECategory("Transform")]
		[AEDisplayName("Translate Y")]
		public float TranslateY { 
			get { return translateY; }
			set {
				if (translateY!=value) {
					dirty = true;
					translateY = value;
				}
			}
		}

		[AECategory("Transform")]
		[AEDisplayName("Translate Z")]
		public float TranslateZ {
			get { return translateZ; }
			set {
				if (translateZ!=value) {
					dirty = true;
					translateZ = value;
				}
			}
		}

		[AECategory("Transform")]
		[AEDisplayName("Rotate Yaw")]
		[AEValueRange(-180,180,15,1)]
		public float RotateYaw {
			get { return rotateYaw; }
			set {
				if (rotateYaw!=value) {
					dirty = true;
					rotateYaw = value;
				}
			}
		}

		[AECategory("Transform")]
		[AEDisplayName("Rotate Pitch")]
		[AEValueRange(-180,180,15,1)]
		public float RotatePitch {
			get { return rotatePitch; }
			set {
				if (rotatePitch!=value) {
					dirty = true;
					rotatePitch = value;
				}
			}
		}

		[AECategory("Transform")]
		[AEDisplayName("Rotate Roll")]
		[AEValueRange(-180,180,15,1)]
		public float RotateRoll {
			get { return rotateRoll; }
			set {
				if (rotateRoll!=value) {
					dirty = true;
					rotateRoll = value;
				}
			}
		}



		/// <summary>
		/// Gets node's world transform matrix 
		/// </summary>
		[Browsable(false)]
		public Matrix WorldMatrix {
			get {
				return Matrix.RotationQuaternion( RotateQuaternion ) 
					* Matrix.Translation( TranslateVector );
			}
		}


		/// <summary>
		/// Gets and sets translation vector
		/// </summary>
		[Browsable(false)]
		public Vector3 TranslateVector {
			get {
				return new Vector3( TranslateX, TranslateY, TranslateZ );
			}
			set {
				TranslateX = value.X;
				TranslateY = value.Y;
				TranslateZ = value.Z;
			}
		}


		/// <summary>
		/// Gets and sets rotation quaternion
		/// </summary>
		[Browsable(false)]
		public Quaternion RotateQuaternion {
			get {
				return Quaternion.RotationYawPitchRoll( 
					MathUtil.DegreesToRadians( rotateYaw   ), 
					MathUtil.DegreesToRadians( rotatePitch ), 
					MathUtil.DegreesToRadians( rotateRoll  )
				);
			}
			set {
				var rotationMatrix = Matrix.RotationQuaternion( value );
				MathUtil.ToAnglesDeg( rotationMatrix, out rotateYaw, out rotatePitch, out rotateRoll );
			}
		}


		/// <summary>
		/// Updates	node state.
		/// Check dirty-flags and reset node if needed.
		/// </summary>
		/// <param name="gameTime"></param>
		public virtual void Update ( GameTime gameTime, GameWorld world )
		{
			if (dirty) {
				ResetNode(world);
				dirty = false;
			}
			if (hardDirty) {
				KillNode(world);
				SpawnNode(world);
				hardDirty = false;
			}
		}


		/// <summary>
		/// Creates instance of map object
		/// </summary>
		/// <returns></returns>
		public abstract void SpawnNode ( GameWorld world );

		/// <summary>
		/// Initiates entity activation
		/// </summary>
		public abstract void ActivateNode ();

		/// <summary>
		/// Initiates entity activation
		/// </summary>
		public abstract void UseNode ();

		/// <summary>
		/// Resets entity
		/// </summary>
		/// <param name="world"></param>
		public abstract void ResetNode ( GameWorld world );

		/// <summary>
		/// Eliminates object
		/// </summary>
		/// <param name="world"></param>
		public abstract void KillNode ( GameWorld world );

		/// <summary>
		/// Creates copy of current node without activation
		/// </summary>
		/// <returns></returns>
		public abstract MapNode DuplicateNode ();

		/// <summary>
		/// 
		/// </summary>
		/// <param name="dr"></param>
		public abstract void DrawNode ( GameWorld world, DebugRender dr, Color color, bool selected );
	}
}
