﻿using System;
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
	public partial class RenderModelInstance : DisposableBase
	{
		static readonly Scene EmptyScene = Scene.CreateEmptyScene();

		RenderSystem rs;
		Scene scene;
		SceneView<RenderInstance> sceneView;
		Matrix preTransform;
		Matrix cameraTransform;
		readonly string fpvCameraNode;
		readonly bool fpvEnabled;

		/// <summary>
		/// Indicates that given model is used for first-person-view :
		/// </summary>
		public bool IsFPVModel { get { return fpvEnabled; } }

		/// <summary>
		/// Gets render model's scene
		/// </summary>
		public Scene Scene { get { return scene; } }


		/// <summary>
		/// Gets model's pretransform matrix 
		/// </summary>
		public Matrix PreTransform
		{
			get { return preTransform; }
		}


		/// <summary>
		/// Creates instance of render model
		/// </summary>
		public RenderModelInstance ( GameState gs, RenderModel rm, Matrix tm, string fpvCameraNode = null )
		{
			this.fpvCameraNode	=	fpvCameraNode;
			fpvEnabled			=	!string.IsNullOrEmpty(fpvCameraNode);

			var content		=	gs.GetService<ContentManager>();
			rs				=	gs.GetService<RenderSystem>();

			scene		=	string.IsNullOrWhiteSpace(rm.scenePath) ? Scene.Empty : content.Load( rm.scenePath, Scene.Empty );
			
			sceneView	=	new SceneView<RenderInstance>( scene, 
							mesh => new RenderInstance( rs, scene, mesh ),
							node => rm.AcceptVisibleNode( node )
							);

			preTransform	=	rm.transform;
			cameraTransform	=	Matrix.Identity;

			sceneView.ForEachMesh( mesh => {
				mesh.Group	= rm.UseLightMap ? InstanceGroup.Static : InstanceGroup.Kinematic;
				mesh.Color	= Color4.Zero;
				mesh.LightMapGuid = rm.lightmapGuid;
				mesh.LightMapSize = rm.lightmapSize;
				rs.RenderWorld.Instances.Add( mesh );
			});

			if (fpvEnabled)
			{
				SetFPVEnabled();
			}

			WorldMatrix = tm ;
		}


		/// <summary>
		/// Setups FPV stuff :
		/// </summary>
		void SetFPVEnabled()
		{
			var fpvCameraIndex		=	scene.GetNodeIndex( fpvCameraNode );

			if (fpvCameraIndex<0) 
			{	
				Log.Warning("Camera node '{0}' does not exist", fpvCameraNode);
				cameraTransform	=	Matrix.Identity;
			} 
			else 
			{
				var fpvCameraMatrix	=	Scene.FixGlobalCameraMatrix( sceneView.GetAbsoluteTransform( fpvCameraIndex ) );
				var fpvViewMatrix	=	Matrix.Invert( fpvCameraMatrix );
				cameraTransform		=	fpvViewMatrix;
			}

			sceneView.ForEachMesh( mesh => mesh.Group = InstanceGroup.Weapon );
		}


		/// <summary>
		/// Disposes stuff
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if (disposing)
			{
				sceneView?.ForEachMesh( mesh => rs.RenderWorld.Instances.Remove( mesh ) );
			}
			
			base.Dispose( disposing );
		}
	

		/// <summary>
		/// Sets and gets model's world matrix
		/// </summary>
		public Matrix WorldMatrix 
		{
			get
			{
				return worldMatrix;
			}

			set 
			{
				worldMatrix = value;
				sceneView.SetTransform( (mesh,matrix) => mesh.World = matrix, cameraTransform * preTransform * worldMatrix );
			}
		}

		Matrix worldMatrix = Matrix.Identity;
	}
}
