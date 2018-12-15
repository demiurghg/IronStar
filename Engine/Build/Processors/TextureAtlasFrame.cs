using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Engine.Imaging;

namespace Fusion.Build.Processors {

	/// <summary>
	/// Defines single frame of texture sequence in TextureAtlas.
	/// </summary>
	public class TextureAtlasFrame {

		readonly string name;
		Image image;

		public string Name { get { return name; } }
		public Image Image { get { return image; } }

		public Point Location;

		public int Width { get { return image.Width; } }
		public int Height { get { return image.Height; } }

		public Rectangle Rectangle {
			get {
				return new Rectangle( Location.X, Location.Y, Width, Height );
			}
		}

		/// <summary>
		/// Creates instance of atlas frame
		/// </summary>
		/// <param name="name"></param>
		public TextureAtlasFrame ( string name )
		{
			this.name = name;
		}


		/// <summary>
		/// Load image for given frame.
		/// </summary>
		/// <param name="basePath"></param>
		public void LoadImage ( string basePath )
		{
			var ext			= Path.GetExtension( Name );
			var fullPath	= Path.Combine( basePath, Name );

			using ( var stream = File.OpenRead( fullPath ) ) {
				if ( ext==".tga" ) {
					image = Image.LoadTga( stream );
				} else
				if ( ext==".png" ) {
					image = Image.LoadPng( stream );
				} else
				if ( ext==".jpg" ) {
					image = Image.LoadJpg( stream );
				} else {
					throw new BuildException( "Only TGA, JPG or PNG images are supported." );
				}
			}
		}


		/// <summary>
		/// Writes sub image to the target image
		/// </summary>
		/// <param name="targetImage"></param>
		public void WriteSubimage ( Image targetImage )
		{
			targetImage.Copy( Location.X, Location.Y, Image );
		}
	}

}
