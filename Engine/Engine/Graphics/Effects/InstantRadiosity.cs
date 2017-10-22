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
using System.Diagnostics;
using Fusion.Core.Extensions;
using Fusion.Engine.Graphics.Ubershaders;

namespace Fusion.Engine.Graphics {

	[RequireShader("instrad", true)]
	[ShaderSharedStructure(typeof(SceneRenderer.LIGHT), typeof(SceneRenderer.LIGHTINDEX))]
	internal partial class InstantRadiosity : RenderComponent {

		[ShaderDefine]
		const int MaxSurfels	=	64*1024;

		[ShaderDefine]
		const int BlockSize		=	256;

		[ShaderDefine]
		const int BlockSize3D	=	4;


		[Flags]
		enum Flags : int
		{
			LIGHTEN		= 0x0001,
			DRAW		= 0x0002,
			LIGHTMAP	= 0x0004,
		}


		[ShaderStructure]
		struct SURFEL {
			public Vector4 Position;
			public Vector4 NormalArea;
			public Vector4 Intensity;
		}


		[ShaderStructure]
		[StructLayout(LayoutKind.Sequential, Pack=4, Size=512)]
		struct LIGHTENPARAMS {
			public Matrix	CascadeViewProjection0	;
			public Matrix	CascadeViewProjection1	;
			public Matrix	CascadeViewProjection2	;
			public Matrix	CascadeViewProjection3	;
			public Vector4	CascadeScaleOffset0		;
			public Vector4	CascadeScaleOffset1		;
			public Vector4	CascadeScaleOffset2		;
			public Vector4	CascadeScaleOffset3		;
			public Vector4	DirectLightDirection	;
			public Color4	DirectLightIntensity	;
			public int		LightCount				;
		}


		/// <summary>
		/// structure size must be the same 
		/// because share same constant buffer object
		/// </summary>
		[ShaderStructure]
		[StructLayout(LayoutKind.Sequential, Pack=4, Size=512)]
		struct DRAWPARAMS {
			public Matrix 	ViewProjection;
		}


		/// <summary>
		/// structure size must be the same 
		/// because share same constant buffer object
		/// </summary>
		[ShaderStructure]
		[StructLayout(LayoutKind.Sequential, Pack=4, Size=512)]
		struct LMPARAMS {
			public Matrix 	ViewProjection;
		}


		Ubershader			shader;
		StateFactory		factory;

		ConstantBuffer		paramsCB;

		Texture3DCompute	lightmap;

		StructuredBuffer	surfels;

		public Texture3DCompute AmbientLightMap {
			get {
				return lightmap;
			}
		}



		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="rs"></param>
		public InstantRadiosity ( RenderSystem rs ) : base( rs )
		{
		}



		/// <summary>
		/// 
		/// </summary>
		public override void Initialize() 
		{
			lightmap	=	new Texture3DCompute( device, 64, 64, 64 );
			
			paramsCB	=	new ConstantBuffer( device, typeof(LIGHTENPARAMS) );

			surfels		=	new StructuredBuffer( device, typeof(SURFEL), MaxSurfels, StructuredBufferFlags.None );

			LoadContent();

			Game.Reloading += (s,e) => LoadContent();
		}



		/// <summary>
		/// 
		/// </summary>
		void LoadContent ()
		{
			shader		=	Game.Content.Load<Ubershader>("instrad");
			factory		=	shader.CreateFactory( typeof(Flags), (ps,i) => EnumAction( ps, (Flags)i ) );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="ps"></param>
		/// <param name="flag"></param>
		void EnumAction ( PipelineState ps, Flags flag )
		{
			if (flag==Flags.DRAW) {
				ps.BlendState			=	BlendState.AlphaBlend;
				ps.DepthStencilState	=	DepthStencilState.Readonly;
				ps.Primitive			=	Primitive.PointList;
				ps.RasterizerState		=	RasterizerState.CullNone;
			}

			if (flag==Flags.LIGHTEN) {
			}
		}



		/// <summary>
		/// 
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing ) {
				SafeDispose( ref lightmap );
				SafeDispose( ref paramsCB );
				SafeDispose( ref surfels  );
			}
			base.Dispose( disposing );
		}





