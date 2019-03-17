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

	[ContentLoader(typeof(IrradianceVolume))]
	public class IrradianceVolumeLoader : ContentLoader {

		public override object Load( ContentManager content, Stream stream, Type requestedType, string assetPath, IStorage storage )
		{
			return new IrradianceVolume( content.Game.RenderSystem, stream );
		}
	}


	public class IrradianceVolume : DisposableBase {

		readonly RenderSystem rs;

		readonly GenericVolume<SHL1>	irradianceR;
		readonly GenericVolume<SHL1>	irradianceG;
		readonly GenericVolume<SHL1>	irradianceB;
		readonly GenericVolume<SHL1>	temporary;
		readonly int width;
		readonly int height;
		readonly int depth;
		readonly float stride;

		public int Width { get { return width; } }
		public int Height { get { return height; } }
		public int Depth { get { return depth; } }
		public float Stride { get { return stride; } }

		public GenericVolume<SHL1>	IrradianceRed	 { get { return irradianceR; } }
		public GenericVolume<SHL1>	IrradianceGreen	 { get { return irradianceG; } }
		public GenericVolume<SHL1>	IrradianceBlue	 { get { return irradianceB; } }

		internal Texture3D	IrradianceTextureRed	{ get { return irradianceTextureR; } }
		internal Texture3D	IrradianceTextureGreen	{ get { return irradianceTextureG; } }
		internal Texture3D	IrradianceTextureBlue	{ get { return irradianceTextureB; } }
		
		Texture3D	irradianceTextureR;
		Texture3D	irradianceTextureG;
		Texture3D	irradianceTextureB;

		public Matrix VolumeTransform {
			get {
				return	Matrix.Identity
					*	Matrix.Translation( Width/2.0f*Stride, 0, Depth/2.0f*Stride )
					*	Matrix.Scaling( 1.0f/Width, 1.0f / Height, 1.0f / Depth ) 
					*	Matrix.Scaling( 1.0f/Stride )
					;
			}
		}
		

		public IrradianceVolume ( RenderSystem rs, int width, int height, int depth, float stride )
		{
			this.rs		=	rs;

			this.width	=	width;
			this.height	=	height;
			this.depth	=	depth;
			this.stride	=	stride;

			irradianceR	=	new GenericVolume<SHL1>( width, height, depth );
			irradianceG	=	new GenericVolume<SHL1>( width, height, depth );
			irradianceB	=	new GenericVolume<SHL1>( width, height, depth );
			temporary	=	new GenericVolume<SHL1>( width, height, depth );

			irradianceTextureR	=	new Texture3D( rs.Device, ColorFormat.Rgba16F, width, height, depth ); 
			irradianceTextureG	=	new Texture3D( rs.Device, ColorFormat.Rgba16F, width, height, depth ); 
			irradianceTextureB	=	new Texture3D( rs.Device, ColorFormat.Rgba16F, width, height, depth ); 
		}



		public IrradianceVolume ( RenderSystem rs, Stream stream )
		{
			this.rs		=	rs;

			using ( var reader = new BinaryReader( stream ) ) {
				
				reader.ExpectFourCC("IRV1", "irradiance map format. IRM1 expected.");

				width	=	reader.ReadInt32();
				height	=	reader.ReadInt32();
				depth	=	reader.ReadInt32();
				stride	=	reader.ReadSingle();

				reader.ExpectFourCC("VOL1", "irradiance map format. MAP1 expected.");

				irradianceTextureR	=	new Texture3D( rs.Device, ColorFormat.Rgba16F, width, height, depth ); 
				irradianceTextureG	=	new Texture3D( rs.Device, ColorFormat.Rgba16F, width, height, depth ); 
				irradianceTextureB	=	new Texture3D( rs.Device, ColorFormat.Rgba16F, width, height, depth ); 
				
				irradianceTextureR.SetData( reader.Read<Half4>( width * height * depth ) );
				irradianceTextureG.SetData( reader.Read<Half4>( width * height * depth ) );
				irradianceTextureB.SetData( reader.Read<Half4>( width * height * depth ) );
			}
		}



		public void WriteToStream ( Stream stream )
		{
			using ( var writer = new BinaryWriter( stream ) ) {

				writer.WriteFourCC("IRV1");

				writer.Write( width );
				writer.Write( height );
				writer.Write( depth );
				writer.Write( stride );

				writer.WriteFourCC("VOL1");

				writer.Write( irradianceR.RawImageData.Select( sh => sh.ToHalf4() ).ToArray() );
				writer.Write( irradianceG.RawImageData.Select( sh => sh.ToHalf4() ).ToArray() );
				writer.Write( irradianceB.RawImageData.Select( sh => sh.ToHalf4() ).ToArray() );
			}
		}


		protected override void Dispose( bool disposing )
		{
			if (disposing) {
				SafeDispose( ref irradianceTextureR );
				SafeDispose( ref irradianceTextureG );
				SafeDispose( ref irradianceTextureB );
			}

			base.Dispose( disposing );
		}



		public void UpdateGPUTextures ()
		{
			irradianceTextureR.SetData( irradianceR.RawImageData.Select( sh => sh.ToHalf4() ).ToArray() );
			irradianceTextureG.SetData( irradianceG.RawImageData.Select( sh => sh.ToHalf4() ).ToArray() );
			irradianceTextureB.SetData( irradianceB.RawImageData.Select( sh => sh.ToHalf4() ).ToArray() );
		}


		public void FillAmbient ( Color4 ambient )
		{
			irradianceR.Fill( new SHL1( ambient.Red,	0,0,0 ) );
			irradianceG.Fill( new SHL1( ambient.Green,	0,0,0 ) );
			irradianceB.Fill( new SHL1( ambient.Blue,	0,0,0 ) );
			UpdateGPUTextures();
		}
	}
}
