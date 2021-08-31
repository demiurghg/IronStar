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
		[AEFileName("prefabs", "*.json", AEFileNameMode.NoExtension)]
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

			if (string.IsNullOrWhiteSpace(PrefabPath))
			{
				nodes = null;
				return;
			}

			using ( var stream = builder.OpenSourceFile( PrefabPath ) )
			{
				nodes = JsonUtils.ImportJson(stream) as MapNodeCollection;

				if (nodes!=null)
				{
					foreach ( var node in nodes )
					{
						node.SpawnNodeECS(gs);

						var t = node.EcsEntity.GetComponent<Transform>();
						t.TransformMatrix = t.TransformMatrix * this.Transform;
					}
				}
			}
		}


		public override void KillNodeECS( GameState gs )
		{
			if (nodes!=null)
			{
				foreach ( var node in nodes )
				{
					node.KillNodeECS(gs);
				}
			}
		}


		public override BoundingBox GetBoundingBox( GameState gs )
		{
			#warning Need more smart bounding box for map models!
			return new BoundingBox( 8, 8, 8 );
		}
	}
}
