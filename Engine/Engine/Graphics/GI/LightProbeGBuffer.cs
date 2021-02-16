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

namespace Fusion.Engine.Graphics.GI {

	[ContentLoader(typeof(LightProbeGBuffer))]
	public class LightProbeGBufferLoader : ContentLoader {

		public override object Load( ContentManager content, Stream stream, Type requestedType, string assetPath, IStorage storage )
		{
			return new LightProbeGBuffer( content.Game.RenderSystem, stream );
		}
	}


	public class LightProbeGBuffer : DisposableBase, ILightProbeProvider
	{
		readonly RenderSystem rs;
		internal ShaderResource		Radiance		{ get { return rs.LightMapResources.LightProbeRadianceArray; } }
		internal TextureCubeArray	GBufferColor	{ get { return rs.LightMapResources.LightProbeColorArray; } }
		internal TextureCubeArray	GBufferMapping	{ get { return rs.LightMapResources.LightProbeMappingArray; } }

		readonly Dictionary<string,int> probes = new Dictionary<string,int>();

		readonly public int CubeCount;
		

		/// <summary>
		/// Creates default black irradiance map with no lights
		/// </summary>
		/// <param name="rs"></param>
		/// <param name="stream"></param>
		public LightProbeGBuffer ( RenderSystem rs )
		{
			this.rs		=	rs;
		}

		

		/// <summary>
		/// 
		/// </summary>
		/// <param name="rs"></param>
		/// <param name="stream"></param>
		public LightProbeGBuffer ( RenderSystem rs, Stream stream )
		{
			this.rs		=	rs;

			using ( var reader = new BinaryReader( stream ) ) 
			{
				reader.ExpectFourCC("IRC3", "light probe G-buffer");
				reader.ExpectFourCC("GBUF", "light probe G-buffer");

				CubeCount	=	reader.ReadInt32();

				int size	=	(int)RenderSystem.LightProbeSize;
				var buffer	=	new Color[ size * size ];

				for ( int cubeId = 0; cubeId < CubeCount; cubeId++ ) 
				{
					reader.ExpectFourCC("CUBE", "light probe G-buffer");

					var name		=	reader.ReadString();
					int mipSize		=	size;
					int dataSize	=	mipSize * mipSize;

					probes.Add( name, cubeId );
					
					for (int face=0; face<6; face++) 
					{
						reader.Read( buffer, dataSize );
						GBufferColor.SetData( cubeId, (CubeFace)face, 0, buffer );
					}

					for (int face=0; face<6; face++) 
					{
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
			rs.LightProbeRelighter.RelightLightProbes(lightSet, camera);
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
