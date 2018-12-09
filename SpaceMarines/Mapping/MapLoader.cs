using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Core.Extensions;
using Fusion.Engine.Graphics;
using SpaceMarines.Core;

namespace SpaceMarines.Mapping {

	[ContentLoader(typeof(Map))]
	public class MapLoader : ContentLoader {


		public override object Load( ContentManager content, Stream stream, Type requestedType, string assetPath, IStorage storage )
		{
			var text = Encoding.UTF8.GetString( stream.ReadAllBytes() );

			return LoadLegacyMap( content, text );

		}


		struct MazeDef {
			public int Count;
			public string Name;
		}

		struct Spawn {
			public int X;
			public int Y;
			public string Name;
			public int Count;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="mapPath"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.NoOptimization)]
		public Map LoadLegacyMap ( ContentManager content, string text )
		{
			var lines = text
				.Split('\r','\n')
				.Select( s0 => s0.Trim(' ','\t',';') )
				.Where( s1 => !s1.StartsWith("//") )
				.Where( s2 => !string.IsNullOrWhiteSpace(s2) )
				.Select( s3 => s3.Replace('\t', ' ') )
				.ToArray();

			var	definitions =	new Dictionary<string, MazeDef>();
			var spawns		=	new List<Spawn>(256);
			var tileSet		=	(TextureAtlas)null;
			var maze		=	(Maze)null;

			//
			//	parse level
			//
			foreach ( var line in lines ) {

				var args = line.SplitCommandLine().ToArray();

				switch ( args[0] ) {

					case "mazeInit":
						var width	=	int.Parse(args[1]);
						var height	=	int.Parse(args[2]);
						tileSet		=	content.Load<TextureAtlas>( args[3] );
						maze		=	new Maze( width, height );
					break;

					case "mazeDef":
						MazeDef def	=	new MazeDef();
						var key		=	args[1];
						def.Name	=	args[2];
						def.Count	=	int.Parse(args[3]);
						definitions.Add( key, def );
					break;

					case "maze":
						var row		=	int.Parse(args[1]);
						var	rowData	=	args[2];

						for (int col=0; col<rowData.Length; col++) {
							var ch			=	rowData[col];
							var tile		=	ch==' ' ? TileContent.Void : TileContent.Floor;
							maze[col,row]	=	tile;

							if (ch!=' ' && ch!='*') {

								MazeDef cellDef;

								if (definitions.TryGetValue(new string(new[] { ch } ), out cellDef)) {

									var spawn = new Spawn();
									spawn.Count	=	cellDef.Count;
									spawn.Name	=	cellDef.Name;
									spawn.X		=	col;
									spawn.Y		=	row;

									spawns.Add( spawn );
								}
							}
						}
					break;
				}
			}


			//
			//	create map from parsed data :
			//
			var map = new Map( maze, tileSet );

			map.GenerateWalls();

			return map;
		}

	}
}
