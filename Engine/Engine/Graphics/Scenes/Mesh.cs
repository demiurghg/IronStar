using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using SharpDX;
using Fusion.Drivers.Graphics;
using System.Reflection;
using System.ComponentModel.Design;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Core.Collection;
using System.Diagnostics;
using System.Threading;

namespace Fusion.Engine.Graphics.Scenes {

	public sealed partial class Mesh : DisposableBase, IEquatable<Mesh> 
	{
		private static int meshInstanceCounter	=	0;

		public readonly int InstanceRef;

		public List<MeshVertex>		Vertices			{ get; set; }	
		public List<MeshTriangle>	Triangles			{ get; private set; }	
		public List<MeshSubset>		Subsets				{ get; private set; }
		public List<MeshSurfel>		Surfels				{ get; private set; }
		public List<string>			UVSetNames			{ get; private set; }
		public int					TriangleCount		{ get { return Triangles.Count; } }
		public int					VertexCount			{ get { return Vertices.Count; } }
		public int					IndexCount			{ get { return TriangleCount * 3; } }

		public BoundingBox			BoundingBox			{ get { return boundingBox; } }
		BoundingBox boundingBox = new BoundingBox(0,0,0);

		internal VertexBuffer		VertexBuffer		{ get { return vertexBuffer; } }
		internal IndexBuffer		IndexBuffer			{ get { return indexBuffer; } }

		public bool					IsSkinned			{ get; private set; }

		public int[]				AdjacentVertices	{ get; private set; } = new int[0];
		public int[]				AdjacentTriangles	{ get; private set; } = new int[0];

		VertexBuffer vertexBuffer;
		IndexBuffer	 indexBuffer;


