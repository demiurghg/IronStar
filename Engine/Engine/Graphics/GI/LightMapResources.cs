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
using System.Diagnostics;
using Fusion.Engine.Imaging;
using Fusion.Core.Configuration;
using Fusion.Build.Mapping;
using System.Threading.Tasks;
using System.IO;
using Fusion.Core.Content;

namespace Fusion.Engine.Graphics.Lights {

	public class LightMapResources : DisposableBase {

		internal Texture2D	IrradianceTextureRed	{ get { return irradianceTextureR; } }
		internal Texture2D	IrradianceTextureGreen	{ get { return irradianceTextureG; } }
		internal Texture2D	IrradianceTextureBlue	{ get { return irradianceTextureB; } }
		
		Texture2D	irradianceTextureR;
		Texture2D	irradianceTextureG;
		Texture2D	irradianceTextureB;

		internal TextureCubeArray	IrradianceCubeMaps	{ get { return irradianceCubeMaps; } }
		TextureCubeArray irradianceCubeMaps;


		public LightMapResources ( RenderSystem rs )
		{
			int lmSize	=	RenderSystem.LightmapSize;

			irradianceTextureR	=	new Texture2D( rs.Device, lmSize, lmSize, ColorFormat.Rgba16F, false ); 
			irradianceTextureG	=	new Texture2D( rs.Device, lmSize, lmSize, ColorFormat.Rgba16F, false ); 
			irradianceTextureB	=	new Texture2D( rs.Device, lmSize, lmSize, ColorFormat.Rgba16F, false ); 

			int size	=	RenderSystem.LightProbeSize;
			int mips	=	RenderSystem.LightProbeMaxMips;
			int length	=	RenderSystem.MaxEnvLights;

			irradianceCubeMaps	=	new TextureCubeArray( rs.Device, size, length, ColorFormat.Rgba16F, mips );
		}


		protected override void Dispose( bool disposing )
		{
			if (disposing)
			{
				SafeDispose( ref irradianceTextureR );
				SafeDispose( ref irradianceTextureG );
				SafeDispose( ref irradianceTextureB );

				SafeDispose( ref irradianceCubeMaps );
			}

			base.Dispose( disposing );
		}

	}
}