		/// <summary>
		/// Renders fog look-up table
		/// </summary>
		internal void RenderIRS( Camera camera, IEnumerable<MeshInstance> instances, LightSet lightSet )
		{
			CollectSurfels( instances );

			using ( new PixEvent("IRS") ) {

				LightenSurfels(); 

				RenderLightmap();
				
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="lightView"></param>
		/// <param name="lightProj"></param>
		/// <param name="lightPos"></param>
		/// <param name="lightColor"></param>
		void LightenSurfels ()
		{
			using ( new PixEvent("Lighten") ) {

				device.ResetStates();		  

				var param				=	new LIGHTENPARAMS();
				var shadowMap			=	rs.LightManager.ShadowMap.ColorBuffer;

				param.DirectLightDirection		=	new Vector4( rs.RenderWorld.LightSet.DirectLight.Direction, 0 );
				param.DirectLightIntensity		=	rs.RenderWorld.LightSet.DirectLight.Intensity;

				param.CascadeViewProjection0	=	rs.LightManager.ShadowMap.GetCascade( 0 ).ViewProjectionMatrix;
				param.CascadeViewProjection1	=	rs.LightManager.ShadowMap.GetCascade( 1 ).ViewProjectionMatrix;
				param.CascadeViewProjection2	=	rs.LightManager.ShadowMap.GetCascade( 2 ).ViewProjectionMatrix;
				param.CascadeViewProjection3	=	rs.LightManager.ShadowMap.GetCascade( 3 ).ViewProjectionMatrix;
			
				param.CascadeScaleOffset0		=	rs.LightManager.ShadowMap.GetCascade( 0 ).ShadowScaleOffset;
				param.CascadeScaleOffset1		=	rs.LightManager.ShadowMap.GetCascade( 1 ).ShadowScaleOffset;
				param.CascadeScaleOffset2		=	rs.LightManager.ShadowMap.GetCascade( 2 ).ShadowScaleOffset;
				param.CascadeScaleOffset3		=	rs.LightManager.ShadowMap.GetCascade( 3 ).ShadowScaleOffset;

				paramsCB.SetData( param );

				device.PipelineState	=	factory[ (int)Flags.LIGHTEN ];

				device.ComputeShaderResources[0]	=	shadowMap;
				device.ComputeShaderResources[1]	=	rs.LightManager.LightGrid.LightDataGpu;
				device.ComputeShaderConstants[0]	=	paramsCB;
				device.ComputeShaderSamplers[0]		=	SamplerState.ShadowSampler;

				device.SetCSRWBuffer( 0, surfels );

				var gx	=	MathUtil.IntDivUp( MaxSurfels, BlockSize );
				var gy	=	1;
				var gz	=	1;

				device.Dispatch( gx, gy, gz );
			}
		}



		public void RenderLightmap ()
		{
			using ( new PixEvent("Lightmap") ) {

				device.ResetStates();		  

				var param				=	new LMPARAMS();
				var shadowMap			=	rs.LightManager.ShadowMap.ColorBuffer;

				paramsCB.SetData( param );

				device.PipelineState	=	factory[ (int)Flags.LIGHTMAP ];

				device.ComputeShaderResources[0]	=	surfels;

				device.SetCSRWTexture( 0, lightmap );

				var gx	=	MathUtil.IntDivUp( 64, BlockSize3D );
				var gy	=	MathUtil.IntDivUp( 64, BlockSize3D );
				var gz	=	MathUtil.IntDivUp( 64, BlockSize3D );

				device.Dispatch( gx, gy, gz );
			}
		}



		public void DrawDebugSurfels ( HdrFrame hdrFrame, Camera camera, StereoEye stereoEye )
		{
			using ( new PixEvent("DrawDebug") ) {

				device.ResetStates();	
				
				device.SetTargets( hdrFrame.DepthBuffer, hdrFrame.HdrBuffer );	  

				DRAWPARAMS param;

				var view				=	camera.GetViewMatrix( stereoEye );	
				var projection			=	camera.GetProjectionMatrix( stereoEye );	

				param.ViewProjection	=	view * projection;

				paramsCB.SetData( param );
			
				device.PipelineState	=	factory[ (int)Flags.DRAW ];

				device.VertexShaderResources[0]		=	surfels;
				device.VertexShaderConstants[0]		=	paramsCB;
				device.GeometryShaderResources[0]	=	surfels;
				device.GeometryShaderConstants[0]	=	paramsCB;
				device.PixelShaderResources[0]		=	surfels;
				device.PixelShaderConstants[0]		=	paramsCB;

				device.PixelShaderSamplers[0]		=	SamplerState.LinearWrap;

				var gx	=	MathUtil.IntDivUp( MaxSurfels, BlockSize );
				var gy	=	1;
				var gz	=	1;

				device.Draw( MaxSurfels, 0 );
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="instances"></param>
		public void CollectSurfels ( IEnumerable<MeshInstance> instances )
		{
			var surfelData	=	new SURFEL[ MaxSurfels ];
			var counter		=	0;

			foreach ( var instance in instances ) {

				if (instance.Mesh==null) {
					continue;
				}

				if (!instance.Mesh.Surfels.Any()) {
					instance.Mesh.BuildSurfels(2.0f);
				}

				foreach ( var surfel in instance.Mesh.Surfels ) {

					var		p	=	Vector3.TransformCoordinate	( surfel.Position, instance.World );
					var		n	=	Vector3.TransformNormal		( surfel.Normal, instance.World );
					
					surfelData[ counter ].Position		=	new Vector4( p, 1 );
					surfelData[ counter ].NormalArea	=	new Vector4( n, surfel.Area );
					surfelData[ counter ].Intensity		=	new Vector4( 0,0,0,0 );

					counter++;

					if (counter>=MaxSurfels) {
						Log.Warning("Too much surfels");
						break;
					}
				}

			}

			surfels.SetData( surfelData );
		}

	}
}
