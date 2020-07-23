using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Reflection;
using Native.Fbx;
using IronStar.Entities;
using Fusion.Core.Content;
using System.IO;
using IronStar.Core;
using Fusion.Core;
using Fusion.Core.Extensions;
using Fusion.Engine.Graphics;
using BEPUphysics.BroadPhaseEntries;
using Fusion.Core.Mathematics;
using Fusion;
using Newtonsoft.Json;
using Fusion.Engine.Graphics.GI;
using IronStar.ECS;

namespace IronStar.Mapping {
	public partial class Map : IPrecachable {

		

		/// <summary>
		/// 
		/// </summary>
		public MapEnvironment Environment { get; set; }


		/// <summary>
		/// List of nodes
		/// </summary>
		public MapNodeCollection Nodes { get; set; }


		/// <summary>
		/// List of nodes
		/// </summary>
		public RadiositySettings RadiositySettings { get; set; }


		/// <summary>
		/// 
		/// </summary>
		public Map ()
		{
			Nodes				=	new MapNodeCollection();
			Environment			=	new MapEnvironment();
			RadiositySettings	=	new RadiositySettings();
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="content"></param>
		public void Precache( ContentManager content )
		{
			//content.Precache<Scene>( ScenePath );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameWorld"></param>
		[Obsolete]
		public void UpdateEnvironment ( GameWorld gameWorld )
		{
			#warning move to entities!
			gameWorld.Physics.Gravity			=	Environment.Gravity;

			gameWorld.snapshotHeader.FogDistance	=	Environment.FogDistance;
			gameWorld.snapshotHeader.FogColor		=	Environment.FogColor;
			gameWorld.snapshotHeader.Gravity		=	Environment.Gravity;
			gameWorld.snapshotHeader.SunIntensity	=	Environment.SunIntensity;
			gameWorld.snapshotHeader.SunDirection	=	Environment.SunPosition;
			gameWorld.snapshotHeader.Turbidity		=	Environment.SkyTrubidity;
			gameWorld.snapshotHeader.SkyIntensity	=	Environment.SkyIntensity;
		}


		public void Validate()
		{
			foreach ( var n in Nodes ) {
				if ( Math.Abs( n.TranslateX ) > 1024 ||	Math.Abs( n.TranslateY ) > 1024 || Math.Abs( n.TranslateZ ) > 1024 ) {
					Log.Warning("Map : bad position : [{0} {1} {2}]. Moved to [0 0 0]", n.TranslateX, n.TranslateY, n.TranslateZ );
					n.TranslateX = 0;
					n.TranslateY = 0;
					n.TranslateZ = 0;
				}


				if ( float.IsNaN(n.RotateYaw) || float.IsInfinity(n.RotateYaw) ) {
					Log.Warning("Map : bad rotation yaw : {0}", n.RotateYaw );
					n.RotateYaw = 0;
				}

				if ( float.IsNaN(n.RotatePitch) || float.IsInfinity(n.RotatePitch) ) {
					Log.Warning("Map : bad rotation pitch : {0}", n.RotatePitch );
					n.RotatePitch = 0;
				}

				if ( float.IsNaN(n.RotateRoll) || float.IsInfinity(n.RotateRoll) ) {
					Log.Warning("Map : bad rotation roll : {0}", n.RotateRoll );
					n.RotateRoll = 0;
				}


				if ( float.IsNaN(n.TranslateX) || float.IsInfinity(n.TranslateX) ) {
					Log.Warning("Map : bad translation X : {0}", n.TranslateX );
					n.TranslateX = 0;
				}

				if ( float.IsNaN(n.TranslateY) || float.IsInfinity(n.TranslateY) ) {
					Log.Warning("Map : bad translation Y : {0}", n.TranslateY );
					n.TranslateY = 0;
				}

				if ( float.IsNaN(n.TranslateZ) || float.IsInfinity(n.TranslateZ) ) {
					Log.Warning("Map : bad translation Z : {0}", n.TranslateZ );
					n.TranslateZ = 0;
				}
			}
		}


		internal void ActivateGameState( GameState gs )
		{
			var g = gs.Spawn();
			g.AddComponent( new Physics2.Gravity(48) );

			foreach ( var node in Nodes )
			{
				node.SpawnNodeECS( gs );
			}
		}
	}



	/// <summary>
	/// Map loader
	/// </summary>
	[ContentLoader( typeof( Map ) )]
	public sealed class MapLoader : ContentLoader {

		public override object Load( ContentManager content, Stream stream, Type requestedType, string assetPath, IStorage storage )
		{
			Map map = content.Game.GetService<JsonFactory>().ImportJson( stream ) as Map;

			map.Validate();

			return map;
		}
	}
}
