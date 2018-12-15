#define DIRECTX
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
using Fusion.Core.Content;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Drivers.Graphics;
using Fusion.Engine.Common;


namespace Fusion.Engine.Graphics {

	[ContentLoader(typeof(TextureAtlas))]
	public class TextureAtlasLoader : ContentLoader {

		public override object Load ( ContentManager content, Stream stream, Type requestedType, string assetPath, IStorage storage )
		{
			bool srgb = assetPath.ToLowerInvariant().Contains("|srgb");
			return new TextureAtlas( content.Game.RenderSystem, stream, srgb );
		}
	}
		


	/// <summary>
	/// Represents texture atlas.
	/// </summary>
	public class TextureAtlas : DisposableBase {

		private	Texture	texture;

		readonly TextureAtlasClip[] clips;
		readonly int				width;
		readonly int				height;
		readonly Rectangle[]		rects;
		readonly RectangleF[]		rectsf;
		readonly Dictionary<string,TextureAtlasClip> dictionary;

		public Rectangle[] AbsoluteRectangles {
			get { return rects; }
		}

		public RectangleF[] NormalizedRectangles {
			get { return rectsf; }
		}


		/// <summary>
		/// Atlas texture.
		/// </summary>
		public Texture Texture { 
			get { 
				return texture; 
			} 
		}



		/// <summary>
		/// Creates texture atlas from stream.
		/// </summary>
		/// <param name="device"></param>
		public TextureAtlas ( RenderSystem rs, Stream stream, bool useSRgb = false )
		{
			var device = rs.Game.GraphicsDevice;

			using ( var br = new BinaryReader(stream) ) {
			
				br.ExpectFourCC("ATLS", "texture atlas");
				
				int clipCount	=	br.ReadInt32();
				width			=	br.ReadInt32();
				height			=	br.ReadInt32();

				clips			=	new TextureAtlasClip[clipCount];
				
				for ( int i=0; i<clipCount; i++ ) {

					var name	=	br.ReadString();
					var first	=	br.ReadInt32();
					var length	=	br.ReadInt32();

					var clip	=	new TextureAtlasClip(name, first, length);

					clips[ i ]	=	clip;
				}				

				//-----------------------------------------

				br.ExpectFourCC("FRMS", "frame section");

				int rectCount	=	br.ReadInt32();
				rects			=	new Rectangle[ rectCount ];

				for ( int i=0; i<rectCount; i++ ) {
					rects[i]	=	br.Read<Rectangle>();
				}

				float fwidth	=	width;
				float fheight	=	height;

				rectsf	=	rects
							.Select( r => new RectangleF( r.X / fwidth, r.Y / fheight, r.Width / fwidth, r.Height / fheight ) )
							.ToArray();

				//-----------------------------------------

				br.ExpectFourCC("TEX0", "texture data");

				int ddsFileLength	=	br.ReadInt32();
				
				var ddsImageBytes	=	br.ReadBytes( ddsFileLength );

				texture	=	new UserTexture( rs, ddsImageBytes, useSRgb );
			}

			dictionary	=	clips.ToDictionary( e => e.Name );
		}



		/// <summary>
		/// Gets number of images.
		/// </summary>
		public int Count {
			get {
				return clips.Length;
			}
		}


		
		/// <summary>
		/// Gets subimage rectangle by its index
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		[Obsolete("!", true)]
		public Rectangle this [int index]
		{	
			get {					
				throw new NotImplementedException();
			}
		}

					
		
		/// <summary>
		/// Gets subimage rectangle by its index
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		[Obsolete("!", true)]
		public Rectangle this [string name]
		{	
			get {					
				return GetAbsoluteRectangleByName(name);
			}
		}

					

		/// <summary>
		/// Gets names of all subimages. 
		/// </summary>
		public string[] GetClipNames () 
		{
			return clips.Select( e => e.Name ).ToArray();
		}



		/// <summary>
		/// Gets clip by name
		/// </summary>
		/// <param name="clipName"></param>
		/// <returns>Instance of TextureAtlasClip. If does not exist returns NULL.</returns>
		public TextureAtlasClip GetClipByName ( string clipName )
		{
			TextureAtlasClip clip;
			if ( dictionary.TryGetValue(clipName, out clip) ) {
				return clip;
			} else {
				return null;
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		[Obsolete]
		public RectangleF GetNormalizedRectangleByName ( string name )
		{
			TextureAtlasClip clip;
			if (dictionary.TryGetValue( name, out clip )) {
				return rectsf[ clip.FirstIndex ];
			} else {
				Log.Warning("Missing atlas entry: {0}", name);
				return new RectangleF(0,0,0,0);
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		[Obsolete]
		public Rectangle GetAbsoluteRectangleByName ( string name )
		{
			TextureAtlasClip clip;
			if (dictionary.TryGetValue( name, out clip )) {
				return rects[ clip.FirstIndex ];
			} else {
				Log.Warning("Missing atlas entry: {0}", name);
				return new Rectangle(0,0,0,0);
			}
		}



		/// <summary>
		/// Gets float rectangles of all subimages in normalized texture coordibates
		/// </summary>
		/// <param name="maxCount">Maximum number of recatangles. 
		/// If maxCount greater than number of images the rest of 
		/// he array will be filled with zeroed rectangles.</param>
		/// <returns></returns>
		public RectangleF[] GetNormalizedRectangles ( int maxCount = -1 ) 
		{
			ThrowIfDisposed();

			if (maxCount<0) {
				maxCount = rectsf.Length;
			}

			return Enumerable.Range( 0, maxCount )
				.Select( i => (i<rectsf.Length) ? rectsf[i] : new RectangleF(0,0,0,0) )
				.ToArray();
		}



		/// <summary>
		/// Gets rectangles of all subimages in texels.
		/// </summary>
		/// <param name="maxCount">Maximum number of recatangles. 
		/// If maxCount greater than number of images
		/// the rest of the array will be filled with zeroed rectangles.</param>
		/// <returns></returns>
		public Rectangle[] GetRectangles (int maxCount = -1 ) 
		{
			ThrowIfDisposed();

			if (maxCount<0) {
				maxCount = rects.Length;
			}

			return Enumerable.Range( 0, maxCount )
				.Select( i => (i<rects.Length) ? rects[i] : new Rectangle(0,0,0,0) )
				.ToArray();
		}



		/// <summary>
		/// Gets index if particular subimage.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		[Obsolete("!", true)]
		public int IndexOf( string name )
		{
			return -1;
			//Element e;
			//if (dictionary.TryGetValue(name, out e)) {
			//	return e.Index;
			//} else {
			//	return -1;
			//}
		}
		


		/// <summary>
		/// Disposes texture atlas.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose ( bool disposing )
		{
			if (disposing) {
				SafeDispose( ref texture );
			}
			base.Dispose( disposing );
		}
	}
}
