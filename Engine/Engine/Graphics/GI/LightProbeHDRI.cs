using System;
using System.Collections.Generic;
using System.Linq;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Drivers.Graphics;
using Fusion.Core;
using System.IO;
using Fusion.Core.Content;
using Fusion.Engine.Graphics.GI2;

namespace Fusion.Engine.Graphics.Lights {

	[ContentLoader(typeof(LightProbeHDRI))]
	public class LightProbeHDRILoader : ContentLoader {

		public override object Load( ContentManager content, Stream stream, Type requestedType, string assetPath, IStorage storage )
		{
			return new LightProbeHDRI( content.Game.RenderSystem, stream );
		}
	}


	public class LightProbeHDRI : DisposableBase, ILightProbeProvider
	{
		readonly RenderSystem rs;
		internal ShaderResource		Radiance		{ get { return rs.LightMapResources.LightProbeRadianceArray; } }

		readonly Dictionary<string,int> probes = new Dictionary<string,int>();

		readonly public int CubeCount;
		

		/// <summary>
		/// Creates default black irradiance map with no lights
		/// </summary>
		/// <param name="rs"></param>
		/// <param name="stream"></param>
		public LightProbeHDRI ( RenderSystem rs )
		{
			this.rs		=	rs;
		}

		

		/// <summary>
		/// 
		/// </summary>
		/// <param name="rs"></param>
		/// <param name="stream"></param>
		public LightProbeHDRI ( RenderSystem rs, Stream stream )
		{
			this.rs				=	rs;
			var	cubeArrayHdr	=	rs.LightMapResources.LightProbeRadianceArray;

			using ( var reader = new BinaryReader( stream ) ) 
			{
				reader.ExpectFourCC("IRC3", "light probe G-buffer");
				reader.ExpectFourCC("HDRI", "light probe G-buffer");

				CubeCount	=	reader.ReadInt32();

				int size	=	(int)RenderSystem.LightProbeSize;
				var buffer	=	new Color[ size * size ];

				for ( int cubeId = 0; cubeId < CubeCount; cubeId++ ) 
				{
					reader.ExpectFourCC("CUBE", "light probe G-buffer");

					var name		=	reader.ReadString();

					probes.Add( name, cubeId );
					
					for (int mip=0; mip<RenderSystem.LightProbeMaxMips; mip++)
					{
						int mipSize		=	size >> mip;
						int dataSize	=	mipSize * mipSize;

						for (int face=0; face<6; face++) 
						{
							reader.Read( buffer, dataSize );

							cubeArrayHdr.SetData( cubeId, (CubeFace)face, 0, buffer );
						}
					}
				}
			}
		}


		protected override void Dispose( bool disposing )
		{
			if (disposing)
			{
			}

			base.Dispose( disposing );
		}


		public ShaderResource GetLightProbeCubeArray()
		{
			return Radiance;
		}

		
		public int GetLightProbeIndex( string name )
		{
			return GetLightProbeIndex( Guid.Parse(name) );
		}


		public void Update(LightSet lightSet, Camera camera)
		{
			//	do nothing
		}


		public bool HasLightProbe ( Guid guid )
		{
			return probes.ContainsKey(guid.ToString());
		}



		public int GetLightProbeIndex ( Guid guid )
		{
			var name = guid.ToString();
			int index;
			if (probes.TryGetValue( name, out index ) ) 
			{
				return index;
			} 
			else 
			{
				Log.Warning("LightProbe [{0}] not found", name );
				return -1;
			}
		}
	}
}
