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
	public partial class RenderModelView : DisposableBase
	{
		static readonly Scene EmptyScene = Scene.CreateEmptyScene();

		RenderSystem rs;
		Scene scene;
		SceneView<RenderInstance> sceneView;


		public RenderModelView ( GameState gs, RenderModel rm, Transform t )
		{
			var content	=	gs.GetService<ContentManager>();
			rs			=	gs.GetService<RenderSystem>();

			scene		=	string.IsNullOrWhiteSpace(rm.scenePath) ? Scene.Empty : content.Load( rm.scenePath, Scene.Empty );
			
			sceneView	=	new SceneView<RenderInstance>( scene, 
							mesh => new RenderInstance( rs, scene, mesh ),
							node => rm.AcceptVisibleNode( node )
							);

			sceneView.ForEachMesh( mesh => {
				mesh.Group	= rm.UseLightMap ? InstanceGroup.Static : InstanceGroup.Kinematic;
				mesh.Color	= Color4.Zero;
				mesh.LightMapGuid = rm.lightmapGuid;
				mesh.LightMapSize = rm.lightmapSize;
				rs.RenderWorld.Instances.Add( mesh );
			});

			SetTransform( t.TransformMatrix );
		}


		protected override void Dispose( bool disposing )
		{
			if (disposing)
			{
				sceneView?.ForEachMesh( mesh => rs.RenderWorld.Instances.Remove( mesh ) );
			}
			
			base.Dispose( disposing );
		}
	

		public void SetTransform( Matrix worldMatrix )
		{
			sceneView.SetTransform( (mesh,matrix) => mesh.World = matrix, worldMatrix );
		}
	}
}
