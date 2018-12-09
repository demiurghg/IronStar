using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Engine.Graphics;
using Fusion.Scripting;
using SpaceMarines.SFX;
using KopiLua;
using static KopiLua.Lua;
using SpaceMarines.Core;
using Fusion.Core.Mathematics;

namespace SpaceMarines.Mapping {
	public class Map {

		readonly TextureAtlas tileSet;
		readonly Maze maze;


		public Map ( Maze maze, TextureAtlas tileSet )
		{
			this.maze		=	maze;
			this.tileSet	=	tileSet;
		}


		/// <summary>
		/// 
		/// </summary>
		public void GenerateWalls ()
		{
			for ( int x=0; x<maze.Width; x++ ) {
				for ( int y=0; y<maze.Height; y++ ) {

					if (maze[x,y]==TileContent.Void) {
					
						var t0	=	maze[ x+1, y+1 ]==TileContent.Floor;
						var t1	=	maze[ x+0, y+1 ]==TileContent.Floor;
						var t2	=	maze[ x-1, y+1 ]==TileContent.Floor;
						var t3	=	maze[ x+1, y-1 ]==TileContent.Floor;
						var t4	=	maze[ x+0, y-1 ]==TileContent.Floor;
						var t5	=	maze[ x-1, y-1 ]==TileContent.Floor;
						var t6	=	maze[ x+1, y+0 ]==TileContent.Floor;
						var t7	=	maze[ x-1, y+0 ]==TileContent.Floor;

						if (t0 || t1 || t2 || t3 || t4 || t5 || t6 || t7 ) {
							maze[x,y] = TileContent.Wall;
						}
					}
				}
			}
			
		}


		/// <summary>
		/// Draws static stuff like tiles and static decals
		/// </summary>
		/// <param name="viewWorld"></param>
		public void DrawStatic ( FXPlayback viewWorld )
		{	  
			var tileLayer	=	viewWorld.Layers.Tiles;

			for ( int x=0; x<maze.Width; x++ ) {
				for ( int y=0; y<maze.Height; y++ ) {

					var tileName	=	maze.GetLegacyTilePrefix( x,y );

					if (tileName==null) {
						continue;
					}

					tileName	=	"space02" + tileName;

					var srcRect	=	tileSet.GetAbsoluteRectangleByName( tileName );
					var dstRect	=	new Rectangle( x*4, y*4+4, 4, -4 );

					tileLayer.Draw( tileSet.Texture, dstRect, srcRect, Color.White );
				}
			}
		}



		[LuaApi("mazeInit")]
		int MazeInit ( LuaState L )
		{
			using ( new LuaStackGuard( L ) ) {
				
				int width	=	LuaToInteger( L, 1 );	
				int height	=	LuaToInteger( L, 2 );	

				//maze		=	new Maze( width, height );
			}
			return 0;
		}

	}
}
