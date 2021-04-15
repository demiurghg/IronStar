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
using IronStar.Editor;

namespace IronStar.Mapping 
{
	public abstract class MapNode 
	{
		[AECategory("Node")]
		public string Name 
		{ 
			get { return name; }
			set
			{
				if (name!=value)
				{
					name = value;

					if (MapEditor.Instance!=null)
					{
						name = MapEditor.Instance.GetUniqueName(this);
					}
				}
			}
		}
		string name;

		protected bool dirty = true;

		[AECategory("Node")]
		public bool Visible { get; set; } = true;

		[AECategory("Node")]
		public bool Frozen { get; set; }


		public MapNode ()
		{
			Name = this.GetType().Name + "0";
		}


		[Obsolete("deprecated", true)]
		public static string GenerateUniqueName()
		{
			return Guid.NewGuid().ToString();
		}


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
		public Matrix Transform 
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


		public virtual void ResetNodeECS( GameState gs )
		{
			KillNodeECS(gs);
			SpawnNodeECS(gs);
		}


		public bool HasEntity( ECS.Entity entity )
		{
			return ecsEntity!=null && ecsEntity==entity;
		}

		
		public virtual MapNode DuplicateNode ()
		{
			var node = (MapNode)Activator.CreateInstance( GetType() );

			Misc.CopyProperties( this, node );

			return node;
		}

		
		public virtual BoundingBox GetBoundingBox( GameState gs ) 
		{
			return new BoundingBox( 2, 2, 2 ); 
		}
	}
}
