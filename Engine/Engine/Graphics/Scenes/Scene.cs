using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using SharpDX;
using Fusion;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Drivers.Graphics;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Engine.Graphics.Ubershaders;


namespace Fusion.Engine.Graphics.Scenes {

	public enum TimeMode : int {
		Unknown			=       0,
		Frames24		=   24000,
		Frames30		=   30000,
		Frames48		=   48000,
		Frames50		=   50000,
		Frames59dot94	=   59940,
		Frames60		=   60000,
		Frames72		=   70000,
		Frames96		=   80000,
		Frames100		=   90000,
		Frames120		=  120000,
		Frames1000		= 1000000,
	}


	public sealed class Scene : DisposableBase {

		List<Node>			nodes		=	new List<Node>();
		List<Mesh>			meshes		=	new List<Mesh>();
		List<Material>		materials	=	new List<Material>();
		AnimationTakeCollection	takes		=	new AnimationTakeCollection();

		public static readonly Scene Empty = new Scene();

		int firstFrame = 0;
		int lastFrame = 0;


		public static TimeSpan ComputeFrameLength( int frameCount, TimeMode timeMode )
		{
			return TimeSpan.FromSeconds( frameCount * 1000.0 / (int)timeMode );
		}

		public static void TimeToFrames ( TimeSpan time, TimeMode timeMode, out int prevFrame, out int nextFrame, out float weight )
		{
			double floatFrame = time.TotalSeconds * (int)timeMode / 1000.0;

			prevFrame	=	(int)Math.Floor( floatFrame );
			nextFrame	=	(int)Math.Ceiling( floatFrame );
			weight		=	(float)( floatFrame - prevFrame );
		}



		/// <summary>
		/// Fixes Maya's rotated by 180 degrees camera matrix
		/// </summary>
		/// <param name="globalTransform"></param>
		/// <returns></returns>
		public static Matrix FixGlobalCameraMatrix ( Matrix globalTransform )
		{
			return Matrix.RotationY( -MathUtil.PiOverTwo ) * globalTransform;
		}


		/// <summary>
		/// List of scene nodes
		/// </summary>
		public IList<Node> Nodes { 
			get {
				return nodes;
			}
		}


		/// <summary>
		/// List of scene meshes.
		/// </summary>
		public IList<Mesh> Meshes { 
			get {
				return meshes;
			}
		}


		/// <summary>
		/// List of scene materials.
		/// </summary>
		public IList<Material> Materials { 
			get {
				return materials;
			}
		}


		/// <summary>
		/// List of scene takes
		/// </summary>
		public AnimationTakeCollection Takes {
			get {
				return takes;
			}
		}



		/// <summary>
		/// Gets and sets time mode
		/// </summary>
		public TimeMode TimeMode {
			get; set;
		} = TimeMode.Frames30;


		/// <summary>
		/// Gets first inclusive entire scene animation frame
		/// </summary>
		public int FirstFrame {
			get {
				return firstFrame;
			}
		}


		/// <summary>
		/// Gets last inclusive entire scene animation frame
		/// </summary>
		public int LastFrame {
			get {
				return firstFrame;
			}
		}


