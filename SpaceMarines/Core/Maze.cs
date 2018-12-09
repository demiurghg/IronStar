using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace SpaceMarines.Core {
	public class Maze {

		readonly TileContent[] maze;
		readonly int width;
		readonly int height;

		public int Width {
			get {
				return width;
			}
		}

		public int Height {
			get {
				return height;
			}
		}
		

		/// <summary>
		/// 
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		public Maze ( int width, int height )
		{
			this.maze	=	new TileContent[ width * height ];
			this.width	=	width;
			this.height	=	height;

			for ( int i=0; i<maze.Length; i++ ) {
				maze[i] =	TileContent.Void;
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		int Address ( int x, int y )
		{
			x = MathUtil.Clamp( x, 0, width  - 1 );
			y = MathUtil.Clamp( y, 0, height - 1 );

			return y * width + x;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public TileContent this[ int x, int y ] {
			get {
				return maze[ Address(x,y) ];
			}
			set {
				maze[ Address(x,y) ] = value;
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public string GetLegacyTilePrefix ( int x, int y )
		{
			var cell	=	this[x	, y	 ] == TileContent.Floor;		
			var cell_cl	=	this[x-1, y  ] == TileContent.Floor;	
			var cell_cr	=	this[x+1, y  ] == TileContent.Floor;	
			var cell_tl	=	this[x-1, y+1] == TileContent.Floor;	
			var cell_tr	=	this[x+1, y+1] == TileContent.Floor;	
			var cell_bl	=	this[x-1, y-1] == TileContent.Floor;	
			var cell_br	=	this[x+1, y-1] == TileContent.Floor;	
			var cell_tc	=	this[x  , y+1] == TileContent.Floor;	
			var cell_bc	=	this[x  , y-1] == TileContent.Floor;	

			var rand	=	MathUtil.RandXorShift( (uint)(x * 701 + y * 947) );
			var rand3	=	rand % 3 + 1;
			var rand5	=	rand % 5 + 1;

			//	passages ----------------------------------

			if (cell) {
				return "_cc" + rand5.ToString("00");
			}


			//	internal corners --------------------------

			if (cell_br && cell_cr && cell_bc) {
				return "_otl01";
			}
			
			if (cell_bl && cell_cl && cell_bc) {
				return "_otr01";
			}
			
			
			if (cell_tr && cell_cr && cell_tc) {
				return "_obl01";
			}
			
			if (cell_tl && cell_cl && cell_tc) {
				return "_obr01";
			}

			//	side-walls --------------------------------

			if (cell_cr) {
				return "_cl" + rand3.ToString("00");
			}
	
			if (cell_cl) {
				return "_cr" + rand3.ToString("00");
			}
	
			if (cell_bc) {
				return "_tc" + rand3.ToString("00");
			}
	
			if (cell_tc) {
				return "_bc" + rand3.ToString("00");
			}

			//	corners -----------------------------------

			if (cell_br && !cell_cr && !cell_bc) {
				return "_tl01";
			}

			if (cell_bl && !cell_cl && !cell_bc) {
				return "_tr01";
			}
			
			if (cell_tr && !cell_cr && !cell_tc) {
				return "_bl01";
			}
			
			if (cell_tl && !cell_cl && !cell_tc) {
				return "_br01";
			}

			return null;
		}


	}
}
