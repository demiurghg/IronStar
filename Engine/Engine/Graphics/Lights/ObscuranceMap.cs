using System;
using System.Collections.Generic;
using System.Linq;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Drivers.Graphics;
using Fusion.Engine.Graphics.Ubershaders;
using Native.Embree;
using System.Runtime.InteropServices;

namespace Fusion.Engine.Graphics.Lights {

	[RequireShader("obscurance", true)]
	internal class ObscuranceMap : RenderComponent {

		[ShaderDefine]
		const int BlockSizeX = 4;

		[ShaderDefine]
		const int BlockSizeY = 4;

		[ShaderDefine]
		const int BlockSizeZ = 4;

		[ShaderStructure()]
		[StructLayout(LayoutKind.Sequential, Pack=4, Size=192)]
		struct BAKE_PARAMS {
			public	Matrix	ShadowViewProjection;
			public	Matrix	OcclusionGridTransform;
			public	Vector4 LightDirection;
		}


		public Texture3DCompute OcclusionGrid		{ get { return occlusionGrid0; }	}

		Texture3DCompute	occlusionGrid0;
		Texture3DCompute	occlusionGrid1;
		ConstantBuffer		constBuffer;
		StateFactory		factory;
		Ubershader			shader;


		enum Flags {
			BAKE,
			COPY,
		}


		const int	Width		=	128;
		const int	Height		=	64;
		const int	Depth		=	128;
		const float GridStep	=	2.0f;

		readonly Int3 GridSize	=	new Int3( Width, Height, Depth );
		readonly Int3 BlockSize	=	new Int3( BlockSizeX, BlockSizeY, BlockSizeZ );	


		public Matrix OcclusionGridMatrix {
			get {
				return	Matrix.Identity
					*	Matrix.Translation( Width/2.0f*GridStep, 0, Depth/2.0f*GridStep )
					*	Matrix.Translation( 0.5f*GridStep, 0.5f*GridStep, 0.5f*GridStep )
					*	Matrix.Scaling( 1.0f/Width, 1.0f / Height, 1.0f / Depth ) 
					*	Matrix.Scaling( 1.0f/GridStep )
					;
			}
		}


		/// <summary>
		/// 
		/// </summary>
		public ObscuranceMap(RenderSystem rs) : base(rs)
		{
			occlusionGrid0	=	new Texture3DCompute( rs.Device, Width,Height,Depth );
			occlusionGrid1	=	new Texture3DCompute( rs.Device, Width,Height,Depth );
			constBuffer		=	new ConstantBuffer( rs.Device, typeof(BAKE_PARAMS) );

			LoadContent();

			Game.Reloading += (s,e) => LoadContent();
		}


		/// <summary>
		/// 
		/// </summary>
		void LoadContent ()
		{
			SafeDispose( ref factory );

			shader	=	Game.Content.Load<Ubershader>("obscurance");
			factory	=	shader.CreateFactory( typeof(Flags) );
		}


		/// <summary>
		/// 
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if (disposing) {
				SafeDispose( ref factory );
				SafeDispose( ref occlusionGrid0 );
				SafeDispose( ref occlusionGrid1 );
				SafeDispose( ref constBuffer );
			}

			base.Dispose( disposing );
		}


		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Occlusion grid stuff (GPU version):
		 * 
		-----------------------------------------------------------------------------------------*/

