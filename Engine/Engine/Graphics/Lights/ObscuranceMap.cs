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


		public ShaderResource OcclusionGrid { get { return occlusionGrid; } }
		public ShaderResource IrradianceMap { get { return irradianceMap; } }

		Texture3D			irradianceMap;
		Texture3D			occlusionGrid;
		Texture3DCompute	occlusionGrid0;
		Texture3DCompute	occlusionGrid1;
		ConstantBuffer		constBuffer;
		StateFactory		factory;
		Ubershader			shader;


		enum Flags {
			BAKE,
			COPY,
		}


		const int	Width		=	128/2;
		const int	Height		=	 64/2;
		const int	Depth		=	128/2;
		const float GridStep	=	4.0f;

		readonly Int3 GridSize	=	new Int3( Width, Height, Depth );
		readonly Int3 BlockSize	=	new Int3( BlockSizeX, BlockSizeY, BlockSizeZ );	


		public Matrix OcclusionGridMatrix {
			get {
				return	Matrix.Identity
					*	Matrix.Translation( Width/2.0f*GridStep, 0, Depth/2.0f*GridStep )
					//*	Matrix.Translation( 0.5f*GridStep, 0.5f*GridStep, 0.5f*GridStep )
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

			occlusionGrid	=	new Texture3D( rs.Device, ColorFormat.Rgba8,	Width, Height, Depth );
			irradianceMap	=	new Texture3D( rs.Device, ColorFormat.Rgba32F,	Width, Height, Depth );

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
				SafeDispose( ref irradianceMap );
				SafeDispose( ref occlusionGrid );
			}

			base.Dispose( disposing );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		public void Update ( GameTime gameTime )
		{
			if (hitPoints!=null) {

				rs.RenderWorld.Debug.DrawPoint( originPoint, 1f, Color.Red, 3 );

				for (int i=0; i<hitPoints.Length; i++) {
					//rs.RenderWorld.Debug.DrawLine( originPoint, hitPoints[i], Color.Red );
				}


				for (int i=0; i<hitPoints.Length; i++) {
					rs.RenderWorld.Debug.DrawPoint( hitPoints[i], 0.5f, Color.Orange, 2 );
				}
			}


			rs.RenderWorld.Debug.DrawPoint( originPoint, 0.7f, Color.Red, 3 );

			foreach ( var p in points ) {
				rs.RenderWorld.Debug.DrawPoint( p, 0.3f, Color.Red, 1 );
			}

			/*for ( int x=-Width/2; x<=Width/2; x+=4 ) {
				for ( int y=-Height/2; y<=Height/2; y+=4 ) {
					for ( int z=-Depth/2; z<=Depth/2; z+=4 ) {

						rs.RenderWorld.Debug.DrawPoint( new Vector3( x*2, y*2, z*2), 1f, Color.Red, 3 );

					}
				}
			} */
		}


		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Occlusion grid stuff (GPU version):
		 * 
		-----------------------------------------------------------------------------------------*/

		public void UpdateObscuranceMapGPU ( IEnumerable<MeshInstance> instances, LightSet lightSet, DebugRender dr, int numSamples )
		{
			Log.Message("--------------------------------");
			Log.Message("Building ambient obscurance map (GPU)");

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

			var sw = new Stopwatch();
			sw.Start();

			for (int i=0; i<numSamples; i++) {

				device.Clear( colorBuffer.Surface, Color4.Zero );
				device.Clear( depthBuffer.Surface, 1, 0 ); 

				var lightDir	=	hemisphereRandomPoints[i];
				//var view		=	Matrix.Invert( MathUtil.ComputeAimedBasis( lightDir ) );
				var view		=	Matrix.LookAtRH( lightDir, Vector3.Zero, Vector3.Up );
				var proj		=	Matrix.OrthoRH( 512, 512, -512, 512 );
				var bias		=	0;
				var slope		=	0;

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

			sw.Stop();
			Log.Message("Completed: {0} ms", sw.ElapsedMilliseconds);
			Log.Message("--------------------------------");

			//------------------------------------------

			SafeDispose( ref depthBuffer );
			SafeDispose( ref colorBuffer );
		}




		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Occlusion grid stuff (Embree version):
		 * 
		-----------------------------------------------------------------------------------------*/

		Vector3		originPoint = new Vector3(3,17,5);
		Vector3[] hitPoints;

		static Random rand = new Random();

		Vector3[] sphereRandomPoints;
		Vector3[] hemisphereRandomPoints;
		Vector3[] cubeRandomPoints;


		List<Vector3> points = new List<Vector3>();


		int debugX = Width / 2 - 30/4 - 1;
		int debugY = 30/4;
		int debugZ = Depth / 2 + 30/4 - 1;

		Vector3 skyAmbient;

		/// <summary>
		/// 
		/// </summary>
		public void UpdateIrradianceMap ( IEnumerable<MeshInstance> instances, LightSet lightSet, DebugRender dr, int numSamples )
		{
			Log.Message("--------------------------------");
			Log.Message("Building irradiance map");

			skyAmbient = rs.RenderWorld.SkySettings.AmbientLevel.ToVector3();
			Log.Message( "{0} {1} {2}", skyAmbient.X, skyAmbient.Y, skyAmbient.Z );

			//int numSamples	=	128;

			var dirToLight		=	-lightSet.DirectLight.Direction;

			var randomVectors	=	Enumerable
									.Range(0,91)
									.Select( i => Hammersley.SphereUniform(i,91) )
									.ToArray();

			sphereRandomPoints	= Enumerable
								.Range(0,numSamples)
								.Select( i => Hammersley.SphereUniform(i,numSamples) )
								.ToArray();

			hemisphereRandomPoints	= Enumerable
								.Range(0,numSamples)
								.Select( i => Hammersley.HemisphereCosine(i,numSamples) )
								.ToArray();


			using ( var rtc = new Rtc() ) {

				using ( var scene = new RtcScene( rtc, SceneFlags.Incoherent|SceneFlags.Static, AlgorithmFlags.Intersect1 ) ) {

					Log.Message("...generating scene");

					foreach ( var instance in instances ) {
						if (instance.Group==InstanceGroup.Static) {
							AddMeshInstance( scene, instance );
						}
					}

					scene.Commit();

					Log.Message("...tracing");

					var sw = new Stopwatch();
					sw.Start();

					var data	=	new Color4[ Width*Height*Depth ];
					var data2	=	new Color [ Width*Height*Depth ];

					for ( int x=0; x<Width;  x++ ) {

						Log.Message("...{0}/{1}", x, Width );

						for ( int y=0; y<Height; y++ ) {

							for ( int z=0; z<Depth;  z++ ) {

								int index		=	ComputeAddress(x,y,z);

								var randVector	=	randomVectors[ rand.Next(0,91) ];

								var translation	=	new Vector3( -Width/2.0f, 0, -Depth/2.0f );
								var halfOffset	=	new Vector3( GridStep/2.0f, GridStep/2.0f, GridStep/2.0f );
								var position	=	(new Vector3( x, y, z ) + translation) * GridStep + halfOffset;

								var debugVoxel	=	(x==debugX && y==debugY && z==debugZ);


								var irradiance	=	ComputeIndirectLight( scene, lightSet, position, randVector, 512, numSamples, debugVoxel );
									//irradiance	+=	ComputeDirectLight( scene, lightSet, position, 512, numSamples );

								data[index]		=	new Color4( irradiance, 0 );
								//data[index]		=	rand.NextColor4();
								//data2[index]	=	ComputeSkyOcclusion( scene, position, 512, numSamples );

								if (x==0 || y==0 || z==0) {
									data[index] = Color.Red.ToColor4();
									data2[index] = new Color(127,127,127,127);
								}
								if (x==Width-1 || y==Height-1 || z==Depth-1) {
									data[index] = Color.Red.ToColor4();
									data2[index] = new Color(127,127,127,127);
								}
								if (debugVoxel) {
									data[index] = Color.Orange.ToColor4() * 4;
								}

							}
						}
					}

					occlusionGrid.SetData( data2 );
					irradianceMap.SetData( data );

					sw.Stop();

					Log.Message("Compeleted: {0} ms", sw.Elapsed.TotalMilliseconds );
					Log.Message("--------------------------------");
				}
			}
		}



		int	ComputeAddress ( int x, int y, int z ) 
		{
			return x + y * Width + z * Height*Width;
		}



		Vector3 ComputeIndirectLight ( RtcScene scene, LightSet lightSet, Vector3 point, Vector3 randVector, float maxRange, int numSamples, bool debugVoxel )
		{
			var scale			=	1.0f / numSamples;
			var irradiance		=	Vector3.Zero;
			var dirLightDir		=	-lightSet.DirectLight.Direction.Normalized();
			var dirLightColor	=	lightSet.DirectLight.Intensity.ToVector3();

			for (int i=0; i<numSamples; i++) {

				RtcRay	ray		=	new RtcRay();
				//var		dir		=	Vector3.Reflect( sphereRandomPoints[i], randVector );
				var		dir		=	sphereRandomPoints[i];
				var		origin	=	point;

				EmbreeExtensions.UpdateRay( ref ray, origin, dir, 0f, maxRange );

				if (debugVoxel) {
					points.Add( origin );
				}

				if (scene.Intersect( ref ray )) {

					var o			=	EmbreeExtensions.Convert( ray.Origin );
					var d			=	EmbreeExtensions.Convert( ray.Direction );
					var position	=	o + d * (ray.TFar);
					var normal		=	(-1) * EmbreeExtensions.Convert( ray.HitNormal ).Normalized();
					var nDotP		=	Vector3.Dot( normal, d );
					var nDotL		=	Math.Max( 0, Vector3.Dot( normal.Normalized(), dirLightDir ) );

					if (nDotL>0 && nDotP<0) {

						if (debugVoxel) {
							points.Add( position );
							points.Add( position + normal * 0.5f );
						}

						EmbreeExtensions.UpdateRay( ref ray, position + normal * 0.125f, dirLightDir, 0.125f, maxRange );

						if (!scene.Occluded( ref ray )) {
						
							irradiance	= irradiance + dirLightColor * nDotL * 0.5f;
						}
					}
				} else {
					if (dir.Y>0) {
						irradiance += skyAmbient;// * dir.Y;
					}
				}				
			}

			return irradiance * scale;
		}



		Vector3 ComputeDirectLight ( RtcScene scene, LightSet lightSet, Vector3 point, float maxRange, int numSamples )
		{
			var scale			=	1.0f / numSamples;
			var irradiance		=	Vector3.Zero;
			var dirLightDir		=	-lightSet.DirectLight.Direction.Normalized();
			var dirLightColor	=	lightSet.DirectLight.Intensity.ToVector3();

			RtcRay	ray		=	new RtcRay();
			var		dir		=	dirLightDir;
			var		origin	=	point;

			EmbreeExtensions.UpdateRay( ref ray, point, dirLightDir, 0, 9999);

			if (!scene.Occluded(ref ray)) {
				return dirLightColor;
			} else {
				return Vector3.Zero;
			}

			//return irradiance * scale;
		}



		Color ComputeSkyOcclusion ( RtcScene scene, Vector3 point, float maxRange, int numSamples )
		{
			var bentNormal	=	Vector3.Zero;
			var factor		=	0;
			var scale		=	1.0f / numSamples;

			for (int i=0; i<numSamples; i++) {
				
				var dir		=	hemisphereRandomPoints[i];
				var ray		=	new RtcRay();
				var dirBias	=	GridStep * (float)Math.Sqrt(3f);

				EmbreeExtensions.UpdateRay( ref ray, point + dir*dirBias, dir, 0, maxRange );

				var occluded	=	scene.Occluded( ref ray );

				if (!occluded) {
					factor		+= 1;
					bentNormal	+= dir;
				}
			}

			if (bentNormal.Length()>0) {
				bentNormal = bentNormal * scale;
			} else {
				bentNormal = Vector3.Zero;
			}


			byte byteX	=	(byte)( 255 * MathUtil.Clamp(bentNormal.X * 0.5f+0.5f, 0, 1) );
			byte byteY	=	(byte)( 255 * MathUtil.Clamp(bentNormal.Y * 0.5f+0.5f, 0, 1) );
			byte byteZ	=	(byte)( 255 * MathUtil.Clamp(bentNormal.Z * 0.5f+0.5f, 0, 1) );
			byte byteW	=	(byte)( 255 * 0 );
			
			return new Color( byteX, byteY, byteZ, byteW );
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
