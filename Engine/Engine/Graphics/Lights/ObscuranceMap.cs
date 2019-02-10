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
		public ShaderResource IrradianceMap0 { get { return irradianceMap0; } }
		public ShaderResource IrradianceMap1 { get { return irradianceMap1; } }
		public ShaderResource IrradianceMap2 { get { return irradianceMap2; } }
		public ShaderResource IrradianceMap3 { get { return irradianceMap3; } }
		public ShaderResource IrradianceMap4 { get { return irradianceMap4; } }
		public ShaderResource IrradianceMap5 { get { return irradianceMap5; } }

		Texture3D			irradianceMap0;
		Texture3D			irradianceMap1;
		Texture3D			irradianceMap2;
		Texture3D			irradianceMap3;
		Texture3D			irradianceMap4;
		Texture3D			irradianceMap5;
		
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
			irradianceMap0	=	new Texture3D( rs.Device, ColorFormat.Rgba32F,	Width, Height, Depth );
			irradianceMap1	=	new Texture3D( rs.Device, ColorFormat.Rgba32F,	Width, Height, Depth );
			irradianceMap2	=	new Texture3D( rs.Device, ColorFormat.Rgba32F,	Width, Height, Depth );
			irradianceMap3	=	new Texture3D( rs.Device, ColorFormat.Rgba32F,	Width, Height, Depth );
			irradianceMap4	=	new Texture3D( rs.Device, ColorFormat.Rgba32F,	Width, Height, Depth );
			irradianceMap5	=	new Texture3D( rs.Device, ColorFormat.Rgba32F,	Width, Height, Depth );

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
				SafeDispose( ref occlusionGrid );

				SafeDispose( ref irradianceMap0 );
				SafeDispose( ref irradianceMap1 );
				SafeDispose( ref irradianceMap2 );
				SafeDispose( ref irradianceMap3 );
				SafeDispose( ref irradianceMap4 );
				SafeDispose( ref irradianceMap5 );
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


			if (vpls!=null) {
				foreach (var vpl in vpls) {

					//var m =  Matrix.RotationY(MathUtil.PiOverTwo) * MathUtil.ComputeAimedBasis( vpl.Normal ) * Matrix.Translation( vpl.Position );

					rs.RenderWorld.Debug.DrawPoint( vpl.Position + 0.5f  * vpl.Normal, 0.7f, Color.Yellow, 1 );
					//rs.RenderWorld.Debug.DrawLine( vpl.Position, vpl.Position + vpl.Normal, Color.Yellow );
					//rs.RenderWorld.Debug.DrawRing( m, 6, Color.Yellow, 12 );
				}
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

			var hemispherePoints	= Enumerable.Range(0,numSamples)
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

				var lightDir	=	hemispherePoints[i];
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


		Vector3[] CreateRegularPoints5x5x6 ()
		{
			var points = new List<Vector3>();

			for (int u=-4; u<=4; u++) {
				for (int v=-4; v<=4; v++) {

					points.Add( new Vector3( u, v,  4.5f ) );
					points.Add( new Vector3( u, v, -4.5f ) );

					points.Add( new Vector3( u,  4.5f, v ) );
					points.Add( new Vector3( u, -4.5f, v ) );

					points.Add( new Vector3(  4.5f, u, v ) );
					points.Add( new Vector3( -4.5f, u, v ) );

				}
			}

			return points.Select( vec => vec.Normalized() ).ToArray();
		}


		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Occlusion grid stuff (Embree version):
		 * 
		-----------------------------------------------------------------------------------------*/

		Vector3		originPoint = new Vector3(3,17,5);
		Vector3[] hitPoints;

		static Random rand = new Random();

		List<Vector3> points = new List<Vector3>();

		Vector3 skyAmbient;

		class VPL {
			public VPL(Vector3 p, Vector3 n) { Position = p; Normal = n; }
			public Vector3 Position;
			public Vector3 Normal;
			public bool Reject = false;
			public int Rank;

			public RtcRay CreateRay( Vector3 dir, float bias, float length )
			{
				var ray = new RtcRay();
				EmbreeExtensions.UpdateRay( ref ray, Position + Normal * bias, dir, 0, length );
				return ray;
			}
		}


		List<VPL> vpls;


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
			var dirLightColor	=	lightSet.DirectLight.Intensity.ToVector3();

			var randomVectors	=	Enumerable
									.Range(0,91)
									.Select( i => Hammersley.SphereUniform(i,91) )
									.ToArray();

			var spherePoints	= Enumerable
								.Range(0,numSamples)
								.Select( i => Hammersley.SphereUniform(i,numSamples) )
								.ToArray();//*/

			//var spherePoints	=	CreateRegularPoints5x5x6();
			vpls = new List<VPL>();


			using ( var rtc = new Rtc() ) {

				using ( var scene = new RtcScene( rtc, SceneFlags.Coherent|SceneFlags.Static, AlgorithmFlags.Intersect1 ) ) {

					Log.Message("...generating scene");

					foreach ( var instance in instances ) {
						if (instance.Group==InstanceGroup.Static) {
							AddMeshInstance( scene, instance );
							RasterizeInstance( scene, instance );
						}
					}

					scene.Commit();

					MergeVPLs();

					Log.Message("{0} VPLs placed", vpls.Count);

					vpls = vpls
						.Where( vpl => !scene.Occluded( vpl.CreateRay( dirToLight, 0.125f, 512 ) ) )
						.ToList();

					Log.Message("{0} VPLs are lit", vpls.Count);

					var shadowMatrix = Matrix.LookAtRH( dirToLight * 256 + Vector3.Up*32, Vector3.Up*32, Vector3.Up );
					shadowMatrix.Invert();

					var ray = new RtcRay();

					#if false
					for (int x=-128; x<=128; x+=4) {
						for (int y=-128; y<=128; y+=4) {

							var pos = Vector3.TransformCoordinate( new Vector3(x,y,0), shadowMatrix );
							//vpls.Add( new VPL(pos, shadowMatrix.Forward) );

							EmbreeExtensions.UpdateRay( ref ray, pos, shadowMatrix.Forward, 0, 1024 );

							if (scene.Intersect( ref ray )) {
								var n   = EmbreeExtensions.Convert( ray.HitNormal ).Normalized();
								var d   = EmbreeExtensions.Convert( ray.Direction );
								var p   = EmbreeExtensions.Convert( ray.Origin );
								var f   = ray.TFar;
								var dot	= Math.Max( 0, Vector3.Dot(n,-d) );
								vpls.Add( new VPL( p + d * f, -n ) );
							}

						}
					}
					#endif
					
					Log.Message("...tracing");

					var sw = new Stopwatch();
					sw.Start();

					var data0	=	new Color4[ Width*Height*Depth ];
					var data1	=	new Color4[ Width*Height*Depth ];
					var data2	=	new Color4[ Width*Height*Depth ];
					var data3	=	new Color4[ Width*Height*Depth ];
					var data4	=	new Color4[ Width*Height*Depth ];
					var data5	=	new Color4[ Width*Height*Depth ];

					for ( int x=0; x<Width;  x++ ) {

						Log.Message("...{0}/{1}", x, Width );

						for ( int y=0; y<Height; y++ ) {

							for ( int z=0; z<Depth;  z++ ) {

								int index		=	ComputeAddress(x,y,z);

								var randVector	=	randomVectors[ rand.Next(0,randomVectors.Length) ];

								var translation	=	new Vector3( -Width/2.0f, 0, -Depth/2.0f );
								var halfOffset	=	new Vector3( GridStep/2.0f, GridStep/2.0f, GridStep/2.0f );
								var position	=	(new Vector3( x, y, z ) + translation) * GridStep + halfOffset;

								var irradiance	=	GatherVPLs( scene, lightSet, position, 128 );
								//var irradiance	=	ComputeIndirectLight( scene, lightSet, position, randVector, spherePoints, 128 );
									//irradiance	+=	ComputeDirectLight( scene, lightSet, position, 512, numSamples );

								data0[index]		=	new Color4( irradiance[0], 0 );
								data1[index]		=	new Color4( irradiance[1], 0 );
								data2[index]		=	new Color4( irradiance[2], 0 );
								data3[index]		=	new Color4( irradiance[3], 0 );
								data4[index]		=	new Color4( irradiance[4], 0 );
								data5[index]		=	new Color4( irradiance[5], 0 );
								//data[index]		=	rand.NextColor4();
								//data2[index]	=	ComputeSkyOcclusion( scene, position, 512, numSamples );

								if (x==0 || y==0 || z==0) {
									data0[index]  = Color.Red.ToColor4();
								}
								if (x==Width-1 || y==Height-1 || z==Depth-1) {
									data0[index]  = Color.Red.ToColor4();
								}
							}
						}
					}

					irradianceMap0.SetData( data0 );
					irradianceMap1.SetData( data1 );
					irradianceMap2.SetData( data2 );
					irradianceMap3.SetData( data3 );
					irradianceMap4.SetData( data4 );
					irradianceMap5.SetData( data5 );

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


		Vector3[] GatherVPLs ( RtcScene scene, LightSet lightSet, Vector3 point, float maxRange )
		{
			var irradiance		=	new[] { Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero };

			var dirLightDir		=	-lightSet.DirectLight.Direction.Normalized();
			var dirLightColor	=	lightSet.DirectLight.Intensity.ToVector3();

			var ray				=	new RtcRay();

			for (int i=0; i<vpls.Count; i++) {

				var vpl		=	vpls[i];
				var	dir		=	vpl.Position - point;
				var len		=	dir.Length();
				var dirn	=	dir / len;
				var	origin	=	vpl.Position + vpl.Normal*0.125f;
				var dot		=	-Vector3.Dot(dirn, vpl.Normal);

				dot	=	Math.Max( 0, dot*1.1f - 0.1f );

				if (len==0) {
					continue;
				}

				if (len>64) {
					continue;
				}

				if (dot<=0) {
					continue;
				}


				EmbreeExtensions.UpdateRay( ref ray, origin, -dirn, 0, len*0.99f );

				float rcpLen2 = 1 / (len*len+4);

				if (!scene.Occluded( ref ray ) ) {

					var dotPX	=	Math.Max( 0,  Vector3.Dot( dirn, Vector3.UnitX ) );
					var dotNX	=	Math.Max( 0, -Vector3.Dot( dirn, Vector3.UnitX ) );

					var dotPY	=	Math.Max( 0,  Vector3.Dot( dirn, Vector3.UnitY ) );
					var dotNY	=	Math.Max( 0, -Vector3.Dot( dirn, Vector3.UnitZ ) );

					var dotPZ	=	Math.Max( 0,  Vector3.Dot( dirn, Vector3.UnitZ ) );
					var dotNZ	=	Math.Max( 0, -Vector3.Dot( dirn, Vector3.UnitZ ) );

					irradiance[0] += dotPX * dot * 4 * rcpLen2 / 4 / 3.14f * dirLightColor;
					irradiance[1] += dotNX * dot * 4 * rcpLen2 / 4 / 3.14f * dirLightColor;
					irradiance[2] += dotPY * dot * 4 * rcpLen2 / 4 / 3.14f * dirLightColor;
					irradiance[3] += dotNY * dot * 4 * rcpLen2 / 4 / 3.14f * dirLightColor;
					irradiance[4] += dotPZ * dot * 4 * rcpLen2 / 4 / 3.14f * dirLightColor;
					irradiance[5] += dotNZ * dot * 4 * rcpLen2 / 4 / 3.14f * dirLightColor;
				}
			}

			return irradiance;
		}



		Vector3[] ComputeIndirectLight ( RtcScene scene, LightSet lightSet, Vector3 point, Vector3 randVector, Vector3[] randomPoints, float maxRange )
		{
			var irradiance		=	new[] { Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero };

			var dirLightDir		=	-lightSet.DirectLight.Direction.Normalized();
			var dirLightColor	=	lightSet.DirectLight.Intensity.ToVector3();
			var totalWeight		=	0f;

			for (int i=0; i<randomPoints.Length; i++) {

				RtcRay	ray		=	new RtcRay();
				var		dir		=	Vector3.Reflect( randomPoints[i], randVector );
				//var		dir		=	randomPoints[i];
				var		origin	=	point;

				EmbreeExtensions.UpdateRay( ref ray, origin, dir, 0f, maxRange );

				if (scene.Intersect( ref ray )) {

					var o			=	EmbreeExtensions.Convert( ray.Origin );
					var d			=	EmbreeExtensions.Convert( ray.Direction );
					var position	=	o + d * (ray.TFar);
					var normal		=	(-1) * EmbreeExtensions.Convert( ray.HitNormal ).Normalized();
					var nDotP		=	Vector3.Dot( normal, d );
					var nDotL		=	Math.Max( 0, Vector3.Dot( normal.Normalized(), dirLightDir ) );

					var weight		=	1;//(float)Math.Exp( -ray.TFar/32.0f );
					totalWeight		+=	weight;

					if (nDotL>0 && nDotP<0) {

						EmbreeExtensions.UpdateRay( ref ray, position + normal * 0.125f, dirLightDir, 0.125f, maxRange );

						if (!scene.Occluded( ref ray )) {
						
							irradiance[0]	= irradiance[0] + dirLightColor * nDotL * 0.5f * weight;
						}
					}
				} else {
					/*if (dir.Y>0) {
						irradiance += skyAmbient;// * dir.Y;
					} //*/
				}				
			}

			return irradiance;
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



		Color ComputeSkyOcclusion ( RtcScene scene, Vector3 point, float maxRange, Vector3[] randomDirs )
		{
			var bentNormal	=	Vector3.Zero;
			var factor		=	0;
			var scale		=	1.0f / randomDirs.Length;

			for (int i=0; i<randomDirs.Length; i++) {
				
				var dir		=	randomDirs[i];
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



#if false
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
#endif

		bool CanConsumeVPL( VPL a, VPL b, float maxLinearDistance, float maxProjectedDist )
		{
			var dot		= Vector3.Dot( a.Normal, b.Normal );
			var dist	= a.Position - b.Position;

			if (b.Reject) {
				return false;
			}

			if (dot<(float)Math.Cos(30)) {
				return false;
			}
			
			if ( Math.Abs( Vector3.Dot( a.Normal, dist ) ) > maxProjectedDist ) {
				return false;
			}
			
			if ( Vector3.Distance( a.Position, b.Position ) > maxLinearDistance ) {
				return false;
			}

			return true;
		}


		void RasterizeInstance ( RtcScene scene, MeshInstance instance )
		{
			var mesh		=	instance.Mesh;

			if (mesh==null) {	
				return;
			}

			var indices     =   mesh.GetIndices();
			var vertices    =   mesh.Vertices
								.Select( v1 => Vector3.TransformCoordinate( v1.Position, instance.World ) )
								.ToArray();

			var bias = new Vector3(4,4,4);

			for (int i=0; i<indices.Length/3; i++) {
				var p0 = vertices[indices[i*3+0]] + bias;
				var p1 = vertices[indices[i*3+1]] + bias;
				var p2 = vertices[indices[i*3+2]] + bias;
				var n  = Vector3.Cross( p1 - p0, p2 - p0 ).Normalized();
				Voxelizer.RasterizeTriangle( p0, p1, p2, 2, (p) => {
					var vpl = new VPL(p-bias, n);
					vpls.Add( vpl );
				} );
			}//*/

		}


		void MergeVPLs ()
		{
			Log.Message("Merging VPLs:");

			//------------------------------

			Log.Message("...shuffle");

			vpls = vpls.Shuffle( new Random() ).ToList();

			//------------------------------

			Log.Message("...building KdTree");
			KdTree3<VPL> kdTree = new KdTree3<VPL>();

			foreach ( var vpl in vpls ) {
				kdTree.Add( vpl.Position, vpl );
			}

			//------------------------------

			Log.Message("...ranking VPLs");

			foreach ( var vpl in vpls ) {
				vpl.Rank = kdTree.NearestRadius( vpl.Position, 4.1f, (other) => CanConsumeVPL(vpl, other, 4, 1) ).Count();
			}

			vpls = vpls.OrderByDescending( v => v.Rank ).ToList();

			//------------------------------

			Log.Message("...merging VPLs");

			foreach ( var vpl in vpls ) {

				if (vpl.Reject) {
					continue;
				}
			
				var nearest = kdTree.NearestRadius( vpl.Position, 4, (other) => CanConsumeVPL(vpl, other, 4, 1) ); 	

				foreach ( var other in nearest ) {
					if (other!=vpl) {
						other.Reject = true;
					}
				}
			}

			vpls = vpls.Where( vpl => !vpl.Reject ).ToList();

			Log.Message("Completed.");
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
