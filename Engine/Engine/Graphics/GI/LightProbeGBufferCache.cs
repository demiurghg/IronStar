using System;
using System.Collections.Generic;
using System.Linq;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Drivers.Graphics;
using Fusion.Core;
using System.IO;
using Fusion.Core.Content;

namespace Fusion.Engine.Graphics.Lights {

	[ContentLoader(typeof(LightProbeGBufferCache))]
	public class IrradianceCacheLoader : ContentLoader {

		public override object Load( ContentManager content, Stream stream, Type requestedType, string assetPath, IStorage storage )
		{
			return new LightProbeGBufferCache( content.Game.RenderSystem, stream );
		}
	}


	public class LightProbeGBufferCache : DisposableBase 
	{
		readonly RenderSystem rs;
		internal ShaderResource		Radiance		{ get { return rs.LightMapResources.LightProbeRadianceArray; } }
		internal TextureCubeArray	GBufferColor	{ get { return rs.LightMapResources.LightProbeColorArray; } }
		internal TextureCubeArray	GBufferMapping	{ get { return rs.LightMapResources.LightProbeMappingArray; } }

		readonly Dictionary<Guid,int> probes = new Dictionary<Guid,int>();

		readonly public int CubeCount;
		

		/// <summary>
		/// Creates default black irradiance map with no lights
		/// </summary>
		/// <param name="rs"></param>
		/// <param name="stream"></param>
		public LightProbeGBufferCache ( RenderSystem rs )
		{
			this.rs		=	rs;
		}

		

		/// <summary>
		/// 
		/// </summary>
		/// <param name="rs"></param>
		/// <param name="stream"></param>
		public LightProbeGBufferCache ( RenderSystem rs, Stream stream )
		{
			this.rs		=	rs;

			using ( var reader = new BinaryReader( stream ) ) 
			{
				reader.ExpectFourCC("IRC2", "light probe G-buffer");

				CubeCount	=	reader.ReadInt32();

				int size	=	(int)RenderSystem.LightProbeSize;
				var buffer	=	new Color[ size * size ];

				for ( int cubeId = 0; cubeId < CubeCount; cubeId++ ) 
				{
					reader.ExpectFourCC("CUBE", "light probe G-buffer");

					var guid	=	reader.Read<Guid>();

					probes.Add( guid, cubeId );
					
					for (int face=0; face<6; face++) 
					{
						int mipSize		= size;
						int dataSize	= mipSize * mipSize;

						reader.Read( buffer, dataSize );
						GBufferColor.SetData( cubeId, (CubeFace)face, 0, buffer );

						reader.Read( buffer, dataSize );
						GBufferMapping.SetData( cubeId, (CubeFace)face, 0, buffer );
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



		public bool HasLightProbe ( Guid guid )
		{
			return probes.ContainsKey(guid);
		}



		public int GetLightProbeIndex ( Guid guid )
		{
			int index;
			if (probes.TryGetValue( guid, out index ) ) 
			{
				return index;
			} 
			else 
			{
				Log.Warning("LightProbe [{0}] not found", guid );
				return -1;
			}
		}


		internal void UpdateLightProbe ( Guid guid, RenderTargetCube renderTargetCube )
		{
			int index = GetLightProbeIndex( guid );
			
			if (index<0) 
			{
				return;
			}

			GBufferColor.CopyFromRenderTargetCube( index, renderTargetCube );
		}
	}
}
