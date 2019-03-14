using System;
using System.Collections.Generic;
using System.Linq;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Drivers.Graphics;
using Fusion.Engine.Graphics.Ubershaders;
using Native.Embree;
using System.Runtime.InteropServices;
using Fusion.Core;
using Fusion.Core.Extensions;
using System.Diagnostics;
using Fusion.Engine.Imaging;
using Fusion.Core.Configuration;
using Fusion.Build.Mapping;
using System.Threading.Tasks;
using System.IO;
using Fusion.Core.Content;

namespace Fusion.Engine.Graphics.Lights {

	[ContentLoader(typeof(IrradianceCache))]
	public class IrradianceCacheLoader : ContentLoader {

		public override object Load( ContentManager content, Stream stream, Type requestedType, string assetPath, IStorage storage )
		{
			return new IrradianceCache( content.Game.RenderSystem, stream );
		}
	}


	public class IrradianceCache : DisposableBase {

		readonly RenderSystem rs;
		internal TextureCubeArray	IrradianceCubeMaps	{ get { return irradianceCubeMaps; } }
		TextureCubeArray irradianceCubeMaps;

		readonly Dictionary<Guid,int> probes = new Dictionary<Guid,int>();

		readonly public int Width;
		readonly public int Height;
		readonly public int MipCount;
		readonly public int CubeCount;
		

		public IrradianceCache ( RenderSystem rs, Stream stream )
		{
			this.rs		=	rs;

			using ( var reader = new BinaryReader( stream ) ) {
				
				//reader.ExpectFourCC("IRM1", "irradiance map format. IRM1 expected.");
				//reader.ExpectFourCC("RGN1", "irradiance map format. RGN1 expected.");

				//int count = reader.ReadInt32();

				//for ( int i=0; i<count; i++ ) {
				//	var guid	= reader.Read<Guid>();
				//	var region	= reader.Read<Rectangle>();
				//	regions.Add( guid, region );
				//}

				//reader.ExpectFourCC("MAP1", "irradiance map format. MAP1 expected.");

				//width	=	reader.ReadInt32();
				//height	=	reader.ReadInt32();

				//irradianceTextureR	=	new Texture2D( rs.Device, width, height, ColorFormat.Rgba16F, false ); 
				//irradianceTextureG	=	new Texture2D( rs.Device, width, height, ColorFormat.Rgba16F, false ); 
				//irradianceTextureB	=	new Texture2D( rs.Device, width, height, ColorFormat.Rgba16F, false ); 
				
				//irradianceTextureR.SetData( reader.Read<Half4>( width * height ) );
				//irradianceTextureG.SetData( reader.Read<Half4>( width * height ) );
				//irradianceTextureB.SetData( reader.Read<Half4>( width * height ) );
			}
		}


		protected override void Dispose( bool disposing )
		{
			if (disposing) {
				SafeDispose( ref irradianceCubeMaps );
			}

			base.Dispose( disposing );
		}



		public bool HasLightProbe ( Guid guid )
		{
			return probes.ContainsKey(guid);
		}



		public int GetLightProbeIndex ( Guid guid )
		{
			int index;
			if (probes.TryGetValue( guid, out index ) ) {
				return index;
			} else {
				return -1;
			}
		}
	}
}
