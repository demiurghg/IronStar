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
	[Flags]
	public enum RMFlags
	{	
		None			=	0x0000,
		FirstPointView	=	0x0001,
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

		Size2	lightmapSize;
		Guid	lightmapGuid;

		bool	UseLightMap { get { return lightmapSize.Width>0 && lightmapSize.Height>0; } }
		

		//	operational data :
		Scene scene;
		SceneView<RenderInstance> sceneView;


		public RenderModel ( string scenePath, Matrix transform, Color color, float intensity, RMFlags flags )
		{
			this.scenePath	=	scenePath	;
			this.transform	=	transform	;
			this.color		=	color		;
			this.intensity	=	intensity	;
		}


		public void SetupLightmap( int width, int height, Guid regionGuid )
		{
			lightmapSize	=	new Size2( width, height );
			lightmapGuid	=	regionGuid;
		}


		public override void Added( GameState gs, Entity entity )
		{
			base.Added( gs, entity );
			LoadScene( gs );
		}


		public override void Removed( GameState gs )
		{
			base.Removed( gs );
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
			sceneView.SetTransform( (mesh,matrix) => mesh.World = matrix, worldMatrix );
		}

		/*-----------------------------------------------------------------------------------------------
		 *	Scene management operations :
		-----------------------------------------------------------------------------------------------*/

		void LoadScene ( GameState gs )
		{
			var content	=	gs.GetService<ContentManager>();
			var rs		=	gs.GetService<RenderSystem>();

			scene		=	string.IsNullOrWhiteSpace(scenePath) ? Scene.Empty : content.Load( scenePath, Scene.Empty );
			
			sceneView	=	new SceneView<RenderInstance>( scene, 
							mesh => new RenderInstance( rs, scene, mesh ),
							node => true 
							);

			sceneView.ForEachMesh( mesh => {
				mesh.Group	= UseLightMap ? InstanceGroup.Static : InstanceGroup.Kinematic;
				mesh.Color	= Color4.Zero;
				mesh.LightMapGuid = lightmapGuid;
				mesh.LightMapSize = lightmapSize;
				rs.RenderWorld.Instances.Add( mesh );
			});
		}


		public void UnloadScene(GameState gs)
		{
			var rs	=	gs.GetService<RenderSystem>();

			sceneView?.ForEachMesh( mesh => rs.RenderWorld.Instances.Remove( mesh ) );
		}
	}
}
