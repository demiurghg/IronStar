using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Configuration;
using Fusion.Engine.Common;
using Fusion.Drivers.Graphics;
using System.Runtime.InteropServices;
using Fusion.Engine.Graphics.Lights;


namespace Fusion.Engine.Graphics {
	public class LightGrid : DisposableBase {

		const int MaxLights = 4096;
		const int MaxDecals = 4096;
		const int IndexTableSize = 256 * 512;

		public readonly Game Game;
		public readonly int Width;
		public readonly int Height;
		public readonly int Depth;

		readonly RenderSystem rs;

		public int GridLinearSize { get { return Width * Height * Depth; } }
		
		Texture3D gridTexture;
		FormattedBuffer  indexData;
		StructuredBuffer lightData;
		StructuredBuffer decalData;

		internal Texture3D GridTexture { get { return gridTexture;	} }
		internal StructuredBuffer LightDataGpu { get { return lightData; } }
		internal StructuredBuffer DecalDataGpu { get { return decalData; } }
		internal FormattedBuffer  IndexDataGpu { get { return indexData; } }


		static float GetGridSlice ( float z )
		{
			return 1 - (float)Math.Exp( 0.03f * ( z ) );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="rs"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="depth"></param>
		public LightGrid ( RenderSystem rs, int width, int height, int depth )
		{
			this.rs	=	rs;
			Game	=	rs.Game;
			Width	=	width;
			Height	=	height;
			Depth	=	depth;

			gridTexture	=	new Texture3D( rs.Device, ColorFormat.Rg32, width, height, depth );

			lightData	=	new StructuredBuffer( rs.Device, typeof(SceneRenderer.LIGHT), MaxLights, StructuredBufferFlags.None );
			decalData	=	new StructuredBuffer( rs.Device, typeof(SceneRenderer.DECAL), MaxDecals, StructuredBufferFlags.None );
			indexData	=	new FormattedBuffer( rs.Device, Drivers.Graphics.VertexFormat.UInt, IndexTableSize, StructuredBufferFlags.None ); 

			var rand = new Random();
			var data = new Int2[GridLinearSize];

			for (int i=0; i<Width; i++) {
				for (int j=0; j<Height; j++) {
					for (int k=0; k<Depth; k++) {
						int a = ComputeAddress(i,j,k);
						data[a] = new Int2(rand.Next(10),rand.Next(10));
					}
				}
			}

			gridTexture.SetData( data );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="z"></param>
		/// <returns></returns>
		int	ComputeAddress ( int x, int y, int z ) 
		{
			return x + y * Width + z * Height*Width;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose( bool disposing )
		{
			if (disposing) {
				SafeDispose( ref gridTexture );
				SafeDispose( ref indexData );
				SafeDispose( ref lightData );
				SafeDispose( ref decalData );
			}

			base.Dispose( disposing );
		}



		/// <summary>
		/// Call this method before building shadows!
		/// </summary>
		/// <param name="stereoEye"></param>
		/// <param name="camera"></param>
		/// <param name="lightSet"></param>
		public void UpdateLightSetVisibility ( StereoEye stereoEye, Camera camera, LightSet lightSet )
		{
			var view = camera.GetViewMatrix( stereoEye );
			var proj = camera.GetProjectionMatrix( stereoEye );
			var vpos = camera.GetCameraMatrix( StereoEye.Mono ).TranslationVector;

			UpdateOmniLightExtentsAndVisibility( view, proj, lightSet );
			UpdateSpotLightExtentsAndVisibility( view, proj, lightSet, vpos );
			UpdateDecalExtentsAndVisibility( view, proj, lightSet );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="lightSet"></param>
		public void ClusterizeLightSet ( StereoEye stereoEye, Camera camera, LightSet lightSet )
		{
			var view = camera.GetViewMatrix( stereoEye );
			var proj = camera.GetProjectionMatrix( stereoEye );
			var vpos = camera.GetCameraMatrix( StereoEye.Mono ).TranslationVector;

			ClusterizeLightsAndDecals( view, proj, lightSet );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="view"></param>
		/// <param name="proj"></param>
		/// <param name="lightSet"></param>
		void UpdateOmniLightExtentsAndVisibility ( Matrix view, Matrix proj, LightSet lightSet )
		{
			var vp = new Rectangle(0,0,1,1);

			foreach ( var ol in lightSet.OmniLights ) {

				Vector4 min, max;
				ol.Visible	=	false;

				if ( Extents.GetSphereExtent( view, proj, ol.Position, vp, ol.RadiusOuter, false, out min, out max ) ) {

					min.Z	=	GetGridSlice( min.Z );
					max.Z	=	GetGridSlice( max.Z );

					ol.Visible		=	true;

					ol.MaxExtent.X	=	Math.Min( Width,  (int)Math.Ceiling( max.X * Width  ) );
					ol.MaxExtent.Y	=	Math.Min( Height, (int)Math.Ceiling( max.Y * Height ) );
					ol.MaxExtent.Z	=	Math.Min( Depth,  (int)Math.Ceiling( max.Z * Depth  ) );

					ol.MinExtent.X	=	Math.Max( 0, (int)Math.Floor( min.X * Width  ) );
					ol.MinExtent.Y	=	Math.Max( 0, (int)Math.Floor( min.Y * Height ) );
					ol.MinExtent.Z	=	Math.Max( 0, (int)Math.Floor( min.Z * Depth  ) );
				}
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="view"></param>
		/// <param name="proj"></param>
		/// <param name="lightSet"></param>
		void UpdateSpotLightExtentsAndVisibility ( Matrix view, Matrix proj, LightSet lightSet, Vector3 viewPosition )
		{
			var vp = new Rectangle(0,0,1,1);

			foreach ( var sl in lightSet.SpotLights ) {

				Vector4 min, max;
				sl.Visible	=	false;

				var frustum	=	new BoundingFrustum( sl.SpotView * sl.Projection );

				if ( Extents.GetSphereExtent( view, proj, sl.Position, vp, sl.RadiusOuter, false, out min, out max ) ) {

					min.Z	=	GetGridSlice( min.Z );
					max.Z	=	GetGridSlice( max.Z );

					sl.Visible		=	true;

					sl.DetailLevel	=	GetSpotLightLOD( sl, frustum, viewPosition );

					sl.MaxExtent.X	=	Math.Min( Width,  (int)Math.Ceiling( max.X * Width  ) );
					sl.MaxExtent.Y	=	Math.Min( Height, (int)Math.Ceiling( max.Y * Height ) );
					sl.MaxExtent.Z	=	Math.Min( Depth,  (int)Math.Ceiling( max.Z * Depth  ) );

					sl.MinExtent.X	=	Math.Max( 0, (int)Math.Floor( min.X * Width  ) );
					sl.MinExtent.Y	=	Math.Max( 0, (int)Math.Floor( min.Y * Height ) );
					sl.MinExtent.Z	=	Math.Max( 0, (int)Math.Floor( min.Z * Depth  ) );
				}
			}
		}
				  


		/// <summary>
		/// 
		/// </summary>
		/// <param name="spotLight"></param>
		/// <param name="viewPosition"></param>
		/// <returns></returns>
		int GetSpotLightLOD ( SpotLight spotLight, BoundingFrustum frustum, Vector3 viewPosition )
		{
			if (frustum.Contains( viewPosition )==ContainmentType.Contains) {
				return spotLight.LodBias;
			}

			var corners		=	frustum.GetCorners();

			//	get frustum center of mass
			var centerMass	= 	( spotLight.Position + corners[4] + corners[5] + corners[6] + corners[7] ) / 5.0f;

			//	get size of light spot
			var spotSize	=	Vector3.Distance( corners[4], corners[6] ) + 0.01f;

			//	get distance between viewing point and spot light
			var	distance	=	Vector3.Distance( viewPosition, centerMass );

			//	push distance
			distance		=	Math.Max( 0, distance + spotSize );

			//	compute LOD :
			var lod			=	(int)Math.Log( distance / spotSize, 2 );

			//Log.Message("LOD : {0} - {1}/{2}", lod, distance, spotSize );

			return Math.Max(0, lod + spotLight.LodBias);
		}




		/// <summary>
		/// 
		/// </summary>
		/// <param name="view"></param>
		/// <param name="proj"></param>
		/// <param name="lightSet"></param>
		void UpdateDecalExtentsAndVisibility ( Matrix view, Matrix proj, LightSet lightSet )
		{
			var vp = new Rectangle(0,0,1,1);

			foreach ( var dcl in lightSet.Decals ) {

				Vector4 min, max;
				dcl.Visible	=	false;

				if ( Extents.GetBasisExtent( view, proj, vp, dcl.DecalMatrix, false, out min, out max ) ) {

					min.Z	=	GetGridSlice( -min.Z );
					max.Z	=	GetGridSlice( -max.Z );

					dcl.Visible		=	true;

					dcl.MaxExtent.X	=	Math.Min( Width,  (int)Math.Ceiling( max.X * Width  ) );
					dcl.MaxExtent.Y	=	Math.Min( Height, (int)Math.Ceiling( max.Y * Height ) );
					dcl.MaxExtent.Z	=	Math.Min( Depth,  (int)Math.Ceiling( max.Z * Depth  ) );

					dcl.MinExtent.X	=	Math.Max( 0, (int)Math.Floor( min.X * Width  ) );
					dcl.MinExtent.Y	=	Math.Max( 0, (int)Math.Floor( min.Y * Height ) );
					dcl.MinExtent.Z	=	Math.Max( 0, (int)Math.Floor( min.Z * Depth  ) );
				}
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="view"></param>
		/// <param name="proj"></param>
		/// <param name="lightSet"></param>
		void UpdateLightProbeExtentsAndVisibility ( Matrix view, Matrix proj, LightSet lightSet )
		{
			var vp = new Rectangle(0,0,1,1);

			foreach ( var lightProbe in lightSet.EnvLights ) {

				Vector4 min, max;
				lightProbe.Visible	=	false;

				if ( Extents.GetBasisExtent( view, proj, vp, Matrix.Identity, false, out min, out max ) ) {

					min.Z	=	GetGridSlice( -min.Z );
					max.Z	=	GetGridSlice( -max.Z );

					lightProbe.Visible		=	true;

					lightProbe.MaxExtent.X	=	Math.Min( Width,  (int)Math.Ceiling( max.X * Width  ) );
					lightProbe.MaxExtent.Y	=	Math.Min( Height, (int)Math.Ceiling( max.Y * Height ) );
					lightProbe.MaxExtent.Z	=	Math.Min( Depth,  (int)Math.Ceiling( max.Z * Depth  ) );

					lightProbe.MinExtent.X	=	Math.Max( 0, (int)Math.Floor( min.X * Width  ) );
					lightProbe.MinExtent.Y	=	Math.Max( 0, (int)Math.Floor( min.Y * Height ) );
					lightProbe.MinExtent.Z	=	Math.Max( 0, (int)Math.Floor( min.Z * Depth  ) );
				}
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="lightSet"></param>
		void ClusterizeLightsAndDecals ( Matrix view, Matrix proj, LightSet lightSet )
		{
			var screen = rs.DisplayBounds;
			var vp = new Rectangle(0,0,1,1);

			var lightGrid	=	new SceneRenderer.LIGHTINDEX[GridLinearSize];
			var lightData	=	new SceneRenderer.LIGHT[MaxLights];
			var decalData	=	new SceneRenderer.DECAL[MaxDecals];

			#region	Compute light and decal count
			foreach ( OmniLight ol in lightSet.OmniLights ) {
				if (ol.Visible) {
					for (int i=ol.MinExtent.X; i<ol.MaxExtent.X; i++)
					for (int j=ol.MinExtent.Y; j<ol.MaxExtent.Y; j++)
					for (int k=ol.MinExtent.Z; k<ol.MaxExtent.Z; k++) {
						int a = ComputeAddress(i,j,k);
						lightGrid[a].AddLight();
					}
				}
			}

			foreach ( SpotLight sl in lightSet.SpotLights ) {
				if (sl.Visible) {
					for (int i=sl.MinExtent.X; i<sl.MaxExtent.X; i++)
					for (int j=sl.MinExtent.Y; j<sl.MaxExtent.Y; j++)
					for (int k=sl.MinExtent.Z; k<sl.MaxExtent.Z; k++) {
						int a = ComputeAddress(i,j,k);
						lightGrid[a].AddLight();
					}
				}
			}

			foreach ( Decal dcl in lightSet.Decals ) {
				if (dcl.Visible) {
					for (int i=dcl.MinExtent.X; i<dcl.MaxExtent.X; i++)
					for (int j=dcl.MinExtent.Y; j<dcl.MaxExtent.Y; j++)
					for (int k=dcl.MinExtent.Z; k<dcl.MaxExtent.Z; k++) {
						int a = ComputeAddress(i,j,k);
						lightGrid[a].AddDecal();
					}
				}
			}

			foreach ( EnvLight lpb in lightSet.EnvLights ) {
				if (lpb.Visible) {
					for (int i=lpb.MinExtent.X; i<lpb.MaxExtent.X; i++)
					for (int j=lpb.MinExtent.Y; j<lpb.MaxExtent.Y; j++)
					for (int k=lpb.MinExtent.Z; k<lpb.MaxExtent.Z; k++) {
						int a = ComputeAddress(i,j,k);
						lightGrid[a].AddLightProbe();
					}
				}
			}
			#endregion



			uint offset = 0;
			for ( int i=0; i<lightGrid.Length; i++ ) {

				lightGrid[i].Offset = offset;

				offset += lightGrid[i].LightCount;
				offset += lightGrid[i].DecalCount;

				lightGrid[i].Count	= 0;
			}

			var indexData	=	new uint[ offset + 1 /* one extra element */ ];


			uint index = 0;
			foreach ( var ol in lightSet.OmniLights ) {
				if (ol.Visible) {
					for (int i=ol.MinExtent.X; i<ol.MaxExtent.X; i++)
					for (int j=ol.MinExtent.Y; j<ol.MaxExtent.Y; j++)
					for (int k=ol.MinExtent.Z; k<ol.MaxExtent.Z; k++) {
						int a = ComputeAddress(i,j,k);
						indexData[ lightGrid[a].Offset + lightGrid[a].TotalCount ] = index;
						lightGrid[a].AddLight();
					}

					lightData[index].FromOmniLight( ol );

					index++;
				}
			}

			foreach ( var sl in lightSet.SpotLights ) {
				if (sl.Visible) {
					for (int i=sl.MinExtent.X; i<sl.MaxExtent.X; i++)
					for (int j=sl.MinExtent.Y; j<sl.MaxExtent.Y; j++)
					for (int k=sl.MinExtent.Z; k<sl.MaxExtent.Z; k++) {
						int a = ComputeAddress(i,j,k);
						indexData[ lightGrid[a].Offset + lightGrid[a].TotalCount ] = index;
						lightGrid[a].AddLight();
					}

					lightData[index].FromSpotLight( sl );

					index++;
				}
			}

			foreach ( var dcl in lightSet.Decals ) {
				if (dcl.Visible) {
					for (int i=dcl.MinExtent.X; i<dcl.MaxExtent.X; i++)
					for (int j=dcl.MinExtent.Y; j<dcl.MaxExtent.Y; j++)
					for (int k=dcl.MinExtent.Z; k<dcl.MaxExtent.Z; k++) {
						int a = ComputeAddress(i,j,k);
						indexData[ lightGrid[a].Offset + lightGrid[a].TotalCount ] = index;
						lightGrid[a].AddDecal();
					}

					decalData[index].FromDecal( dcl, proj.M22, ref screen );

					index++;
				}
			}


			using ( new PixEvent( "Update cluster structures" ) ) {
				LightDataGpu.SetData( lightData );
				DecalDataGpu.SetData( decalData );
				IndexDataGpu.SetData( indexData );
				gridTexture.SetData( lightGrid );
			}
		}



	}
}
