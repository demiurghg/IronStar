using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Engine.Graphics;
using SpaceMarines.SFX;

namespace SpaceMarines.Mapping {
	public class Map {

		public TextureAtlas TileSet;

		public readonly List<Tile> Tiles = new List<Tile>();


		/// <summary>
		/// Draws static stuff like tiles and static decals
		/// </summary>
		/// <param name="viewWorld"></param>
		public void DrawStatic ( ViewWorld viewWorld )
		{
		}
	}
}
