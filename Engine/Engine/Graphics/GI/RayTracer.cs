using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Core.Content;
using Fusion.Core.Configuration;
using Fusion.Drivers.Graphics;
using Fusion.Engine.Common;
using Fusion.Engine.Graphics.Ubershaders;
using Fusion.Core;
using Fusion.Engine.Graphics.Lights;
using Fusion.Core.Shell;
using Fusion.Engine.Graphics.Bvh;
using System.Diagnostics;

namespace Fusion.Engine.Graphics.GI
{
	[RequireShader( "raytracer", true )]
	public partial class RayTracer : RenderComponent
	{
		[ShaderDefine]	
		const int TileSize		=	8;

		static FXConstantBuffer<GpuData.CAMERA>		regCamera			=	new CRegister( 0, "Camera"		);
		static FXStructuredBuffer<Triangle>			regRtTriangles		=	new TRegister(20, "RtTriangles"		);
		static FXStructuredBuffer<BvhNode>			regRtBvhTree		=	new TRegister(21, "RtBvhTree"		);

		[ShaderIfDef("RAYTRACE")]   
		static FXRWTexture2D<Vector4>			regRaytraceImage	=	new URegister( 0, "RaytraceImage"	);

		enum Flags 
		{	
			RAYTRACE	=	0x001,
		}


		Ubershader			shader;
		StateFactory		factory;
		StructuredBuffer	sbPrimitives;
		StructuredBuffer	sbBvhTree;

		public RenderTarget2D	raytracedImage;


		class RTBuildBvh : ICommand
		{
			RayTracer rt;

			[CommandLineParser.Option]
			public bool All { get; set; }

			public RTBuildBvh( RayTracer rt )
			{
				this.rt	=	rt;
			}

			public object Execute()
			{
				rt.BuildAccelerationStructure(All);
				return null;
			}
		}



		/// <summary>
		/// 
		/// </summary>
		public RayTracer( RenderSystem rs ) : base( rs )
		{
		}



		public override void Initialize()
		{
			base.Initialize();

			raytracedImage	=	new RenderTarget2D( rs.Device, ColorFormat.Rg11B10, 800, 600,true );

			LoadContent();

			Game.Invoker.RegisterCommand("rtBuildBvh", ()=>new RTBuildBvh(this) );

			Game.Reloading += (s,e) => LoadContent();
		}


		public void LoadContent()
		{
			shader	=	Game.Content.Load<Ubershader>("raytracer");
			factory	=	shader.CreateFactory( typeof(Flags) );
		}



		protected override void Dispose( bool disposing )
		{
			if (disposing)
			{
				SafeDispose( ref sbBvhTree );
				SafeDispose( ref sbPrimitives );

				SafeDispose( ref raytracedImage );
			}

			base.Dispose( disposing );
		}


		/*-----------------------------------------------------------------------------------------
		 *	Scene preprocessing :
		-----------------------------------------------------------------------------------------*/

		/// <summary>
		/// 
		/// </summary>
		public void TestRayTracing()
		{
			using ( new PixEvent( "Ray Tracing" ) )
			{

				device.ComputeConstants[ regCamera			]	=	rs.RenderWorld.Camera.CameraData;
				device.ComputeResources[ regRtTriangles		]	=	sbPrimitives;
				device.ComputeResources[ regRtBvhTree		]	=	sbBvhTree;

				device.PipelineState    =   factory[(int)Flags.RAYTRACE];		
				
				device.SetComputeUnorderedAccess( regRaytraceImage, raytracedImage.Surface.UnorderedAccess );	

				int width  = raytracedImage.Width;
				int height = raytracedImage.Height;

				device.Dispatch( new Int2( width, height ), new Int2( TileSize, TileSize ) );
			}
		}


		/*-----------------------------------------------------------------------------------------
		 *	Scene preprocessing :
		-----------------------------------------------------------------------------------------*/

		public void BuildAccelerationStructure(bool all)
		{
			Log.Message("Build acceleration structure");
			var sw = new Stopwatch();
			sw.Start();

			var instances	=	all ? rs.RenderWorld.Instances.ToArray() : rs.RenderWorld.Instances.Where( inst => inst.Group==InstanceGroup.Static ).ToArray();
			var tris		=	new List<Triangle>();
			var totalTris	=	0;

			foreach ( var instance in instances )
			{
				totalTris = GetRenderInstanceTriangles( tris, instance );
			}

			var bvhTree	=	new BvhTree<Triangle>( tris, prim => prim.ComputeBBox(), prim => prim.ComputeCentroid() );
			var flatTree =	bvhTree.FlattenTree( (isLeaf,index,bbox) => new BvhNode( isLeaf, index, bbox ) );


			SafeDispose( ref sbBvhTree );
			SafeDispose( ref sbPrimitives );

			sbPrimitives	=	new StructuredBuffer( rs.Device, typeof(Triangle), bvhTree.Primitives.Length,	StructuredBufferFlags.None );
			sbBvhTree		=	new StructuredBuffer( rs.Device, typeof(BvhNode),  flatTree.Length,				StructuredBufferFlags.None );

			sbPrimitives.SetData( bvhTree.Primitives );
			sbBvhTree.SetData( flatTree );

			sw.Stop();
			Log.Message("Done: {0} ms", sw.ElapsedMilliseconds);
		}



