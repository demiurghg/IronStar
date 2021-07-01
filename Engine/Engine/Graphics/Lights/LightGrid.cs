﻿using System;
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
using Fusion.Engine.Graphics.GI;


namespace Fusion.Engine.Graphics {
	public class LightGrid : DisposableBase {

		public const int MaxLights			= 4096;
		public const int MaxDecals			= 4096;
		public const int MaxLightProbes	= 256;
		public const int IndexTableSize	= 256 * 512;
		// to cull all light in single pass :
		public const int MaxRadLights	= RadiositySettings.TileSize * RadiositySettings.TileSize;

		public readonly Game Game;
		public readonly int Width;
		public readonly int Height;
		public readonly int Depth;

		readonly RenderSystem rs;

		public int GridLinearSize { get { return Width * Height * Depth; } }
		
		Texture3D		 gridTexture;
		FormattedBuffer  indexBuffer;
		StructuredBuffer lightBuffer;
		StructuredBuffer decalBuffer;
		StructuredBuffer probeBuffer;
		StructuredBuffer radLtBuffer;

		readonly SceneRenderer.LIGHTINDEX[]	lightGrid;
		readonly SceneRenderer.LIGHT[]		lightData;
		readonly SceneRenderer.LIGHT[]		radLtData;
		readonly SceneRenderer.DECAL[]		decalData;
		readonly SceneRenderer.LIGHTPROBE[]	probeData;
		readonly uint[]						indexData;

		internal Texture3D GridTexture { get { return gridTexture;	} }
		internal StructuredBuffer LightDataGpu	{ get { return lightBuffer; } }
		internal StructuredBuffer ProbeDataGpu	{ get { return probeBuffer; } }
		internal StructuredBuffer DecalDataGpu	{ get { return decalBuffer; } }
		internal FormattedBuffer  IndexDataGpu	{ get { return indexBuffer; } }
		internal StructuredBuffer RadLtDataGpu	{ get { return radLtBuffer; } }

