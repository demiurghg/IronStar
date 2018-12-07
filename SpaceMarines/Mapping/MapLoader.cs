using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Core.Extensions;

namespace SpaceMarines.Mapping {

	[ContentLoader(typeof(Map))]
	public class MapLoader : ContentLoader {


		public override object Load( ContentManager content, Stream stream, Type requestedType, string assetPath, IStorage storage )
		{
			var text = Encoding.UTF8.GetString( stream.ReadAllBytes() );

			return LoadLegacyMap( text );

		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="mapPath"></param>
		/// <returns></returns>
		public Map LoadLegacyMap ( string text )
		{
			var map = new Map();

			var lines = text
				.Split('\r','\n')
				.Select( s0 => s0.Trim(' ','\t') )
				.Where( s1 => !s1.StartsWith("//") )
				.Where( s2 => !string.IsNullOrWhiteSpace(s2) )
				.ToArray();

			int mapWidth = 0;
			int mapHeight = 0;
			var tileSet = "";


			foreach ( var line in lines ) {

				var args = line.SplitCommandLine().ToArray();

				switch ( args[0] ) {
					case "mazeInit":
						   mapWidth		=	int.Parse(args[1]);
						   mapHeight	=	int.Parse(args[2]);
						   tileSet		=	args[3];
						break;
				}


			}


			return map;
		}

	}
}
