using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion.Core.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using Fusion.Engine.Graphics.Scenes;
using Fusion.Engine.Audio;
using IronStar.Views;
using KopiLua;
using Fusion.Scripting;
using IronStar.ECS;

namespace IronStar.SFX2 
{
	[Flags]
	public enum RMFlags
	{
		FirstPointView,
	}

	public partial class RenderModel : Component
	{
		static readonly Scene EmptyScene = Scene.CreateEmptyScene();

		//	pure component data :
		string	scenePath;
		Matrix	transform;
		Color	color;
		float	intensity;
		RMFlags	rmFlags;

		//	operational data :
		Scene scene;

		Matrix[] globalTransforms;
		Matrix[] animSnapshot;
		RenderInstance[] meshInstances;


		public RenderModel ( string scenePath, Matrix transform, Color color, float intensity, RMFlags flags )
		{
			this.scenePath	=	scenePath	;
			this.transform	=	transform	;
			this.color		=	color		;
			this.intensity	=	intensity	;
		}


		public override void Added( GameState gs, Entity entity )
		{
			base.Added( gs, entity );
			LoadScene( gs );
		}


		public override void Removed( GameState gs, Entity entity )
		{
			base.Removed( gs, entity );
			UnloadScene( gs );
		}


		public override void Load( GameState gs, Stream stream )
		{
			base.Load( gs, stream );
		}


		public override void Save( GameState gs, Stream stream )
		{
			base.Save( gs, stream );
		}

		/*-----------------------------------------------------------------------------------------------
		 *	Transformation and animation :
		-----------------------------------------------------------------------------------------------*/

		public void SetTransform( Matrix worldMatrix )
		{
			UpdateInternal( worldMatrix, animSnapshot );
		}


		public Matrix ComputeWorldMatrix ()
		{
			var q	=	Entity.Rotation;
			var p	=	Entity.Position;

			var worldMatrix = Matrix.RotationQuaternion( q ) * Matrix.Translation( p );

			/*
			if ( fpvEnabled ) 
			{
				var playerCameraMatrix	=	modelManager.rw.Camera.CameraMatrix;
				worldMatrix				= 	playerCameraMatrix;
			}
			*/

			return worldMatrix;
		}
		

		void UpdateInternal ( Matrix worldMatrix, Matrix[] nodeTransforms )
		{
			if (scene==null || scene==EmptyScene) return;

			if (nodeTransforms==null) throw new ArgumentNullException("nodeTransforms");
			if (scene.Nodes.Count>nodeTransforms.Length) throw new ArgumentException("nodeTransforms.Length < nodeCount");

			var instGroup	=	rmFlags.HasFlag(RMFlags.FirstPointView) ? InstanceGroup.Weapon : InstanceGroup.Dynamic;
			var glowColor	=	color.ToColor4() * MathUtil.Exp2( intensity );

			for ( int i = 0; i<scene.Nodes.Count; i++ ) 
			{
				if (meshInstances[i]!=null) 
				{
					meshInstances[i].Group	=	instGroup;
					meshInstances[i].World	=	nodeTransforms[i] * transform * worldMatrix;
					meshInstances[i].Color	=	glowColor;
				}
			}
		}

		/*-----------------------------------------------------------------------------------------------
		 *	Scene management operations :
		-----------------------------------------------------------------------------------------------*/

		void LoadScene ( GameState gs )
		{
			var content		=	gs.GetService<ContentManager>();
			var rs			=	gs.GetService<RenderSystem>();
			
			if (string.IsNullOrWhiteSpace(scenePath)) 
			{
				scene	=	EmptyScene;
			} else 
			{
				scene	=	content.Load<Scene>( scenePath );
			}

			globalTransforms	=	new Matrix[ scene.Nodes.Count ];
			animSnapshot		=	new Matrix[ scene.Nodes.Count ];
			scene.ComputeAbsoluteTransforms( globalTransforms );
			scene.ComputeAbsoluteTransforms( animSnapshot );
			
			meshInstances	=	new RenderInstance[ scene.Nodes.Count ];

			for ( int i=0; i<scene.Nodes.Count; i++ ) 
			{
				var meshIndex = scene.Nodes[i].MeshIndex;
				
				if (meshIndex>=0) 
				{
					meshInstances[i]		= new RenderInstance( rs, scene, scene.Meshes[meshIndex] );
					meshInstances[i].Group	= InstanceGroup.Dynamic;
					meshInstances[i].Color	= Color4.Zero;
					rs.RenderWorld.Instances.Add( meshInstances[i] );
				}
				else 
				{
					meshInstances[i] = null;
				}
			}
		}


		public void UnloadScene(GameState gs)
		{
			var rs			=	gs.GetService<RenderSystem>();

			if (meshInstances!=null)
			{
				foreach ( var mesh in meshInstances )
				{
					rs.RenderWorld.Instances.Remove( mesh );
				}
			}
		}
	}
}
