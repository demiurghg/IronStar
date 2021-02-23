using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Core.Content;
using Fusion.Core.Configuration;
using Fusion.Drivers.Graphics;
using Fusion.Engine.Common;
using Fusion.Engine.Graphics.Ubershaders;
using Fusion.Core;
using Fusion.Engine.Graphics.Lights;
using Fusion.Core.Shell;
using Fusion.Engine.Graphics.Bvh;
using System.Diagnostics;
using System.IO;
using Fusion.Build;

namespace Fusion.Engine.Graphics.GI
{
	public enum LightProbeCaptureMode 
	{
		HdrImage,
		GBuffer,
	}

	public partial class LightProbeBaker : RenderComponent
	{
		private		Camera	cubemapCamera;
		readonly	Color[]	stagingBuffer;

		public LightProbeBaker( RenderSystem rs ) : base(rs)
		{
			var bufferSize	=	RenderSystem.LightProbeSize * RenderSystem.LightProbeSize;
			stagingBuffer	=	new Color[ bufferSize ];
		}


		public override void Initialize()
		{
			cubemapCamera	=	new Camera(rs, nameof(cubemapCamera));

			LoadContent();
			Game.Reloading += (s,e) => LoadContent();
			
			Game.Invoker.RegisterCommand("bakeLightProbes", () => new BakeLightProbes(this) );
		}


		void LoadContent ()
		{
		}


		protected override void Dispose( bool disposing )
		{
			if (disposing)
			{
				SafeDispose( ref cubemapCamera );
			}

			base.Dispose( disposing );
		}

		/*-----------------------------------------------------------------------------------------
		 *	Light probe relighting :
		-----------------------------------------------------------------------------------------*/

		class BakeLightProbes : ICommand
		{
			[CommandLineParser.Required]
			public LightProbeCaptureMode Mode { get; set; }

			[CommandLineParser.Required]
			[CommandLineParser.Name("mapname")]
			public string MapName { get; set; }

			readonly LightProbeBaker lpb;
			
			public BakeLightProbes ( LightProbeBaker lpb ) 
			{
				this.lpb = lpb;
			}
			
			public object Execute()
			{
				using ( var stream = lpb.Game.GetService<Builder>().CreateSourceFile( RenderSystem.LightProbePath, MapName + ".bin" ) )
				{
					lpb.CaptureLightProbes( stream, Mode );
				}

				return null;
			}
		}

		/*-----------------------------------------------------------------------------------------
		 *	Light probe relighting :
		-----------------------------------------------------------------------------------------*/

