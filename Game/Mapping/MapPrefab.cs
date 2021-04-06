using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Fusion.Core.Mathematics;
using Fusion.Engine.Graphics;
using IronStar.SFX;
using Fusion.Development;
using System.Drawing.Design;
using Fusion;
using BEPUphysics.BroadPhaseEntries;
using BEPUTransform = BEPUutilities.AffineTransform;
using Fusion.Core.Shell;
using Fusion.Engine.Graphics.Scenes;
using IronStar.ECS;
using IronStar.ECSPhysics;
using Fusion.Widgets.Advanced;
using Fusion.Core.Extensions;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Build;

namespace IronStar.Mapping 
{
	public class MapPrefab : MapNode 
	{
		[AECategory( "Prefab" )]
		[AEFileName("prefabs", "*.pfb", AEFileNameMode.NoExtension)]
		public string PrefabPath { get; set; } = "";

		MapNodeCollection nodes;
	

		/// <summary>
		/// 
		/// </summary>
		public MapPrefab ()
		{
		}


		public override void SpawnNodeECS( GameState gs )
		{
			var builder = gs.Game.GetService<Builder>();
			using ( var stream = builder.OpenSourceFile( PrefabPath ) )
			{
				nodes = JsonUtils.ImportJson(stream) as MapNodeCollection;

				if (nodes!=null)
				{
					
				}
			}
			/*ecsEntity = gs.Spawn();

			ecsEntity.AddComponent( new ECS.Transform( Matrix.Scaling(Scale) * Transform ) );
			ecsEntity.AddComponent( new StaticCollisionComponent() { Walkable = Walkable } );

			var rm		=	new SFX2.RenderModel( ScenePath, Matrix.Identity, Color.White, 1, SFX2.RMFlags.None );
			rm.cmPrefix	=	UseCollisionMesh ? "cm_" : "";
			var lmSize	=	UseLightVolume ? 0 : (int)LightMapSize;
			rm.SetupLightmap( lmSize, lmSize, Name );
			ecsEntity.AddComponent( rm );*/
		}


		public override BoundingBox GetBoundingBox()
		{
			#warning Need more smart bounding box for map models!
			return new BoundingBox( 8, 8, 8 );
		}
	}
}