		public void UpdateIrradianceMapGPU ( IEnumerable<MeshInstance> instances, LightSet lightSet, DebugRender dr, int numSamples )
		{
			Log.Message("Building ambient occlusion map (GPU)");

			Log.Message("...initialization");

			var device	=	rs.Device;

			hemisphereRandomPoints	= Enumerable.Range(0,numSamples)
					.Select( i => Hammersley.HemisphereUniform(i,numSamples) )
					.ToArray();

			var depthBuffer	=	new DepthStencil2D( rs.Device, DepthFormat.D24S8, 1024, 1024 );
			var colorBuffer	=	new RenderTarget2D( rs.Device, ColorFormat.R32F, 1024, 1024 );

			occlusionGrid0.Clear(Vector4.Zero);
			occlusionGrid1.Clear(Vector4.Zero);

			//------------------------------------------

			Log.Message("...baking : {0} samples", numSamples);

			for (int i=0; i<numSamples; i++) {

				device.Clear( colorBuffer.Surface, Color4.Zero );
				device.Clear( depthBuffer.Surface, 1, 0 ); 

				var lightDir	=	hemisphereRandomPoints[i];
				//var view		=	Matrix.Invert( MathUtil.ComputeAimedBasis( lightDir ) );
				var view		=	Matrix.LookAtRH( lightDir, Vector3.Zero, Vector3.Up );
				var proj		=	Matrix.OrthoRH( 1024, 1024, -1024, 1024 );
				var bias		=	0;
				var slope		=	0;

				Log.Message("...{0}", lightDir);

				var context	=	new ShadowContext( view, proj, bias, slope, depthBuffer.Surface, colorBuffer.Surface ); 
				
				rs.SceneRenderer.RenderShadowMap( context, rs.RenderWorld, InstanceGroup.Static ); 

				//------------------------------------------
				//	compute occlusion for given shadow map :
				//------------------------------------------

				device.ResetStates();

				BAKE_PARAMS constData;
				constData.ShadowViewProjection		=	view * proj;
				constData.OcclusionGridTransform	=	OcclusionGridMatrix;
				constData.LightDirection			=	new Vector4( lightDir, 0 );
				constBuffer.SetData( constData );

				//	obscurance pass :
				device.ComputeShaderSamplers[0]		=	SamplerState.ShadowSamplerPoint;
				device.ComputeShaderResources[0]	=	colorBuffer;
				device.ComputeShaderResources[1]	=	occlusionGrid0;
				device.ComputeShaderConstants[0]	=	constBuffer;
				device.SetCSRWTexture( 0, occlusionGrid1 );

				device.PipelineState	=	factory[ (int)Flags.BAKE ];
				device.Dispatch( GridSize, BlockSize );

				Misc.Swap( ref occlusionGrid0, ref occlusionGrid1 );
			}

			Log.Message("Completed.");

			//------------------------------------------

			SafeDispose( ref depthBuffer );
			SafeDispose( ref colorBuffer );
		}




		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Occlusion grid stuff (Embree version):
		 * 
		-----------------------------------------------------------------------------------------*/

		static Random rand = new Random();

		Vector3[] sphereRandomPoints;
		Vector3[] hemisphereRandomPoints;
		Vector3[] cubeRandomPoints;


		List<Vector3> points = new List<Vector3>();


		/// <summary>
		/// 
		/// </summary>
		public void UpdateIrradianceMap ( IEnumerable<MeshInstance> instances, LightSet lightSet, DebugRender dr, int numSamples )
		{
			Log.Message("Building ambient occlusion map");

			using ( var rtc = new Rtc() ) {

				using ( var scene = new RtcScene( rtc, SceneFlags.Coherent|SceneFlags.Static, AlgorithmFlags.Intersect1 ) ) {

					points.Clear();

					var min		=	Vector3.One * (-GridStep/2.0f);
					var max		=	Vector3.One * ( GridStep/2.0f);

					sphereRandomPoints		= Enumerable.Range(0,numSamples).Select( i => Hammersley.SphereUniform(i,numSamples) ).ToArray();
					hemisphereRandomPoints	= Enumerable.Range(0,numSamples).Select( i => Hammersley.HemisphereUniform(i,numSamples) ).ToArray();
					cubeRandomPoints		= Enumerable.Range(0,numSamples).Select( i => rand.NextVector3( min, max ) ).ToArray();

					foreach ( var p in hemisphereRandomPoints ) {
						dr.DrawPoint( p, 0.1f, Color.Orange );
					}

					Log.Message("...generating scene");

					foreach ( var instance in instances ) {
						AddMeshInstance( scene, instance );
					}

					scene.Commit();

					Log.Message("...tracing");

					var data	= new Color[ Width*Height*Depth ];
					var indices = new Color[ Width*Height*Depth ];
					var weights = new Color[ Width*Height*Depth ];


					for ( int x=0; x<Width;  x++ ) {

						for ( int y=0; y<Height; y++ ) {

							for ( int z=0; z<Depth;  z++ ) {

								int index		=	ComputeAddress(x,y,z);

								var offset		=	new Vector3( GridStep/2.0f, GridStep/2.0f, GridStep/2.0f );
								var translation	=	new Vector3( -Width/2.0f, 0, -Depth/2.0f );
								var position	=	(new Vector3( x, y, z ) + translation) * GridStep;

								var localAO		=	1;//ComputeLocalOcclusion( scene, position, 3, numSamples );
								var globalAO	=	ComputeSkyOcclusion( scene, position, 128, numSamples );

								byte byteX		=	(byte)( 255 * (globalAO.X * 0.5+0.5) );
								byte byteY		=	(byte)( 255 * (globalAO.Y * 0.5+0.5) );
								byte byteZ		=	(byte)( 255 * (globalAO.Z * 0.5+0.5) );
								byte byteW		=	(byte)( 255 * localAO );

								data[index]		=	new Color( byteX, byteY, byteZ, byteW );

								/*if (x==0 || y==0 || z==0 || x==Width-1 || y==Height-1 || z==Depth-1 ) {
									data[index]	=	new Color( 127,255,127,0 );
								} */
							}
						}
					}

					occlusionGrid0.SetData( data );

					Log.Message("Done!");
				}
			}
		}



