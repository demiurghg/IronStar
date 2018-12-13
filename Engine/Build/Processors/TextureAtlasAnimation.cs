using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Engine.Imaging;

namespace Fusion.Build.Processors {

	/// <summary>
	/// Defines texture sequence in TextureAtlas.
	/// </summary>
	public class TextureAtlasAnimation {

		public readonly string Name;
		public readonly TextureAtlasFrame[] Frames;

		public TextureAtlasAnimation ( string name, IEnumerable<string> textureNames )
		{
			Name	=	name;
			Frames	=	textureNames	
						.Select( texName => new TextureAtlasFrame(texName) )
						.ToArray();
		}
	}

}
