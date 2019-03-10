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

	[ContentLoader(typeof(IrradianceMap))]
	public class IrradianceMapLoader : ContentLoader {

		public override object Load( ContentManager content, Stream stream, Type requestedType, string assetPath, IStorage storage )
		{
			return new IrradianceMap( content.Game.RenderSystem, stream );
		}
	}


	public class IrradianceMap : DisposableBase {

		readonly RenderSystem rs;

		readonly GenericImage<SHL1>	irradianceR;
		readonly GenericImage<SHL1>	irradianceG;
		readonly GenericImage<SHL1>	irradianceB;
		readonly GenericImage<SHL1>	temporary;
		readonly int width;
		readonly int height;

		public GenericImage<SHL1>	IrradianceRed	 { get { return irradianceR; } }
		public GenericImage<SHL1>	IrradianceGreen	 { get { return irradianceG; } }
		public GenericImage<SHL1>	IrradianceBlue	 { get { return irradianceB; } }

		internal Texture2D	IrradianceTextureRed	{ get { return irradianceTextureR; } }
		internal Texture2D	IrradianceTextureGreen	{ get { return irradianceTextureG; } }
		internal Texture2D	IrradianceTextureBlue	{ get { return irradianceTextureB; } }
		
		Texture2D	irradianceTextureR;
		Texture2D	irradianceTextureG;
		Texture2D	irradianceTextureB;


		readonly Dictionary<Guid,Rectangle> regions = new Dictionary<Guid, Rectangle>();
		

		public IrradianceMap ( RenderSystem rs, int width, int height )
		{
			this.rs		=	rs;

			this.width	=	width;
			this.height	=	height;

			irradianceR	=	new GenericImage<SHL1>( width, height );
			irradianceG	=	new GenericImage<SHL1>( width, height );
			irradianceB	=	new GenericImage<SHL1>( width, height );
			temporary	=	new GenericImage<SHL1>( width, height );

			irradianceTextureR	=	new Texture2D( rs.Device, width, height, ColorFormat.Rgba16F, false ); 
			irradianceTextureG	=	new Texture2D( rs.Device, width, height, ColorFormat.Rgba16F, false ); 
			irradianceTextureB	=	new Texture2D( rs.Device, width, height, ColorFormat.Rgba16F, false ); 
		}



		public IrradianceMap ( RenderSystem rs, Stream stream )
		{
			this.rs		=	rs;

			using ( var reader = new BinaryReader( stream ) ) {
				
				reader.ExpectFourCC("IRM1", "Bad irradiance map format. IRM1 expected.");
				reader.ExpectFourCC("RGN1", "Bad irradiance map format. RGN1 expected.");

				int count = reader.ReadInt32();

				for ( int i=0; i<count; i++ ) {
					var guid	= reader.Read<Guid>();
					var region	= reader.Read<Rectangle>();
					regions.Add( guid, region );
				}

				reader.ExpectFourCC("MAP1", "Bad irradiance map format. MAP1 expected.");

				width	=	reader.ReadInt32();
				height	=	reader.ReadInt32();

				irradianceTextureR	=	new Texture2D( rs.Device, width, height, ColorFormat.Rgba16F, false ); 
				irradianceTextureG	=	new Texture2D( rs.Device, width, height, ColorFormat.Rgba16F, false ); 
				irradianceTextureB	=	new Texture2D( rs.Device, width, height, ColorFormat.Rgba16F, false ); 
				
				irradianceTextureR.SetData( reader.Read<Half4>( width * height ) );
				irradianceTextureG.SetData( reader.Read<Half4>( width * height ) );
				irradianceTextureB.SetData( reader.Read<Half4>( width * height ) );
			}
		}



		public void WriteToStream ( Stream stream )
		{
			using ( var writer = new BinaryWriter( stream ) ) {

				writer.WriteFourCC("IRM1");

				writer.WriteFourCC("RGN1");

				writer.Write( regions.Count );
				foreach ( var pair in regions ) {
					writer.Write( pair.Key );
					writer.Write( pair.Value );
				}

				writer.WriteFourCC("MAP1");

				writer.Write( width );
				writer.Write( height );

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


		public void DilateRadiance ( GenericImage<Color> albedo )
		{
			irradianceR.Dilate( temporary, (xy) => albedo[xy].A > 0 );
			irradianceG.Dilate( temporary, (xy) => albedo[xy].A > 0 );
			irradianceB.Dilate( temporary, (xy) => albedo[xy].A > 0 );
		}


		public void FillAmbient ( Color4 ambient )
		{
			irradianceR.Fill( new SHL1( ambient.Red,	0,0,0 ) );
			irradianceG.Fill( new SHL1( ambient.Green,	0,0,0 ) );
			irradianceB.Fill( new SHL1( ambient.Blue,	0,0,0 ) );
			UpdateGPUTextures();
		}


		public void AddRegion( Guid guid, Rectangle region )
		{
			regions.Add( guid, region );
		}


		public bool HasRegion ( Guid guid )
		{
			return regions.ContainsKey(guid);
		}


		public Rectangle GetRegion ( Guid guid )
		{
			Rectangle rect;
			if (regions.TryGetValue( guid, out rect ) ) {
				return rect;
			} else {
				return new Rectangle(0,0,0,0);
			}
		}



		public Vector4 GetRegionMadScaleOffset ( Guid guid )
		{
			return GetRegion(guid).GetMadOpScaleOffsetNDC( width, height );
		}
	}
}
