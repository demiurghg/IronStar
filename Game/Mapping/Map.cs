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

namespace IronStar.Mapping {
	public partial class Map : IPrecachable {


		/// <summary>
		/// 
		/// </summary>
		#warning move to entities!
		public MapEnvironment Environment { get; set; }


		/// <summary>
		/// List of nodes
		/// </summary>
		public MapNodeCollection Nodes { get; set; }


		public MapNavigation Navigation { get; set; }


		/// <summary>
		/// 
		/// </summary>
		public Map ()
		{
			Nodes		=	new MapNodeCollection();
			Environment	=	new MapEnvironment();
			Navigation	=	new MapNavigation();
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
			gameWorld.snapshotHeader.FogHeight		=	Environment.FogHeight;
			gameWorld.snapshotHeader.FogColor		=	Environment.FogColor;
			gameWorld.snapshotHeader.Gravity		=	Environment.Gravity;
			gameWorld.snapshotHeader.SunIntensity	=	Environment.SunIntensity;
			gameWorld.snapshotHeader.SunDirection	=	Environment.SunPosition;
			gameWorld.snapshotHeader.Turbidity		=	Environment.SkyTrubidity;
			gameWorld.snapshotHeader.AmbientLevel	=	Environment.AmbientLevel;
		}
	}



	/// <summary>
	/// Map loader
	/// </summary>
	[ContentLoader( typeof( Map ) )]
	public sealed class MapLoader : ContentLoader {

		public override object Load( ContentManager content, Stream stream, Type requestedType, string assetPath, IStorage storage )
		{
			return content.Game.GetService<JsonFactory>().ImportJson( stream );
		}
	}
}
