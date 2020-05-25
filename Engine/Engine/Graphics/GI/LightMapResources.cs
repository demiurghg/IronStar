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

		internal Texture2D	LightMapR		{ get { return lightMapR; } }
		internal Texture2D	LightMapG		{ get { return lightMapG; } }
		internal Texture2D	LightMapB		{ get { return lightMapB; } }
		
		internal Texture3D	LightVolumeR	{ get { return lightVolumeR; } }
		internal Texture3D	LightVolumeG	{ get { return lightVolumeG; } }
		internal Texture3D	LightVolumeB	{ get { return lightVolumeB; } }
		
		Texture2D	lightMapR;
		Texture2D	lightMapG;
		Texture2D	lightMapB;

		Texture3D	lightVolumeR;
		Texture3D	lightVolumeG;
		Texture3D	lightVolumeB;

		internal TextureCubeArray	LightProbeColorArray	{ get { return lightProbeColorArray; } }
		internal TextureCubeArray	LightProbeMappingArray	{ get { return lightProbeMappingArray; } }
		internal TextureCubeArrayRW	LightProbeRadianceArray	{ get { return lightProbeRadianceArray; } }
		TextureCubeArray	lightProbeColorArray;
		TextureCubeArray	lightProbeMappingArray;
		TextureCubeArrayRW	lightProbeRadianceArray;

		internal RenderTargetCube	LightProbeColor		{ get { return lightProbeColor; } }
		internal RenderTargetCube	LightProbeMapping	{ get { return lightProbeMapping; } }
		internal DepthStencil2D		LightProbeDepth		{ get { return lightProbeDepth; } }
		internal RenderTargetCube	LightProbeRadiance	{ get { return lightProbeRadiance; } }
		RenderTargetCube			lightProbeColor;
		RenderTargetCube			lightProbeMapping;
		DepthStencil2D				lightProbeDepth;
		RenderTargetCube			lightProbeRadiance;


		public LightMapResources ( RenderSystem rs )
		{
			int lmSize		=	RenderSystem.LightmapSize;

			lightMapR		=	new Texture2D( rs.Device, lmSize, lmSize, ColorFormat.Rgba16F, false ); 
			lightMapG		=	new Texture2D( rs.Device, lmSize, lmSize, ColorFormat.Rgba16F, false ); 
			lightMapB		=	new Texture2D( rs.Device, lmSize, lmSize, ColorFormat.Rgba16F, false ); 

			int w			=	RenderSystem.LightVolumeWidth;
			int h			=	RenderSystem.LightVolumeHeight;
			int d			=	RenderSystem.LightVolumeDepth;

			lightVolumeR	=	new Texture3D( rs.Device, ColorFormat.Rgba16F, w,h,d ); 
			lightVolumeG	=	new Texture3D( rs.Device, ColorFormat.Rgba16F, w,h,d ); 
			lightVolumeB	=	new Texture3D( rs.Device, ColorFormat.Rgba16F, w,h,d ); 

			int size		=	RenderSystem.LightProbeSize;
			int mips		=	RenderSystem.LightProbeMaxMips;
			int length		=	RenderSystem.MaxEnvLights;

			lightProbeColorArray	=	new TextureCubeArray	( rs.Device, size, length, ColorFormat.Rgba8, 1			);
			lightProbeMappingArray	=	new TextureCubeArray	( rs.Device, size, length, ColorFormat.Rg16_UNorm, 1	);
			lightProbeRadianceArray	=	new TextureCubeArrayRW	( rs.Device, size, length, ColorFormat.Rg11B10, true, 1	);
			lightProbeColor			=	new RenderTargetCube	( rs.Device, ColorFormat.Rgba8,			size		);
			lightProbeMapping		=	new RenderTargetCube	( rs.Device, ColorFormat.Rg16_UNorm,	size		);
			lightProbeRadiance		=	new RenderTargetCube	( rs.Device, ColorFormat.Rg11B10,		size, mips	);
			lightProbeDepth			=	new DepthStencil2D		( rs.Device, DepthFormat.D24S8,			size, size	);
		}


		protected override void Dispose( bool disposing )
		{
			if (disposing)
			{
				SafeDispose( ref lightMapR );
				SafeDispose( ref lightMapG );
				SafeDispose( ref lightMapB );

				SafeDispose( ref lightVolumeR );
				SafeDispose( ref lightVolumeG );
				SafeDispose( ref lightVolumeB );

				SafeDispose( ref lightProbeColorArray	 );
				SafeDispose( ref lightProbeMappingArray	 );
				SafeDispose( ref lightProbeRadianceArray );
				SafeDispose( ref lightProbeColor		 );
				SafeDispose( ref lightProbeMapping		 );
			}

			base.Dispose( disposing );
		}

	}
}