		public void CaptureLightProbes ( Stream stream, LightProbeCaptureMode captureMode )
		{
			var lmRc		=	rs.LightMapResources;
			var lightSet	=	rs.RenderWorld.LightSet;
			var sw			=	new Stopwatch();
			var device		=	Game.GraphicsDevice;
			var useGBuffer	=	captureMode == LightProbeCaptureMode.GBuffer;
			var camera		=	cubemapCamera;
			var time		=	GameTime.Zero;
			sw.Start();

			Log.Message("---- Building Environment Radiance ----");

			using ( var writer = new BinaryWriter(stream) ) 
			{
				writer.WriteFourCC("IRC3");
				writer.WriteFourCC(useGBuffer ? "GBUF" : "HDRI");
				writer.Write( lightSet.LightProbes.Count );

				for ( int index = 0; index < lightSet.LightProbes.Count; index++ )
				{
					var lightProbe =	lightSet.LightProbes[index];

					using ( new PixEvent( "Render Cube" ) )
					{
						Log.Message( "...{0}", lightProbe.Guid );

						for (int i=0; i<6; i++) 
						{
							var face	=	(CubeFace)i;
							var depth	=	lmRc.LightProbeDepth.GetSurface();
							var gbuf0	=	lmRc.LightProbeColor.GetSurface( 0, face );
							var gbuf1	=	lmRc.LightProbeMapping.GetSurface( 0, face );
							var hdr		=	lmRc.LightProbeRadiance.GetSurface( 0, face );

							camera.SetupCameraCubeFaceLH( lightProbe.ProbeMatrix.TranslationVector, face, 0.125f, 4096 );
					
							device.Clear( depth );
							device.Clear( hdr,	 MathUtil.Random.NextColor4() );
							device.Clear( gbuf0, Color4.Zero );
							device.Clear( gbuf1, Color4.Zero );

							var groups	=	InstanceGroup.Static | InstanceGroup.Kinematic;

							if (useGBuffer) 
							{
								//	render gbuffer albedo and lightmap coords:
								var context	=	new LightProbeContext( rs, camera, depth, gbuf0, gbuf1 );
								rs.SceneRenderer.RenderLightProbeGBuffer( context, rs.RenderWorld, groups );
							}
							else
							{
								//	render hdr image :
								var context	=	new LightProbeContext( rs, camera, depth, hdr, null );
								RenderHdrScene( hdr, context, rs.RenderWorld, groups );
							}
						}
					}

					writer.WriteFourCC("CUBE");
					writer.Write( lightProbe.Guid.ToString() );
				
					if (useGBuffer)
					{
						WriteCubemapToStream( writer, lmRc.LightProbeColor );
						WriteCubemapToStream( writer, lmRc.LightProbeMapping );
					}
					else
					{
						device.ResetStates();
						Game.GetService<CubeMapFilter>().GenerateCubeMipLevel( lmRc.LightProbeRadiance );
						Game.GetService<CubeMapFilter>().PrefilterLightProbe( lmRc.LightProbeRadiance, lmRc.LightProbeRadianceArray, index );
						WriteCubemapToStream( writer, lmRc.LightProbeRadianceArray, index );
					}
				}
			}

			sw.Stop();
			Log.Message("{0} light probes - {1} ms", lightSet.LightProbes.Count, sw.ElapsedMilliseconds);
			Log.Message("----------------");
		}


		void RenderHdrScene(RenderTargetSurface hdrSurf, LightProbeContext context, RenderWorld rw, InstanceGroup groups)
		{
			var camera	=	context.GetCamera();

			rs.Sky.RenderSkyLut( GameTime.Zero, camera );
			rs.Sky.RenderSky( GameTime.Zero, camera, StereoEye.Mono, hdrSurf );
			rs.Sky.RenderSkyCube( GameTime.Zero, camera );

			rs.SceneRenderer.RenderLightProbeRadiance( context, rs.RenderWorld, groups );
		}


		void WriteCubemapToStream( BinaryWriter writer, RenderTargetCube cube )
		{
			int count;

			for (int mip=0; mip<cube.MipCount; mip++)
			{
				for (int face=0; face<6; face++) 
				{
					count = cube.GetData( (CubeFace)face, mip, stagingBuffer );
					writer.Write( stagingBuffer, count );
				}
			}
		}


		void WriteCubemapToStream( BinaryWriter writer, TextureCubeArrayRW cubeArray, int index )
		{
			int count;

			for (int mip=0; mip<RenderSystem.LightProbeMaxMips; mip++)
			{
				for (int face=0; face<6; face++) 
				{
					count = cubeArray.GetData( mip, index, (CubeFace)face, stagingBuffer );
					writer.Write( stagingBuffer, count );
				}
			}
		}


		public void CaptureLightProbes ( string mapName, LightProbeCaptureMode captureMode )
		{
			var device			=	Game.GraphicsDevice;
			var builder			=	Game.GetService<Builder>();
			var basePath		=	builder.GetBaseInputDirectory();

			var pathIrrCache	=	Path.Combine(basePath, RenderSystem.LightProbePath, Path.ChangeExtension( mapName, ".bin" ) );

			ContentUtils.MakeDirectoryForFile( pathIrrCache );

			using ( var stream = File.OpenWrite( pathIrrCache ) ) 
			{
				CaptureLightProbes( stream, captureMode );
			}
		}
	}
}
