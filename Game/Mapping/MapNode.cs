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
using Fusion.Widgets.Advanced;
using Fusion.Core.Extensions;

namespace IronStar.Mapping 
{
	public abstract class MapNode 
	{
		public string Name;

		//public MapNodeCollection Children {	get { return children; } }
		//readonly MapNodeCollection children;

		/// <summary>
		/// Indicates that map object or entity should be updated without
		/// </summary>
		protected bool dirty = true;

		/// <summary>
		/// Gets and sets parent node
		/// </summary>
		public MapNode Parent 
		{ 
			get { return parent; }
			set
			{
				if (parent!=value)
				{
					parent?.children.Remove(this);
					parent = value;
					parent?.children.Add(this);
				}
			}
		}

		MapNode parent = null;
		MapNodeCollection children = new MapNodeCollection();

		[JsonIgnore]
		public IEnumerable<MapNode> Children { get { return children; } }


		/// <summary>
		/// 
		/// </summary>
		public MapNode ()
		{
			Name = GenerateUniqueName();
		}


		public static string GenerateUniqueName()
		{
			return Guid.NewGuid().ToString();
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
		[AESlider(-180,180,15,1)]
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
		[AESlider(-180,180,15,1)]
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
		[AESlider(-180,180,15,1)]
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
		public Matrix GlobalTransform 
		{
			get 
			{
				var transform = Matrix.Identity;
				var node = this;

				while (node!=null)
				{
					transform	= transform * node.LocalTransform;
					node		= node.Parent;
				}

				return transform;
			}
		}


		/// <summary>
		/// Gets node's world transform matrix 
		/// </summary>
		[Browsable(false)]
		[JsonIgnore]
		public Matrix LocalTransform 
		{
			get 
			{
				return Matrix.Scaling( Scaling ) * Matrix.RotationQuaternion( Rotation ) * Matrix.Translation( Translation );
			}
			set 
			{
				Vector3 s, t;
				Quaternion r;
				value.Decompose( out s, out r, out t );
				Translation	=	t;
				Scaling		=	s;
				Rotation	=	r;
			}
		}




		/// <summary>
		/// Gets and sets translation vector
		/// </summary>
		[Browsable(false)]
		public Vector3 Translation {
			get {
				return new Vector3( TranslateX, TranslateY, TranslateZ );
			}
			set {
				TranslateX = value.X;
				TranslateY = value.Y;
				TranslateZ = value.Z;
			}
		}


		[Browsable(false)]
		public Vector3 Scaling
		{
			get;
			set;
		} = Vector3.One;


		/// <summary>
		/// Gets and sets rotation quaternion
		/// </summary>
		[Browsable(false)]
		public Quaternion Rotation {
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
		
		public virtual void ResetNodeECS( GameState gs, bool recursive = true )
		{
			KillNodeECS(gs);
			SpawnNodeECS(gs);

			if (recursive)
			{
				foreach (var child in Children)
				{
					child?.ResetNodeECS(gs);
				}
			}
		}

		public bool HasEntity( ECS.Entity entity )
		{
			return ecsEntity!=null && ecsEntity==entity;
		}

		public virtual MapNode DuplicateNode ()
		{
			var node = (MapNode)Activator.CreateInstance( GetType() );

			Misc.CopyProperties( this, node );

			node.Name = Guid.NewGuid().ToString();

			return node;
		}

		public virtual BoundingBox GetBoundingBox() { return new BoundingBox( 2, 2, 2 ); }

	}
}