		/// <summary>
		/// Frames rate
		/// </summary>
		public float FramesPerSecond {
			get {
				return ((int)TimeMode) / 1000.0f;
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="frame"></param>
		/// <returns></returns>
		public TimeSpan GetTime ( int frame )
		{
			return TimeSpan.FromMilliseconds( frame * (int)TimeMode );
		}


		/// <summary>
		/// Constrcutor.
		/// </summary>
		public Scene ()
		{
		}


		public Scene( TimeMode timeMode )
		{
			this.TimeMode	=	timeMode;
		}


		/// <summary>
		/// Creates empty scene
		/// </summary>
		/// <returns></returns>
		static public Scene CreateEmptyScene ()
		{
			var scene = new Scene();
			scene.Nodes.Add( new Node() );
			return scene;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose ( bool disposing )
		{
			if (disposing) {
				foreach ( var mesh in Meshes ) {
					if (mesh!=null) {
						mesh.Dispose();
					}
				}
			}
			base.Dispose(disposing);
		}


		/*---------------------------------------------------------------------
		 * 
		 *	Topology stuff :
		 *	
		---------------------------------------------------------------------*/

		public string GetFullNodePath ( Node node )
		{
			string path;

			path = node.Name;

			while ( node.ParentIndex >= 0 ) {
				node = Nodes[ node.ParentIndex ];
				path = node.Name + "|" + path;
			}

			return path;
		}



		/// <summary>
		/// Gets node index by its name
		/// </summary>
		/// <param name="name"></param>
		/// <returns>Negatvie value if such node does not exist</returns>
		public int GetNodeIndex ( string name )
		{
			for (int i=0; i<Nodes.Count; i++) {
				if (Nodes[i].Name==name) {
					return i;
				}
			}
			return -1;
		}



		/// <summary>
		/// Gets map dictionary
		/// </summary>
		/// <returns></returns>
		public Dictionary<string,Node> GetPathNodeMapping ()
		{
			return Nodes.ToDictionary( node => GetFullNodePath(node), node => node );
		}


		/// <summary>
		/// Copies absolute transform to provided array.
		/// </summary>
		/// <param name="destination"></param>
		public void CopyLocalTransformsTo ( Matrix[] destination )
		{
			for ( int i=0; i<Nodes.Count; i++) {
				
				var node = Nodes[i];
				var transform = node.Transform;

				destination[i] = transform;
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="destination"></param>
		public void ComputeAbsoluteTransforms ( Matrix[] destination )
		{
			ComputeAbsoluteTransforms( destination, Matrix.Identity );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public Matrix[] ComputeAbsoluteTransforms()
		{
			var transforms = new Matrix[Nodes.Count];
			ComputeAbsoluteTransforms( transforms );
			return transforms;
		}


		/// <summary>
		/// Copies absolute transform to provided array.
		/// </summary>
		/// <param name="destination"></param>
		public void ComputeAbsoluteTransforms ( Matrix[] destination, Matrix preTransform )
		{
			if ( destination.Length < Nodes.Count ) 
			{
				throw new ArgumentOutOfRangeException("destination.Length must be greater of equal to Nodes.Count");
			}

			for ( int i=0; i<Nodes.Count; i++) 
			{
				var node = Nodes[i];
				var transform = node.Transform;
				var parentIndex = node.ParentIndex;

				while ( parentIndex!=-1 ) 
				{
					var parent	=	Nodes[ parentIndex ].Transform;

					transform	=	transform * parent;
					parentIndex =	Nodes[ parentIndex ].ParentIndex;
				}

				destination[i] = transform * preTransform;
			} 
		}


		/// <summary>
		/// Computes local transforms for given global
		/// </summary>
		/// <param name="destination"></param>
		public Matrix ComputeLocalTransform( Matrix[] globalTransforms, int index )
		{
			if ( globalTransforms == null ) throw new ArgumentNullException("source");
			if ( globalTransforms.Length < Nodes.Count ) throw new ArgumentOutOfRangeException("source.Length must be greater of equal to Nodes.Count");

			var node			=	Nodes[index];
			var parentIndex		=	node.ParentIndex;
			var globalTransform	=	globalTransforms[index];
			
			return 	(parentIndex<0) ? globalTransform : globalTransform * Matrix.Invert( globalTransforms[parentIndex] );
		}


		/// <summary>
		/// Computes absolute transformations using local transforms and scene's hierarchy.
		/// Number of source matricies, destination matricies and node count must be equal.
		/// Arguments 'sourceLocalTransforms' and 'destinationGlobalTransforms' may be the same object.
		/// </summary>
		/// <param name="sourceLocalTransforms"></param>
		/// <param name="destinationGlobalTransforms"></param>
		public void ComputeAbsoluteTransforms ( Matrix[] source, Matrix[] destination )
		{
			if ( source == null ) throw new ArgumentNullException("source");
			if ( destination == null ) throw new ArgumentNullException("destination");
			if ( source.Length < Nodes.Count ) throw new ArgumentOutOfRangeException("source.Length must be greater of equal to Nodes.Count");
			if ( destination.Length < Nodes.Count ) throw new ArgumentOutOfRangeException("destination.Length must be greater of equal to Nodes.Count");

			for ( int i=0; i<Nodes.Count; i++) {
				
				var node		= Nodes[i];
				var transform	= source[i];
				var parentIndex = node.ParentIndex;

				while ( parentIndex!=-1 ) {
					transform	=	transform * source[ parentIndex ];
					parentIndex =	Nodes[ parentIndex ].ParentIndex;
				}

				destination[i] = transform;
			}
		}



		/// <summary>
		/// Computes global bones transforms for skinning taking in account bind position.
		/// </summary>
		/// <param name="source">Global bone transforms</param>
		/// <param name="destination">Global bone transforms multiplied by bind pose matrix</param>
		public void ComputeBoneTransforms ( Matrix[] source, Matrix[] destination )
		{
			for ( int i=0; i<Nodes.Count; i++ ) 
			{
				#warning PERFORMANCE: precompute inverse bind pose transform
				destination[i] = Matrix.Invert( Nodes[i].BindPose ) * source[i];
			}
		}


		/// <summary>
		/// Computes global bones transforms for skinning taking in account bind position.
		/// </summary>
		/// <param name="source">Global bone transforms</param>
		/// <param name="destination">Global bone transforms multiplied by bind pose matrix</param>
		public void ComputeBoneTransformsFromLocal ( Matrix[] source, Matrix[] destination )
		{
			ComputeAbsoluteTransforms( source, destination );

			for ( int i=0; i<Nodes.Count; i++ ) 
			{
				#warning PERFORMANCE: precompute inverse bind pose transform
				destination[i] = Matrix.Invert( Nodes[i].BindPose ) * destination[i];
			}
		}

		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Animation stuff :
		 * 
		-----------------------------------------------------------------------------------------*/

		/// <summary>
		/// 
		/// </summary>
		/// <param name="channelNodeIndex"></param>
		/// <returns></returns>
		public Node[] GetChannelNodes ( int channelNodeIndex )
		{
			if (channelNodeIndex<0) {
				return new Node[0];
			}
			if (channelNodeIndex>=Nodes.Count) {
				throw new ArgumentOutOfRangeException("channelNodeIndex >= Nodes.Count");
			}

			var parent = Nodes[ channelNodeIndex ];

			return Nodes
				.Where( node => IsParent( parent, node ) )
				.ToArray();
		}



		/// <summary>
		/// Gets indices of all channel children
		/// If negative value provided returns indices of all nodes.
		/// </summary>
		/// <param name="chnnelNodeIndex"></param>
		/// <returns></returns>
		public int[] GetChannelNodeIndices ( int channelNodeIndex )
		{
			if (channelNodeIndex<0) {
				return Enumerable.Range(0, Nodes.Count).ToArray();
			}

			return GetChannelNodes(channelNodeIndex)
				.Select( node => Nodes.IndexOf(node) )
				.ToArray();
		}



		bool IsParent ( Node parent, Node child )
		{
			while ( child.ParentIndex >= 0 ) {
				if ( Nodes[ child.ParentIndex ] == parent ) {
					return true;
				}

				child = Nodes[ child.ParentIndex ];
			}

			return false;
		}

		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Optimization stuff :
		 * 
		-----------------------------------------------------------------------------------------*/

		class Comparer : IEqualityComparer<Mesh> {
			public bool Equals ( Mesh a, Mesh b ) 
			{
				return a.Equals(b);
			}
			
			public int GetHashCode ( Mesh a ) {
				return a.GetHashCode();
			}	
		}

		/// <summary>
		/// 
		/// </summary>
		public void DetectAndMergeInstances ()
		{
			//	creates groups of each mesh :
			var nodeMeshGroups	=	Nodes
									.Where( n1 => n1.MeshIndex >= 0 )
									.Select( n2 => new { Node = n2, Mesh = Meshes[n2.MeshIndex] } )
									.GroupBy( nm => nm.Mesh, nm => nm.Node )
									.ToArray();

			//foreach ( var ig in nodeMeshGroups ) {
			//	Log.Message("{0}", ig.Key.ToString());
			//	foreach ( var n in ig ) {
			//		Log.Message("  {0}", n.Name );
			//	}
			//}

			meshes	=	nodeMeshGroups
						.Select( nmg => nmg.Key )
						.ToList();

			for	( int i=0; i<nodeMeshGroups.Length; i++) {
				foreach ( var n in nodeMeshGroups[i] ) {
					n.MeshIndex = i;
				}
			}
		}

		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Save/Load stuff :
		 * 
		-----------------------------------------------------------------------------------------*/

		/// <summary>
		/// Loads scene
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public static Scene Load( Stream stream, bool skipMeshData = false ) 
		{
			var scene = new Scene();
			
			using( var reader = new BinaryReader( stream ) ) {

				reader.ExpectFourCC("SCN2", "scene");

				//---------------------------------------------
				//scene.StartTime			=	new TimeSpan( reader.ReadInt64() );
				//scene.EndTime			=	new TimeSpan( reader.ReadInt64() );
				scene.TimeMode			=	(TimeMode)reader.ReadInt32();
				scene.firstFrame		=	reader.ReadInt32();
				scene.lastFrame			=	reader.ReadInt32();
				
				reader.ExpectFourCC("ANIM", "scene");

				var takeCount			=	reader.ReadInt32();

				for (int i=0; i<takeCount; i++) {
					reader.ExpectFourCC("TAKE", "scene");
					var take = AnimationTake.Read( reader );
					scene.Takes.Add( take );
				}

				//---------------------------------------------
				reader.ExpectFourCC("MTRL", "scene");

				var mtrlCount = reader.ReadInt32();

				scene.materials.Clear();
				
				for ( int i=0; i<mtrlCount; i++) {
					var mtrl	=	new Material();
					mtrl.Name	=	reader.ReadString();

					if (reader.ReadBoolean()==true) {
						mtrl.ColorMap = reader.ReadString();
					} else {
						mtrl.ColorMap = null;
					}
					scene.Materials.Add( mtrl );
				}

				//---------------------------------------------
				reader.ExpectFourCC("NODE", "scene");
				
				var nodeCount = reader.ReadInt32();
				
				for ( int i = 0; i < nodeCount; ++i ) {
					var node = new Node();
					node.Name			=	reader.ReadString();
					node.Comment		=	reader.ReadString();
					node.ParentIndex	=	reader.ReadInt32();
					node.MeshIndex		=	reader.ReadInt32();
					node.Transform		=	reader.Read<Matrix>();
					node.BindPose		=	reader.Read<Matrix>();
					scene.nodes.Add( node );
				}

				if (skipMeshData) {
					return scene;
				}

				//---------------------------------------------
				reader.ExpectFourCC("MESH", "scene");
				
				var meshCount = reader.ReadInt32();

				for ( int i = 0; i < meshCount; i++ ) {
					var mesh = new Mesh();
					mesh.Deserialize( reader );
					scene.Meshes.Add( mesh );
				}
			}

			return scene;
		}



		/// <summary>
		/// Saves scene
		/// </summary>
		/// <param name="path"></param>
		public void Save( Stream stream ) {
			
			using( var writer = new BinaryWriter( stream ) ) {

				//---------------------------------------------
				writer.Write(new[]{'S','C','N','2'});

				writer.Write( (int)TimeMode );
				writer.Write( FirstFrame );
				writer.Write( LastFrame	);

				//---------------------------------------------
				writer.Write(new[]{'A','N','I','M'});

				writer.Write( takes.Count );

				for (int i=0; i<takes.Count; i++) {

					writer.Write(new[]{'T','A','K','E'});
					AnimationTake.Write( takes[i], writer );
				}

				//---------------------------------------------
				writer.Write(new[]{'M','T','R','L'});

				writer.Write( Materials.Count );

				foreach ( var mtrl in Materials ) {
					writer.Write( mtrl.Name );
					if ( mtrl.ColorMap!=null ) {
						writer.Write( true );
						writer.Write( mtrl.ColorMap );
					} else {
						writer.Write( false );
					}
				}

				//---------------------------------------------
				writer.Write(new[]{'N','O','D','E'});

				writer.Write( Nodes.Count );

				foreach ( var node in Nodes ) {
					writer.Write( node.Name );
					writer.Write( node.Comment );
					writer.Write( node.ParentIndex );
					writer.Write( node.MeshIndex );
					writer.Write( node.Transform );
					writer.Write( node.BindPose );
				}

				//---------------------------------------------
				writer.Write(new[]{'M','E','S','H'});

				writer.Write( Meshes.Count );

				foreach ( var mesh in Meshes ) {
					mesh.Serialize( writer );
				}
			}
		}



		/// <summary>
		/// 
		/// </summary>
		public void StripNamespaces ()
		{
			foreach ( var mtrl in materials ) {
				mtrl.Name	=	StripNamespace(mtrl.Name);
			}

			foreach ( var node in nodes ) {
				node.Name	=	StripNamespace(node.Name);
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		string StripNamespace ( string name )
		{
			return name.Split(new[]{':'}).Last();
		}



		/// <summary>
		/// Make texture paths relative to base directory.
		/// </summary>
		/// <param name="sceneFullPath"></param>
		/// <param name="baseDirectory"></param>
		public void ResolveTexturePathToBaseDirectory ( string sceneFullPath, string baseDirectory )
		{
			Log.Message("{0}", baseDirectory);
			var baseDirUri			= new Uri( baseDirectory + @"\" );
			var sceneDirFullPath	= Path.GetDirectoryName( Path.GetFullPath( sceneFullPath ) ) + @"\";

			Log.Message("{0}", baseDirUri );

			foreach ( var mtrl in Materials ) {
					
				if (mtrl.ColorMap==null) {
					continue;
				}
				Log.Message( "-" + mtrl.ColorMap );

				var absTexPath		=	Path.Combine( sceneDirFullPath, mtrl.ColorMap );
				var texUri			=	new Uri( absTexPath );
				mtrl.ColorMap		=	baseDirUri.MakeRelativeUri( texUri ).ToString();

				Log.Message( "-" + texUri );
				Log.Message( "+" + mtrl.ColorMap );
			}
		}



		public int CalculateNodeDepth ( Node node )
		{
			int depth = 0;
			while (node.ParentIndex>=0) {
				node = Nodes[node.ParentIndex];
				depth++;
			}
			return depth;
		}
	}
}