		int GetRenderInstanceTriangles( List<Triangle> tris, RenderInstance instance )
		{
			if (instance.Mesh==null)
			{
				return 0;
			}

			var mesh		=	instance.Mesh;

			var indices		=	mesh.GetIndices();
			var positions	=	mesh.Vertices
								.Select( v1 => Vector3.TransformCoordinate( v1.Position, instance.World ) )
								.ToArray();

			var numTris		=	indices.Length / 3;

			for (int i=0; i<numTris; i++)
			{
				var p0	=	positions[ indices[ i*3+0 ] ];
				var p1	=	positions[ indices[ i*3+1 ] ];
				var p2	=	positions[ indices[ i*3+2 ] ];

				tris.Add( new Triangle( p0, p1, p2 ) );
			}

			return numTris;
		}

		/*-----------------------------------------------------------------------------------------
		 *	Ray-tracing structures :
		-----------------------------------------------------------------------------------------*/

		[StructLayout(LayoutKind.Sequential, Pack=4, Size=64)]
		public struct Triangle
		{
			public Triangle( Vector3 p0, Vector3 p1, Vector3 p2 )
			{
				Point0	=	new Vector4( p0, 1 );
				Point1	=	new Vector4( p1, 1 );
				Point2	=	new Vector4( p2, 1 );

				PlaneEq	=	new Vector4( new Plane( p0, p1, p2 ).ToArray() );
			}
			public Vector4 Point0;
			public Vector4 Point1; 
			public Vector4 Point2;
			public Vector4 PlaneEq;

			public BoundingBox ComputeBBox()
			{
				return BoundingBox.FromPoints( 
					new Vector3( Point0.X, Point0.Y, Point0.Z ),
					new Vector3( Point1.X, Point1.Y, Point1.Z ),
					new Vector3( Point2.X, Point2.Y, Point2.Z ) );
			}

			public Vector3 ComputeCentroid()
			{
				return 
					( new Vector3( Point0.X, Point0.Y, Point0.Z )
					+ new Vector3( Point1.X, Point1.Y, Point1.Z )
					+ new Vector3( Point2.X, Point2.Y, Point2.Z ) ) / 3.0f;
			}
		}


		public struct BvhNode
		{
			public BvhNode ( bool isLeaf, uint index, BoundingBox bbox )
			{
			#if false
				// expand bbox to solve f16-precision issues :
				Half3	bboxMin		=	( bbox.Minimum - Vector3.One * 0.05f );
				Half3	bboxMax		=	( bbox.Maximum + Vector3.One * 0.05f );

				uint	leadBit		=	isLeaf ? 0x80000000u : 0;
				uint	indexBits	=	index  & 0x7FFFFFFFu;

				PackedMinMaxIndex.X	=	(uint)(( bboxMin.X.RawValue << 16 ) | ( bboxMin.Y.RawValue ));
				PackedMinMaxIndex.Y	=	(uint)(( bboxMin.Z.RawValue << 16 ) | ( bboxMax.X.RawValue ));
				PackedMinMaxIndex.Z	=	(uint)(( bboxMax.Y.RawValue << 16 ) | ( bboxMax.Z.RawValue ));
				PackedMinMaxIndex.W	=	leadBit | indexBits;
			#else
				uint	leadBit		=	isLeaf ? 0x80000000u : 0;
				uint	indexBits	=	index  & 0x7FFFFFFFu;

				BBoxMin		=	bbox.Minimum;
				BBoxMax		=	bbox.Maximum;
				Index		=	leadBit | indexBits;
				Reserved	=	0;
			#endif
			}

			public Vector3	BBoxMin;
			public uint		Index;
			public Vector3	BBoxMax;
			public uint		Reserved;
			//[ minX ][ minY ]
			//[ minZ ][ maxX ]
			//[ maxY ][ maxZ ]
			//[ IsLeaf:Index ]
			//public UInt4	PackedMinMaxIndex;
		}


	}
}
