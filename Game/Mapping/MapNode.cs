using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Fusion.Core.Mathematics;
using Fusion.Core;
using Fusion.Engine.Graphics;
using IronStar.SFX;
using Fusion.Core.Shell;
using Fusion.Engine.Common;
using Newtonsoft.Json;
using IronStar.ECS;

namespace IronStar.Mapping {
	public abstract class MapNode {

		public Guid NodeGuid = Guid.NewGuid();

		/// <summary>
		/// Indicates that map object or entity should be updated without
		/// </summary>
		protected bool dirty = true;


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
		[JsonIgnore]
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
		[JsonIgnore]
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
		[JsonIgnore]
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


		[JsonIgnore]
		protected ECS.Entity ecsEntity = null;
		public virtual void SpawnNodeECS( GameState gs ) {}

		[JsonIgnore]
		public ECS.Entity EcsEntity { get { return ecsEntity; } }
		
		public virtual void KillNodeECS( GameState gs ) 
		{
			gs.Kill(ecsEntity); 
		}
		
		public virtual void ResetNodeECS( GameState gs )
		{
			KillNodeECS(gs);
			SpawnNodeECS(gs);
		}

		public bool HasEntity( ECS.Entity entity )
		{
			return ecsEntity!=null && ecsEntity==entity;
		}

		public abstract MapNode DuplicateNode ();

		public virtual BoundingBox GetBoundingBox() { return new BoundingBox( 2, 2, 2 ); }

	}
}