		/// <summary>
		/// Mesh constructor
		/// </summary>
		public Mesh ()
		{
			//	keeps more or less unique index used to group render instances
			//	it works, bacause runtime meshes are created usually once.
			InstanceRef		=	Interlocked.Increment( ref meshInstanceCounter );

			Trace.Assert( InstanceRef < 16000000 );

			Vertices		=	new List<MeshVertex>();
			Triangles		=	new List<MeshTriangle>();
			Subsets			=	new List<MeshSubset>();
			Surfels			=	new List<MeshSurfel>();
			UVSetNames		=	new List<string>();
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose ( bool disposing )
		{
			if (disposing) 
			{
				SafeDispose( ref vertexBuffer );
				SafeDispose( ref indexBuffer );
			}
			base.Dispose( disposing );
		}



		public override string ToString ()
		{
			return string.Format("Mesh: [{0} vertices, {1} indices, {2} subsets]", VertexCount, IndexCount, Subsets.Count );
		}


		/// <summary>
		/// Gets indices ready for hardware use
		/// </summary>
		/// <returns></returns>
		public int[] GetIndices ( int baseIndex = 0 )
		{
			int[] array = new int[ Triangles.Count * 3];

			for (int i=0; i<Triangles.Count; i++) {
				array[i*3+0] = Triangles[i].Index0 + baseIndex;
				array[i*3+1] = Triangles[i].Index1 + baseIndex;
				array[i*3+2] = Triangles[i].Index2 + baseIndex;
			}

			return array;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="tolerance"></param>
		public void Prepare ( Scene scene, float tolerance )
		{
			MergeVertices( tolerance );
			DefragmentSubsets(scene, true);
			ComputeTangentFrame();
			ComputeBoundingBox();//*/
		}



		/// <summary>
		/// 
		/// </summary>
		internal void CreateVertexAndIndexBuffers ( GraphicsDevice device )
		{
			indexBuffer		=	IndexBuffer.Create( device, GetIndices() );

			bool skinned	=	false;

			foreach ( var v in Vertices )
			 {
				if (v.SkinIndices!=Int4.Zero) 
				{
					skinned = true;
					break;
				}
			}

			IsSkinned	=	skinned;


			if (skinned) 
			{
				vertexBuffer = VertexBuffer.Create( device, Vertices.Select( v => VertexColorTextureTBNSkinned.Convert(v) ).ToArray() );
			} 
			else 
			{
				vertexBuffer = VertexBuffer.Create( device, Vertices.Select( v => VertexColorTextureTBNRigid.Convert(v) ).ToArray() );
			}
		}



		/// <summary>
		/// This methods check equality of two diferrent mesh by 
		/// the following criterias performing early quit if any of the fails:
		///		- Tag object
		///		- Vertex count
		///		- Triangle count
		///		- Subsets count
		///		- Materials count.
		///		- Vertex buffer
		///		- Index buffer
		///		- Vertices list
		/// 	- Triangles list
		/// 	- Subsets list
		/// 	- Materials list
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals ( Mesh other )
		{
			if (other==null) return false;

			if (Object.ReferenceEquals( this, other )) {
				return true;
			}

			if ( this.VertexCount		!= other.VertexCount	) return false;
			if ( this.TriangleCount		!= other.TriangleCount	) return false;
			if ( this.IndexCount		!= other.IndexCount		) return false;
			if ( this.Subsets.Count		!= other.Subsets.Count	) return false;
			
			if ( !this.Vertices .SequenceEqual( other.Vertices  ) ) return false;
			if ( !this.Triangles.SequenceEqual( other.Triangles ) ) return false;
			if ( !this.Subsets  .SequenceEqual( other.Subsets   ) ) return false;

			return true;
		}


		public override bool Equals ( object obj )
		{
			if (obj==null) return false;
			if (obj as Mesh==null) return false;
			return Equals((Mesh)obj);
		}



		public override int GetHashCode ()
		{
			unchecked
			{
				return ( Vertices.Count * 37 + Triangles.Count ) * 37 + Subsets.Count;
			}
			//return Misc.Hash( Vertices, Triangles, Subsets );
		}



		public static bool operator == (Mesh obj1, Mesh obj2)
		{
			if ((object)obj1 == null || ((object)obj2) == null)
				return Object.Equals(obj1, obj2);

			return obj1.Equals(obj2);
		}



		public static bool operator != (Mesh obj1, Mesh obj2)
		{
			if (obj1 == null || obj2 == null)
				return ! Object.Equals(obj1, obj2);

			return ! (obj1.Equals(obj2));
		}

		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Computational and optimization stuff :
		 * 
		-----------------------------------------------------------------------------------------*/

		/// <summary>
		/// Defragmentates mesh subsets with same materials
		/// </summary>
		public void DefragmentSubsets ( Scene scene, bool takeFromTriangleMtrlIndices )
		{
			//	if there are not shading groups, 
			//	take them from per triangle material indices
			if (!Subsets.Any() || takeFromTriangleMtrlIndices) 
			{
				for ( int i=0; i<Triangles.Count; i++ ) 
				{
					MeshSubset sg = new MeshSubset();
					sg.MaterialIndex	=	Triangles[i].MaterialIndex;
					sg.StartPrimitive	=	i;
					sg.PrimitiveCount	=	1;

					Subsets.Add( sg );
					//Console.Write( "*{0}", Triangles[i].MaterialIndex );
				}
			}

			if ( Subsets.Count==1 ) 
			{
				return;
			}

			List<List<MeshTriangle>>	perMtrlTris = new List<List<MeshTriangle>>();

			foreach ( var mtrl in scene.Materials ) 
			{
				perMtrlTris.Add( new List<MeshTriangle>() );
			}

			foreach ( var sg in Subsets ) 
			{

				for ( int i = sg.StartPrimitive; i < sg.StartPrimitive + sg.PrimitiveCount; i++ ) 
				{
					perMtrlTris[ sg.MaterialIndex ].Add( Triangles[i] );
				}
			}

			Subsets.Clear();
			Triangles.Clear();

			for ( int i=0; i<perMtrlTris.Count; i++ ) 
			{
				var sg = new MeshSubset();
				sg.MaterialIndex	=	i;
				sg.StartPrimitive	=	Triangles.Count;
				sg.PrimitiveCount	=	perMtrlTris[i].Count;

				if (sg.PrimitiveCount==0) 
				{
					continue;
				}

				Triangles.AddRange( perMtrlTris[i] );
				Subsets.Add( sg );
			}
			
		}


		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Adjacency :
		 * 
		-----------------------------------------------------------------------------------------*/

		public void BuildAdjacency ()
		{
			var count = Triangles.Count;

			AdjacentTriangles	=	Enumerable.Repeat( -1, count * 3 ).ToArray();
			AdjacentVertices	=	Enumerable.Repeat( -1, count * 3 ).ToArray();

			for ( int i=0; i<count; i++ ) {

				for ( int j=0; j<count; j++ ) {

					if (i==j) {
						continue;
					}

					UpdateAdjacency( i, j );
				}
			}
		}


		public bool IsTrianglesAdjacent( int triIndexA, int triIndexB )
		{
			if (AdjacentVertices==null) throw new InvalidOperationException("AdjacentVertices is null -- build adjacency info first");
			if (AdjacentTriangles==null) throw new InvalidOperationException("AdjacentTriangles is null -- build adjacency info first");
			int length = AdjacentVertices.Length;
			var triCount = Triangles.Count;

			if (triIndexA<0 || triIndexA>=triCount) throw new ArgumentOutOfRangeException(nameof(triIndexA), triIndexA, "value must be less then triangle count: " + triCount.ToString());
			if (triIndexB<0 || triIndexB>=triCount) throw new ArgumentOutOfRangeException(nameof(triIndexB), triIndexB, "value must be less then triangle count: " + triCount.ToString());

			return 	AdjacentTriangles[triIndexA*3+0]==triIndexB
				||	AdjacentTriangles[triIndexA*3+1]==triIndexB
				||	AdjacentTriangles[triIndexA*3+2]==triIndexB
				;
		}


		bool UpdateAdjacency( int indexA, int indexB )
		{
			var a = Triangles[indexA];
			var b = Triangles[indexB];

			var adjTris		=	AdjacentTriangles;
			var adjVerts	=	AdjacentVertices;
			int adjIndex	=	indexA * 3;

			if (a.Index0 == b.Index1 && a.Index1 == b.Index0) { adjTris[adjIndex] = indexB; adjVerts[adjIndex] = b.Index2; return true; }
			if (a.Index0 == b.Index2 && a.Index1 == b.Index1) { adjTris[adjIndex] = indexB; adjVerts[adjIndex] = b.Index0; return true; }
			if (a.Index0 == b.Index0 && a.Index1 == b.Index2) { adjTris[adjIndex] = indexB; adjVerts[adjIndex] = b.Index1; return true; }

			adjIndex	=	indexA * 3 + 1;

			if (a.Index1 == b.Index1 && a.Index2 == b.Index0) { adjTris[adjIndex] = indexB; adjVerts[adjIndex] = b.Index2; return true; }
			if (a.Index1 == b.Index2 && a.Index2 == b.Index1) { adjTris[adjIndex] = indexB; adjVerts[adjIndex] = b.Index0; return true; }
			if (a.Index1 == b.Index0 && a.Index2 == b.Index2) { adjTris[adjIndex] = indexB; adjVerts[adjIndex] = b.Index1; return true; }

			adjIndex	=	indexA * 3 + 2;

			if (a.Index2 == b.Index1 && a.Index0 == b.Index0) { adjTris[adjIndex] = indexB; adjVerts[adjIndex] = b.Index2; return true; }
			if (a.Index2 == b.Index2 && a.Index0 == b.Index1) { adjTris[adjIndex] = indexB; adjVerts[adjIndex] = b.Index0; return true; }
			if (a.Index2 == b.Index0 && a.Index0 == b.Index2) { adjTris[adjIndex] = indexB; adjVerts[adjIndex] = b.Index1; return true; }

			return false;
		}


		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Vertex merge stuff :
		 * 
		-----------------------------------------------------------------------------------------*/


		[DebuggerDisplay("{Source} --> {Target}")]
		class ReIndex {
			public ReIndex( int source, int target )
			{
				Source	=	source;
				Target	=	target;
			}
			public readonly int Source;
			public readonly int Target;
		}


		/// <summary>
		/// Compares vertices
		/// </summary>
		/// <param name="v0"></param>
		/// <param name="v1"></param>
		/// <returns></returns>
		int CompareVertexPair ( ReIndex index0, ReIndex index1 )
		{
			var v0 = Vertices[index0.Source];
			var v1 = Vertices[index1.Source];

			if ( Vector3.DistanceSquared( v0.Position	, v1.Position	 ) > float.Epsilon * 8192 ) return 1;
			if ( Vector3.DistanceSquared( v0.Tangent	, v1.Tangent	 ) > float.Epsilon * 8192 ) return 1;
			if ( Vector3.DistanceSquared( v0.Binormal	, v1.Binormal	 ) > float.Epsilon * 8192 ) return 1;
			if ( Vector3.DistanceSquared( v0.Normal		, v1.Normal		 ) > float.Epsilon * 8192 ) return 1;
			if ( Vector2.DistanceSquared( v0.TexCoord0	, v1.TexCoord0	 ) > float.Epsilon * 8192 ) return 1;
			if ( Vector2.DistanceSquared( v0.TexCoord1	, v1.TexCoord1	 ) > float.Epsilon * 8192 ) return 1;

			if ( v0.Color0 != v1.Color0 ) return 1;
			//if ( v0.Color1 != v1.Color1 ) return false;

			if ( Vector4.DistanceSquared( v0.SkinWeights, v1.SkinWeights ) > float.Epsilon * 8192 ) return 1;
			if ( v0.SkinIndices != v1.SkinIndices ) return 1;

			return 0;
		}


		/// <summary>
		/// Merges vertices
		/// </summary>
		public void MergeVertices ( float tolerance )
		{
			var octree	=	new Octree<ReIndex>();
			var remap	=	new ReIndex[ Vertices.Count ];
			int counter	=	0;

			for (int index=0; index<Vertices.Count; index++) {

				var vertex		=	Vertices[index];
				var newIndex	=	octree.Insert( vertex.Position, new ReIndex(index,counter), CompareVertexPair, (idx) => counter++ );

				remap[ index ]	=	newIndex;
			}

			for (int i=0; i<Triangles.Count; i++) {

				var index0		=	remap[ Triangles[i].Index0 ].Target;
				var index1		=	remap[ Triangles[i].Index1 ].Target;
				var index2		=	remap[ Triangles[i].Index2 ].Target;
				var mtrl		=	Triangles[i].MaterialIndex;
				
				Triangles[i] = new MeshTriangle( index0, index1, index2, mtrl);
			}

			Vertices	=	remap
							.DistinctBy( value => value.Target )
							.Select( idx0 => Vertices[idx0.Source] )
							.ToList();
		}


		/// <summary>
		/// Separates triangles making triangle indices unique.
		/// Vertices will be duplicated if necessary.
		/// </summary>
		public void SeparateTriangles ()
		{
			var oldVertices = Vertices;

			Vertices	=	new List<MeshVertex>( TriangleCount * 3 );

			for ( int i=0; i<TriangleCount; i++ ) {

				var tri = Triangles[i];

				Vertices.Add( oldVertices[ tri.Index0 ] );
				Vertices.Add( oldVertices[ tri.Index1 ] );
				Vertices.Add( oldVertices[ tri.Index2 ] );

				tri.Index0 = i*3+0;
				tri.Index1 = i*3+1;
				tri.Index2 = i*3+2;

				Triangles[i] = tri;
			}
		}



		/// <summary>
		/// Computes bounding box for given mesh.
		/// </summary>
		public BoundingBox ComputeBoundingBox() 
		{
			Vector3 min = new Vector3( float.MaxValue );
			Vector3 max = new Vector3( float.MinValue );

			for( int i = Vertices.Count; --i >= 0; ) 
			{
				min = Vector3.Min( min, Vertices[i].Position );
				max = Vector3.Max( max, Vertices[i].Position );
			}

			boundingBox = new BoundingBox( min, max );

			return boundingBox;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="ray"></param>
		/// <returns></returns>
		public float Intersects( ref Ray ray )
		{
			float minDistance = float.MaxValue;
			
			/*if ( !BBox.Intersects( ref ray ) )
				return minDistance;*/

			float distance;
			for( int i = Triangles.Count; --i >= 0; ) {
				var v0	= Vertices[ Triangles[i].Index0 ].Position;
				var v1	= Vertices[ Triangles[i].Index1 ].Position;
				var v2	= Vertices[ Triangles[i].Index2 ].Position;
				if ( ray.Intersects( ref v0, ref v1, ref v2, out distance) ) {
					minDistance = (minDistance > distance) ? distance : minDistance;
				}
			}

			return minDistance;//*/
		}

		/*-----------------------------------------------------------------------------------------
		 *	Tangent space :
		-----------------------------------------------------------------------------------------*/
	
		/// <summary>
		/// Computes tangent frame
		/// </summary>
		public void ComputeTangentFrame ()
		{
			for ( int i=0; i<TriangleCount; i++) {

				var tri	  = Triangles[i];
				
				var inds  = new[]{ tri.Index0, tri.Index1, tri.Index2 };

				for (uint j=0; j<3; j++)
				{
					MeshVertex	vert0	=	Vertices[inds[(0+j)%3]];
					MeshVertex	vert1	=	Vertices[inds[(1+j)%3]];
					MeshVertex	vert2	=	Vertices[inds[(2+j)%3]];

			
					Vector3	v0	= vert1.Position  - vert0.Position;
					Vector3	v1	= vert2.Position  - vert0.Position;
					Vector2	t0	= vert1.TexCoord0 - vert0.TexCoord0;	
					Vector2	t1	= vert2.TexCoord0 - vert0.TexCoord0;	

					{	// X :
						float	det		= t0.X * t1.Y  -  t1.X * t0.Y;
						float	dett	= v0.X * t1.Y  -  v1.X * t0.Y;
						float	detb	= t0.X * v1.X  -  t1.X * v0.X;
						//if (Math.Abs(det)<float.Epsilon) Log.Warning("Tri is too small");
						vert0.Tangent.X		= dett / det;								
						vert0.Binormal.X	= detb / det;								
					}
					{	// Y :
						float	det		= t0.X * t1.Y  -  t1.X * t0.Y;
						float	dett	= v0.Y * t1.Y  -  v1.Y * t0.Y;
						float	detb	= t0.X * v1.Y  -  t1.X * v0.Y;
						//if (Math.Abs(det)<float.Epsilon) Log.Warning("Tri is too small");
						vert0.Tangent.Y		= dett / det;								
						vert0.Binormal.Y	= detb / det;								
					}
					{	// Z :
						float	det		= t0.X * t1.Y  -  t1.X * t0.Y;
						float	dett	= v0.Z * t1.Y  -  v1.Z * t0.Y;
						float	detb	= t0.X * v1.Z  -  t1.X * v0.Z;
						//if (Math.Abs(det)<float.Epsilon) Log.Warning("Tri is too small");
						vert0.Tangent.Z		= dett / det;								
						vert0.Binormal.Z	= detb / det;								
					}

					//vert0.Normal	= Vector3.Cross(v1, v0);
			
					if ( vert0.Tangent.Length()  > float.Epsilon * 8 ) vert0.Tangent.Normalize();
					if ( vert0.Binormal.Length() > float.Epsilon * 8 ) vert0.Binormal.Normalize();
					if ( vert0.Normal.Length()   > float.Epsilon * 8 ) vert0.Normal.Normalize();
					//vert0.Tangent.Normalize()  ;
					//vert0.Binormal.Normalize() ;
					//vert0.Normal.Normalize()   ;
			
					Vector3	temp;
					temp = Vector3.Cross( vert0.Tangent, vert0.Normal );
					vert0.Tangent = Vector3.Cross( vert0.Normal, temp );
			
					temp = Vector3.Cross( vert0.Binormal, vert0.Normal );
					vert0.Binormal = Vector3.Cross( vert0.Normal, temp );//*/

					//	assign vertex
					Vertices[inds[(0+j)%3]] = vert0;
				}
			}
		}

		/*-----------------------------------------------------------------------------------------
		 *	UV shell
		-----------------------------------------------------------------------------------------*/

		public int BuildShellIndices( int baseShellIndex )
		{
			var adjacency = new Dictionary<Vector2, HashSet<int>>();
			var triangles = Triangles.ToArray();

			for (int i=0; i<TriangleCount; i++)
			{
				var tri	=	Triangles[i];
				var uvs =	new Vector2[3];
				uvs[0]	=	Vertices[ tri.Index0 ].TexCoord1;
				uvs[1]	=	Vertices[ tri.Index1 ].TexCoord1;
				uvs[2]	=	Vertices[ tri.Index2 ].TexCoord1;

				triangles[i].ShellIndex = -1;

				HashSet<int> adjTris;

				for (int j=0; j<3; j++)
				{
					if (adjacency.TryGetValue( uvs[j], out adjTris ))
					{
						adjTris.Add( i );
					}
					else
					{
						adjTris = new HashSet<int>();
						adjTris.Add( i );
						adjacency.Add( uvs[j], adjTris ); 
					}
				}
			}

			int shellIndexCounter = baseShellIndex;
			int nextStartingTri = 0;

			var trisQueue = new Queue<int>();

			while ( nextStartingTri < TriangleCount )
			{											
				if ( triangles[nextStartingTri].ShellIndex < 0 )
				{
					shellIndexCounter++;

					trisQueue.Enqueue( nextStartingTri );

					while (trisQueue.Any())
					{
						var triIndex = trisQueue.Dequeue();

						if ( triangles[ triIndex ].ShellIndex < 0 )
						{
							triangles[ triIndex ].ShellIndex = shellIndexCounter;

							var uvs =	new Vector2[3];
							uvs[0]	=	Vertices[ triangles[ triIndex ].Index0 ].TexCoord1;
							uvs[1]	=	Vertices[ triangles[ triIndex ].Index1 ].TexCoord1;
							uvs[2]	=	Vertices[ triangles[ triIndex ].Index2 ].TexCoord1;

							for (int j=0; j<3; j++)
							{
								foreach ( var adjTriIndex in adjacency[ uvs[j] ] )
								{
									if ( adjTriIndex!=triIndex && triangles[ adjTriIndex ].ShellIndex < 0 )
									{	 
										trisQueue.Enqueue( adjTriIndex );
									}
								}
							}
						}
					}
				}
				else
				{
					nextStartingTri++;
				}
			}

			Triangles.Clear();
			Triangles.AddRange( triangles );

			return shellIndexCounter;
		}

		/*-----------------------------------------------------------------------------------------
		 *	Some stuff
		-----------------------------------------------------------------------------------------*/

		public void BuildSurfels ( float maxArea )
		{
			Surfels.Clear();

			var rand = new Random();

			foreach ( var tri in Triangles ) {
				
				var p0		=	Vertices[ tri.Index0 ].Position;
				var p1		=	Vertices[ tri.Index1 ].Position;
				var p2		=	Vertices[ tri.Index2 ].Position;

				var v01		=	p1 - p0;
				var v02		=	p2 - p0;

				var n		=	Vector3.Cross( v01, v02 );

				var	area	=	n.Length() / 2;

				n.Normalize();

				if (area>1024*maxArea) {
					Log.Warning("Triangle is too big");
					continue;
				}


				if (area>maxArea) {
					int count	=	Math.Max(0, (int)(area / maxArea));

					for ( int i=0; i<count; i++ ) {
					
						var surfel	=	new MeshSurfel();

						var rpos	=	rand.NextPointOnTriangle( p0, p1, p2 );

						surfel.Position =	rpos;
						surfel.Area		=	area / count;
						surfel.Normal	=	n;

						surfel.Albedo	=	new Color(128,128,128,255);

						Surfels.Add( surfel );
					}
				} else {	

					var prob = area / maxArea;

					if (rand.NextFloat(0,1)<prob) {
						
						var surfel	=	new MeshSurfel();

						var rpos	=	rand.NextPointOnTriangle( p0, p1, p2 );

						surfel.Position =	rpos;
						surfel.Area		=	maxArea;
						surfel.Normal	=	n;

						surfel.Albedo	=	new Color(128,128,128,255);

						Surfels.Add( surfel );
					}
				}
			}

			//Log.Message("{0} triangles", Triangles.Count );
			//Log.Message("{0} surfels are built", Surfels.Count );
		}

		/*-----------------------------------------------------------------------------------------
		 *	Some stuff
		-----------------------------------------------------------------------------------------*/

		/// <summary>
		///	Removes degenerate triangles
		/// </summary>
		public void RemoveDegenerateTriangles ()
		{
			Triangles.RemoveAll( tri => tri.IsDegenerate() );
		}


		/// <summary>
		/// PrintMeshInfo
		/// </summary>
		public void PrintMeshInfo ()
		{
			Console.WriteLine("Vertex count   : {0}", Vertices.Count );
			Console.WriteLine("Triangle count : {0}", Triangles.Count );
			Console.WriteLine("Shading groups : {0}", Subsets.Count );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		public void Deserialize( BinaryReader reader )
		{
			boundingBox	=	reader.Read<BoundingBox>();
			
			//	read vertices :
			int vertexCount	=	reader.ReadInt32();
			Vertices		=	reader.Read<MeshVertex>( vertexCount ).ToList();
			
			//	read trinagles :
			int trisCount	=	reader.ReadInt32();
			Triangles		=	reader.Read<MeshTriangle>( trisCount ).ToList();
							
			//	read subsets :
			int subsetCount	=	reader.ReadInt32();
			Subsets			=	reader.Read<MeshSubset>( subsetCount ).ToList();

			//	read UV-set names :
			int uvSetCount	=	reader.ReadInt32();
			for (int i=0; i<uvSetCount; i++) UVSetNames.Add( reader.ReadString() );
							
			//	read adjacent tris :
			int adjTrisCount		=	reader.ReadInt32();
			AdjacentTriangles	=	reader.Read<int>( adjTrisCount );

			//	read adjacent verts :
			int adjVertsCount	=	reader.ReadInt32();
			AdjacentVertices	=	reader.Read<int>( adjVertsCount );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="writer"></param>
		public void Serialize( BinaryWriter writer )
		{
			writer.Write( boundingBox );
			
			writer.Write( VertexCount );
			writer.Write( Vertices.ToArray() );
			
			writer.Write( TriangleCount );
			writer.Write( Triangles.ToArray() );

			writer.Write( Subsets.Count );
			writer.Write( Subsets.ToArray() );

			writer.Write( UVSetNames.Count );
			foreach ( var uvSetName in UVSetNames ) writer.Write( uvSetName );

			writer.Write( AdjacentTriangles.Length );
			writer.Write( AdjacentTriangles );

			writer.Write( AdjacentVertices.Length );
			writer.Write( AdjacentVertices );
		}
	}
}
