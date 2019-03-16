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

		readonly public int CubeCount;
		

		/// <summary>
		/// Creates default black irradiance map with no lights
		/// </summary>
		/// <param name="rs"></param>
		/// <param name="stream"></param>
		public IrradianceCache ( RenderSystem rs )
		{
			this.rs		=	rs;
			irradianceCubeMaps	=	new TextureCubeArray( rs.Device, 4, 1, ColorFormat.Rgba16F, 1 );
		}

		

		/// <summary>
		/// 
		/// </summary>
		/// <param name="rs"></param>
		/// <param name="stream"></param>
		public IrradianceCache ( RenderSystem rs, Stream stream )
		{
			this.rs		=	rs;

			using ( var reader = new BinaryReader( stream ) ) {
				
				reader.ExpectFourCC("IRC1", "irradiance cache format");

				CubeCount	=	reader.ReadInt32();

				int size	=	RenderSystem.LightProbeSize;
				int mips	=	RenderSystem.LightProbeMaxMips;
				var buffer	=	new Half4[ size * size ];

				irradianceCubeMaps	=	new TextureCubeArray( rs.Device, size, CubeCount, ColorFormat.Rgba16F, mips );

				for ( int cubeId = 0; cubeId < CubeCount; cubeId++ ) {

					reader.ExpectFourCC("CUBE", "irradiance cache cubemap");

					var guid	=	reader.Read<Guid>();

					probes.Add( guid, cubeId );
					
					for (int face=0; face<6; face++) {

						for (int mip=0; mip<mips; mip++) {

							int mipSize		= size >> mip;
							int dataSize	= mipSize * mipSize;

							reader.Read( buffer, dataSize );

							irradianceCubeMaps.SetData( cubeId, (CubeFace)face, mip, buffer );
						}
					}

				}
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
				Log.Warning("LightProbe [{0}] not found", guid );
				return -1;
			}
		}
	}
}