		int	ComputeAddress ( int x, int y, int z ) 
		{
			return x + y * Width + z * Height*Width;
		}



		float ComputeLocalOcclusion ( RtcScene scene, Vector3 point, float maxRange, int numSamples )
		{
			float factor = 0;

			for (int i=0; i<numSamples; i++) {
				
				var dir		=	sphereRandomPoints[i];
				var bias	=	cubeRandomPoints[i];

				var x	=	point.X + bias.X - dir.X;
				var y	=	point.Y + bias.Y - dir.Y;
				var z	=	point.Z + bias.Z - dir.Z;
				var dx	=	dir.X;
				var dy	=	dir.Y;
				var dz	=	dir.Z;

				var dist	=	scene.Intersect( x,y,z, dx,dy,dz, 0, maxRange );

				if (dist>=0) {
					var localFactor = (float)Math.Exp(-dist*2) / numSamples;
					factor = factor + (float)localFactor;
				}
			}

			return 1-MathUtil.Clamp( factor * 2, 0, 1 );
		}



		Vector3 ComputeSkyOcclusion ( RtcScene scene, Vector3 point, float maxRange, int numSamples )
		{
			var bentNormal	=	Vector3.Zero;
			var factor		=	0;
			var scale		=	1.0f / numSamples;

			for (int i=0; i<numSamples; i++) {
				
				var dir		=	hemisphereRandomPoints[i];
				var bias	=	Vector3.Zero;// cubeRandomPoints[i];

				var x	=	point.X + bias.X + dir.X * GridStep / 2.0f;
				var y	=	point.Y + bias.Y + dir.Y * GridStep / 2.0f;
				var z	=	point.Z + bias.Z + dir.Z * GridStep / 2.0f;
				var dx	=	dir.X;
				var dy	=	dir.Y;
				var dz	=	dir.Z;

				//var dist	=	scene.Intersect( x,y,z, dx,dy,dz, 0, maxRange );

				//if (dist<=0) {
				//	factor		+= 1;
				//	bentNormal	+= dir;
				//}
				
				var occluded	=	scene.Occluded( x,y,z, dx,dy,dz, 0, maxRange );

				if (!occluded) {
					factor		+= 1;
					bentNormal	+= dir;
				}
			}

			if (bentNormal.Length()>0) {
				bentNormal.Normalize();
				bentNormal = bentNormal * (float)Math.Sqrt( factor * scale );
			} else {
				bentNormal = Vector3.Zero;
			}

			return bentNormal;
		}




		void AddMeshInstance ( RtcScene scene, MeshInstance instance )
		{
			var mesh		=	instance.Mesh;

			if (mesh==null) {	
				return;
			}

			var indices     =   mesh.GetIndices();
			var vertices    =   mesh.Vertices
								.Select( v1 => Vector3.TransformCoordinate( v1.Position, instance.World ) )
								.Select( v2 => new Vector4( v2.X, v2.Y, v2.Z, 0 ) )
								.ToArray();

			var id		=	scene.NewTriangleMesh( GeometryFlags.Static, indices.Length/3, vertices.Length );

			var pVerts	=	scene.MapBuffer( id, BufferType.VertexBuffer );
			var pInds	=	scene.MapBuffer( id, BufferType.IndexBuffer );

			SharpDX.Utilities.Write( pVerts, vertices, 0, vertices.Length );
			SharpDX.Utilities.Write( pInds,  indices,  0, indices.Length );

			scene.UnmapBuffer( id, BufferType.VertexBuffer );
			scene.UnmapBuffer( id, BufferType.IndexBuffer );
		}
	}
}