		public static float GetGridSlice ( float z )
		{
			var k = RenderSystem.LightClusterExpScale;
				z = Math.Abs( z );
			return 1 - (float)Math.Exp( - k * z );
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

			gridTexture		=	new Texture3D( rs.Device, ColorFormat.Rg32, width, height, depth );
			lightBuffer		=	new StructuredBuffer( rs.Device, typeof(SceneRenderer.LIGHT),		MaxLights,		StructuredBufferFlags.None );
			radLtBuffer		=	new StructuredBuffer( rs.Device, typeof(SceneRenderer.LIGHT),		MaxRadLights,	StructuredBufferFlags.None );
			decalBuffer		=	new StructuredBuffer( rs.Device, typeof(SceneRenderer.DECAL),		MaxDecals,		StructuredBufferFlags.None );
			probeBuffer		=	new StructuredBuffer( rs.Device, typeof(SceneRenderer.LIGHTPROBE),	MaxLightProbes, StructuredBufferFlags.None );
			indexBuffer		=	new FormattedBuffer( rs.Device, Drivers.Graphics.VertexFormat.UInt,	IndexTableSize, StructuredBufferFlags.None ); 

			lightGrid		=	new SceneRenderer.LIGHTINDEX[GridLinearSize];
			lightData		=	new SceneRenderer.LIGHT[MaxLights];
			radLtData		=	new SceneRenderer.LIGHT[MaxRadLights];
			decalData		=	new SceneRenderer.DECAL[MaxDecals];
			probeData		=	new SceneRenderer.LIGHTPROBE[MaxLightProbes];
			indexData		=	new uint[ IndexTableSize ];

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
				SafeDispose( ref indexBuffer );
				SafeDispose( ref lightBuffer );
				SafeDispose( ref probeBuffer );
				SafeDispose( ref decalBuffer );

				SafeDispose( ref radLtBuffer );
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
			rs.extentTest.Clear();

			var view = camera.ViewMatrix;
			var proj = camera.ProjectionMatrix;
			var vpos = camera.CameraMatrix.TranslationVector;

			lightSet.SortLightProbes();

			UpdateOmniLightExtentsAndVisibility( view, proj, lightSet );
			UpdateSpotLightExtentsAndVisibility( view, proj, lightSet, vpos );
			UpdateDecalExtentsAndVisibility( view, proj, lightSet, vpos );
			UpdateLightProbeExtentsAndVisibility( view, proj, lightSet );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="lightSet"></param>
		public void ClusterizeLightSet ( StereoEye stereoEye, Camera camera, LightSet lightSet )
		{
			var view = camera.ViewMatrix;
			var proj = camera.ProjectionMatrix;
			var vpos = camera.CameraMatrix.TranslationVector;

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

				Vector3 min, max;
				ol.Visible	=	false;

				float length	=	Vector3.Distance( ol.Position0, ol.Position1 );
				float radius	=	ol.RadiusOuter + ol.RadiusInner + length;

				if ( Extents.GetSphereExtent( view, proj, ol.CenterPosition, vp, radius, false, out min, out max ) ) {

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

			foreach ( var sl in lightSet.SpotLights ) 
			{
				Vector3 min, max, minF, maxF, minS, maxS;
				sl.Visible	=	false;

				var frustum	=	new BoundingFrustum( sl.SpotView * sl.Projection );

				bool visibleAsFrustum	=	Extents.GetFrustumExtent( view, proj, vp, frustum, false, out minF, out maxF );
				bool visibleAsSphere	=	Extents.GetSphereExtent( view, proj, sl.CenterPosition, vp, sl.RadiusOuter, false, out minS, out maxS );

				if ( visibleAsFrustum && visibleAsSphere )
				{ //*/
					min		=	Vector3.Max( minF, minS );
					max		=	Vector3.Min( maxF, maxS );

					min.Z	=	GetGridSlice( min.Z );
					max.Z	=	GetGridSlice( max.Z );

					sl.Visible		=	!rs.SkipSpotLights;

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
			//	viewer is inside of the spot-light :
			if (frustum.Contains( viewPosition )==ContainmentType.Contains) 
			{
				return spotLight.LodBias;
			}

			var corners		=	frustum.GetCorners();

			//	get frustum center of mass
			var centerMass	= 	( spotLight.CenterPosition + corners[4] + corners[5] + corners[6] + corners[7] ) / 5.0f;

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
		void UpdateDecalExtentsAndVisibility ( Matrix view, Matrix proj, LightSet lightSet, Vector3 viewPos )
		{
			var vp = new Rectangle(0,0,1,1);

			foreach ( var dcl in lightSet.Decals ) {

				var distance	=	Vector3.Distance( dcl.DecalMatrix.TranslationVector, viewPos )+0.1f;

				if (dcl.CharacteristicSize / distance < 0.005f)
				{
					continue;
				}

				Vector3 min, max;
				dcl.Visible	=	false;

				if ( Extents.GetBasisExtent( view, proj, vp, dcl.DecalMatrix, out min, out max ) ) {

					min.Z	=	GetGridSlice( min.Z );
					max.Z	=	GetGridSlice( max.Z );

					TestExtent( min, max, new Color(255,0,0,64) );

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



		void TestExtent ( Vector3 min, Vector3 max, Color color )
		{
			if (rs.ShowExtents) {
				var vp	=	rs.DisplayBounds;
				var x	=	min.X * vp.Width;
				var y	=	min.Y * vp.Height;
				var w	=	(max.X - min.X) * vp.Width;
				var h	=	(max.Y - min.Y) * vp.Height;
				rs.extentTest.Draw( null, x,y,w,h, color );
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

			foreach ( var lpb in lightSet.LightProbes ) {

				Vector3 min, max;
				lpb.Visible	=	false;

				if ( lpb.Mode==LightProbeMode.CubeReflection)
				{
					if ( Extents.GetBasisExtent( view, proj, vp, lpb.ProbeMatrix, out min, out max ) ) 
					{
						min.Z	=	GetGridSlice( min.Z );
						max.Z	=	GetGridSlice( max.Z );

						TestExtent( min, max, new Color(0,0,255,64) );

						lpb.Visible		=	true;

						lpb.MaxExtent.X	=	Math.Min( Width,  (int)Math.Ceiling( max.X * Width  ) );
						lpb.MaxExtent.Y	=	Math.Min( Height, (int)Math.Ceiling( max.Y * Height ) );
						lpb.MaxExtent.Z	=	Math.Min( Depth,  (int)Math.Ceiling( max.Z * Depth  ) );

						lpb.MinExtent.X	=	Math.Max( 0, (int)Math.Floor( min.X * Width  ) );
						lpb.MinExtent.Y	=	Math.Max( 0, (int)Math.Floor( min.Y * Height ) );
						lpb.MinExtent.Z	=	Math.Max( 0, (int)Math.Floor( min.Z * Depth  ) );
					}
				} 
				else if ( lpb.Mode==LightProbeMode.SphereReflection)
				{
					if ( Extents.GetSphereExtent( view, proj, lpb.ProbeMatrix.TranslationVector, vp, lpb.Radius, false, out min, out max ) ) 
					{
						min.Z	=	GetGridSlice( min.Z );
						max.Z	=	GetGridSlice( max.Z );

						lpb.Visible		=	true;

						lpb.MaxExtent.X	=	Math.Min( Width,  (int)Math.Ceiling( max.X * Width  ) );
						lpb.MaxExtent.Y	=	Math.Min( Height, (int)Math.Ceiling( max.Y * Height ) );
						lpb.MaxExtent.Z	=	Math.Min( Depth,  (int)Math.Ceiling( max.Z * Depth  ) );

						lpb.MinExtent.X	=	Math.Max( 0, (int)Math.Floor( min.X * Width  ) );
						lpb.MinExtent.Y	=	Math.Max( 0, (int)Math.Floor( min.Y * Height ) );
						lpb.MinExtent.Z	=	Math.Max( 0, (int)Math.Floor( min.Z * Depth  ) );
					}
				}
				else
				{
					throw new InvalidOperationException("Bad light probe mode");
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


			#region	Compute light and decal count
			foreach ( OmniLight ol in lightSet.OmniLights )
			{
				if (ol.Visible) 
				{
					for (int i=ol.MinExtent.X; i<ol.MaxExtent.X; i++)
					for (int j=ol.MinExtent.Y; j<ol.MaxExtent.Y; j++)
					for (int k=ol.MinExtent.Z; k<ol.MaxExtent.Z; k++) {
						int a = ComputeAddress(i,j,k);
						lightGrid[a].AddLight();
					}
				}
			}

			foreach ( SpotLight sl in lightSet.SpotLights ) 
			{
				if (sl.Visible) 
				{
					for (int i=sl.MinExtent.X; i<sl.MaxExtent.X; i++)
					for (int j=sl.MinExtent.Y; j<sl.MaxExtent.Y; j++)
					for (int k=sl.MinExtent.Z; k<sl.MaxExtent.Z; k++) 
					{
						int a = ComputeAddress(i,j,k);
						lightGrid[a].AddLight();
					}
				}
			}

			foreach ( Decal dcl in lightSet.Decals ) 
			{
				if (dcl.Visible) 
				{
					for (int i=dcl.MinExtent.X; i<dcl.MaxExtent.X; i++)
					for (int j=dcl.MinExtent.Y; j<dcl.MaxExtent.Y; j++)
					for (int k=dcl.MinExtent.Z; k<dcl.MaxExtent.Z; k++) 
					{
						int a = ComputeAddress(i,j,k);
						lightGrid[a].AddDecal();
					}
				}
			}

			foreach ( LightProbe lpb in lightSet.LightProbes ) 
			{
				if (lpb.Visible) 
				{
					for (int i=lpb.MinExtent.X; i<lpb.MaxExtent.X; i++)
					for (int j=lpb.MinExtent.Y; j<lpb.MaxExtent.Y; j++)
					for (int k=lpb.MinExtent.Z; k<lpb.MaxExtent.Z; k++) 
					{
						int a = ComputeAddress(i,j,k);
						lightGrid[a].AddLightProbe();
					}
				}
			}
			#endregion



			uint offset = 0;
			for ( int i=0; i<lightGrid.Length; i++ ) 
			{

				lightGrid[i].Offset = offset;

				offset += lightGrid[i].LightCount;
				offset += lightGrid[i].DecalCount;
				offset += lightGrid[i].ProbeCount;

				lightGrid[i].Count	= 0;
			}



			uint index = 0;
			foreach ( var ol in lightSet.OmniLights ) 
			{
				if (ol.Visible) 
				{
					for (int i=ol.MinExtent.X; i<ol.MaxExtent.X; i++)
					for (int j=ol.MinExtent.Y; j<ol.MaxExtent.Y; j++)
					for (int k=ol.MinExtent.Z; k<ol.MaxExtent.Z; k++) 
					{
						int a = ComputeAddress(i,j,k);
						indexData[ lightGrid[a].Offset + lightGrid[a].TotalCount ] = index;
						lightGrid[a].AddLight();
					}

					lightData[index].FromOmniLight( ol );

					index++;
				}
			}

			foreach ( var sl in lightSet.SpotLights ) 
			{
				if (sl.Visible)
				{
					for (int i=sl.MinExtent.X; i<sl.MaxExtent.X; i++)
					for (int j=sl.MinExtent.Y; j<sl.MaxExtent.Y; j++)
					for (int k=sl.MinExtent.Z; k<sl.MaxExtent.Z; k++) 
					{
						int a = ComputeAddress(i,j,k);
						indexData[ lightGrid[a].Offset + lightGrid[a].TotalCount ] = index;
						lightGrid[a].AddLight();
					}

					lightData[index].FromSpotLight( sl );

					index++;
				}
			}

			index = 0;

			foreach ( var dcl in lightSet.Decals ) 
			{
				if (dcl.Visible) 
				{
					for (int i=dcl.MinExtent.X; i<dcl.MaxExtent.X; i++)
					for (int j=dcl.MinExtent.Y; j<dcl.MaxExtent.Y; j++)
					for (int k=dcl.MinExtent.Z; k<dcl.MaxExtent.Z; k++) 
					{
						int a = ComputeAddress(i,j,k);
						indexData[ lightGrid[a].Offset + lightGrid[a].TotalCount ] = index;
						lightGrid[a].AddDecal();
					}

					decalData[index].FromDecal( dcl, proj.M22, ref screen );

					index++;
				}
			}

			index = 0;

			foreach ( var lpb in lightSet.LightProbes ) 
			{
				if (lpb.Visible) 
				{
					for (int i=lpb.MinExtent.X; i<lpb.MaxExtent.X; i++)
					for (int j=lpb.MinExtent.Y; j<lpb.MaxExtent.Y; j++)
					for (int k=lpb.MinExtent.Z; k<lpb.MaxExtent.Z; k++) 
					{
						int a = ComputeAddress(i,j,k);
						indexData[ lightGrid[a].Offset + lightGrid[a].TotalCount ] = index;
						lightGrid[a].AddLightProbe();
					}

					probeData[index].FromLightProbe( lpb );

					index++;
				}
			}


			//	update GI ligths :
			index = 0;

			for (int i=0; i<MaxRadLights; i++) radLtData[i].ClearLight();

			foreach ( var sl in lightSet.SpotLights ) 
			{ 
				if ( sl.EnableGI )
				{
					radLtData[index].FromSpotLight( sl ); 
					index++;

					if (index>=MaxLights)
					{
						Log.Warning("Too much GI lights");
						break;
					}
				}
			}


			using ( new PixEvent( "Update cluster structures" ) ) 
			{
				LightDataGpu.UpdateData	( lightData );
				DecalDataGpu.UpdateData	( decalData );
				IndexDataGpu.UpdateData	( indexData );
				ProbeDataGpu.UpdateData	( probeData );
				RadLtDataGpu.UpdateData	( radLtData );
				gridTexture	.SetData	( lightGrid );
			}
		}



	}
}
