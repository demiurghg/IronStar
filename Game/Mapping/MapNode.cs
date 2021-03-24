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


		/*-----------------------------------------------------------------------------------------
		 *	Transformation stuff :
		-----------------------------------------------------------------------------------------*/

		Vector3 translation	=	Vector3.Zero;
		Vector3 scaling		=	Vector3.One;
		Quaternion rotation	=	Quaternion.Identity;

		[Browsable(false)]
		public Vector3 Translation 
		{
			get { return translation; }
			set { translation = value; }
		}


		[Browsable(false)]
		public Vector3 Scaling
		{
			get { return scaling; }
			set { scaling = value; }
		}


		[Browsable(false)]
		public Quaternion Rotation 
		{
			get { return rotation; }
			set { rotation = value; }
		}


		/*-----------------------------------------------------------------------------------------
		 *	Transformation components :
		-----------------------------------------------------------------------------------------*/

		[AECategory("Transform")]
		[AEDisplayName("Translate X")]
		public float TranslateX 
		{ 
			get { return translation.X; }
			set { translation.X = value; }
		}

		
		[AECategory("Transform")]
		[AEDisplayName("Translate Y")]
		public float TranslateY 
		{ 
			get { return translation.Y; }
			set { translation.Y = value; }
		}

		
		[AECategory("Transform")]
		[AEDisplayName("Translate Z")]
		public float TranslateZ 
		{
			get { return translation.Z; }
			set { translation.Z = value; }
		}

		
		[AECategory("Transform")]
		[AEDisplayName("Rotate Yaw")]
		[AESlider(-180,180,15,1)]
		public float RotateYaw 
		{
			get { return EulerAngles.RotationQuaternion(Rotation).Yaw.Degrees; }
			set 
			{
				var angles = EulerAngles.RotationQuaternion(Rotation);
				angles.Yaw.Degrees = value;
				rotation = angles.ToQuaternion();
			}
		}


		[AECategory("Transform")]
		[AEDisplayName("Rotate Pitch")]
		[AESlider(-180,180,15,1)]
		public float RotatePitch 
		{
			get { return EulerAngles.RotationQuaternion(Rotation).Pitch.Degrees; }
			set 
			{
				var angles = EulerAngles.RotationQuaternion(Rotation);
				angles.Pitch.Degrees = value;
				rotation = angles.ToQuaternion();
			}
		}


		[AECategory("Transform")]
		[AEDisplayName("Rotate Roll")]
		[AESlider(-180,180,15,1)]
		public float RotateRoll 
		{
			get { return EulerAngles.RotationQuaternion(Rotation).Roll.Degrees; }
			set 
			{
				var angles = EulerAngles.RotationQuaternion(Rotation);
				angles.Roll.Degrees = value;
				rotation = angles.ToQuaternion();
			}
		}


		/*-----------------------------------------------------------------------------------------
		 *	Transformation matricies :
		-----------------------------------------------------------------------------------------*/

		[Browsable(false)]
		[JsonIgnore]
		public Matrix GlobalTransform 
		{
			get 
			{
				//return ParentTransform * LocalTransform;
				var node = this;
				var transform = LocalTransform;
				var parent = node.Parent;

				while (parent!=null)
				{
					transform	= transform * parent.LocalTransform;
					parent		= parent.Parent;
				}

				return transform;//*/
			}
			set 
			{
				var global = GlobalTransform;
				var local  = LocalTransform;
				LocalTransform = value * Matrix.Invert(GlobalTransform) * LocalTransform;
			}
		}


		Matrix ParentTransform
		{
			get 
			{
				var transform = Matrix.Identity;
				var node = this.Parent;

				while (node!=null)
				{
					transform	= transform * node.LocalTransform;
					node		= node.Parent;
				}

				return transform;
			}
		}


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

		/*-----------------------------------------------------------------------------------------
		 *	ECS stuff :
		-----------------------------------------------------------------------------------------*/

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

		
		public virtual BoundingBox GetBoundingBox() 
		{
			return new BoundingBox( 2, 2, 2 ); 
		}
	}
}
