using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Configuration;
using Fusion.Core.Extensions;
using Fusion.Engine.Common;
using Fusion.Drivers.Graphics;
using Fusion.Engine.Imaging;
using System.Diagnostics;
using Fusion.Core.Content;

namespace Fusion.Engine.Graphics {
	
	/// <summary>
	/// Represents virtual texture resource
	/// </summary>
	public class VirtualTexture : DisposableBase {

		[ContentLoader(typeof(VirtualTexture))]
		internal class Loader : ContentLoader {

			public override object Load( ContentManager content, Stream stream, Type requestedType, string assetPath, IStorage storage )
			{
				//bool srgb = assetPath.ToLowerInvariant().Contains("|srgb");
				return new VirtualTexture(content.Game.RenderSystem, stream, content.VTStorage );
			}
		}

		readonly IStorage tileStorage;


		/// <summary>
		/// Gets tile storage
		/// </summary>
		internal IStorage TileStorage {
			get {
				return tileStorage;
			}
		}


		public class SegmentInfo {

			public static readonly SegmentInfo Empty = new SegmentInfo();

			private SegmentInfo () 
			{
				MaxMipLevel = 0;
				Region = new RectangleF(0,0,0,0);
				AverageColor = Color.Gray;
				Transparent = false;
			}

			public SegmentInfo ( int x, int y, int w, int h, Color color, bool transparent ) 
			{
				var fx		=   x / (float)VTConfig.TextureSize;
				var fy		=   y / (float)VTConfig.TextureSize;
				var fw		=   w / (float)VTConfig.TextureSize;
				var fh		=   h / (float)VTConfig.TextureSize;
				Region		=	new RectangleF( fx, fy, fw, fh );
				MaxMipLevel	=	MathUtil.LogBase2( w / VTConfig.PageSize );
				Transparent	=	transparent;
				AverageColor=	color;
			}

			public readonly RectangleF Region;
			public readonly int MaxMipLevel;
			public bool Transparent;
			public Color AverageColor;
		}


		Dictionary<string,SegmentInfo> textures;

		HashSet<string> warnings = new HashSet<string>();


		/// <summary>
		/// 
		/// </summary>
		/// <param name="rs"></param>
		internal VirtualTexture ( RenderSystem rs, Stream stream, IStorage storage )
		{
			tileStorage	=	storage;

			int num;

			using ( var reader = new BinaryReader( stream ) ) {				
			
				num	=	reader.ReadInt32();

				textures = new Dictionary<string, SegmentInfo>(num);

				for ( int i=0; i<num; i++ ) {
				
					var name	=	reader.ReadString();
					var x       =   reader.ReadInt32();
					var y       =   reader.ReadInt32();
					var w       =   reader.ReadInt32();
					var h       =   reader.ReadInt32();
					var t		=	reader.ReadBoolean();
					var c		=	reader.Read<Color>();

					textures.Add( name, new SegmentInfo( x, y, w, h, c, t ) );
				}

			}
		}



		/// <summary>
		/// Dispose virtual texture stuff
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose( bool disposing )
		{
			if (disposing) {
			}

			base.Dispose(disposing);
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		internal SegmentInfo GetTextureSegmentInfo ( string name )
		{
			if (string.IsNullOrWhiteSpace(name)) {
				return SegmentInfo.Empty;
			}

			SegmentInfo segmentInfo;

			if ( textures.TryGetValue( name, out segmentInfo ) ) {

				return segmentInfo;

			} else {

				var warning = string.Format("Missing VT region {0}", name);

				if (warnings.Add(warning)) {
					Log.Warning(warning);
				}

				//Log.Warning("Missing VT region {0}", name);
				return SegmentInfo.Empty;
			}
		}

	}
}
