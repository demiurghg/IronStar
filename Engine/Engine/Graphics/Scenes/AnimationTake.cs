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

namespace Fusion.Engine.Graphics.Scenes {

	public class AnimationTake {

		readonly string		name;

		readonly AnimationKey[]	animData;
		readonly AnimationKey[]	animDelta;
		readonly int		frameCount;
		readonly int		nodeCount;
		readonly int		firstFrame;
		readonly int		lastFrame;

		/// <summary>
		/// Gets take name
		/// </summary>
		public string Name { get { return name; } }

		/// <summary>
		/// First inclusive frame
		/// </summary>
		public int FirstFrame { get { return firstFrame; } }

		/// <summary>
		/// Last inclusive frame
		/// </summary>
		public int LastFrame { get { return lastFrame; } }

		/// <summary>
		/// Gets frame count
		/// </summary>
		public int FrameCount { get { return frameCount; } }

		/// <summary>
		/// Gets nodes count
		/// </summary>
		public int NodeCount { get { return nodeCount; } }


		/// <summary>
		/// Creates instance of anim take
		/// </summary>
		/// <param name="nodeCount">Node count</param>
		/// <param name="firstFrame">First inclusive frame</param>
		/// <param name="lastFrame">Last inclusive frame</param>
		public AnimationTake ( string name, int nodeCount, int firstFrame, int lastFrame )
		{
			this.name		=	name;
			this.nodeCount	=	nodeCount;

			this.firstFrame	=	firstFrame;
			this.lastFrame	=	lastFrame;
			this.frameCount	=	lastFrame - firstFrame + 1;

			this.animData	=	new AnimationKey[ nodeCount * frameCount ];
			this.animDelta	=	new AnimationKey[ nodeCount * frameCount ];

			for ( int i=0; i<animData.Length; i++)
			{
				animData[i]		=	AnimationKey.Identity;
				animDelta[i]	=	AnimationKey.Identity;
			}
		}



		/// <summary>
		/// Evaluate animation for given take
		/// </summary>
		/// <param name="frame"></param>
		/// <param name="mode"></param>
		public void Evaluate ( int frame, AnimationWrapMode wrapMode, Matrix[] destination )
		{
			if (destination==null) {
				throw new ArgumentNullException("destination");
			}

			if (destination.Length<nodeCount) {
				throw new ArgumentOutOfRangeException("destination.Length");
			}

			switch (wrapMode) 
			{
				case AnimationWrapMode.Clamp:	frame = MathUtil.Clamp( frame, FirstFrame, LastFrame ); break;
				case AnimationWrapMode.Repeat:	frame = MathUtil.Wrap ( frame, FirstFrame, LastFrame ); break;
				default: throw new ArgumentException("mode");
			}
				
			frame = frame - FirstFrame;

			for (int i=0; i<nodeCount; i++) 
			{
				destination[i] = animData[ Address( frame, i ) ].Transform;
			}
		}


		int Address ( int frame, int node )
		{
			return frame * nodeCount + node;
		}


		/// <summary>
		/// Sets anim key
		/// </summary>
		/// <param name="frame"></param>
		/// <param name="node"></param>
		/// <param name="transform"></param>
		public void SetKey ( int frame, int node, Matrix transform )
		{
			if (frame<FirstFrame) {
				throw new ArgumentOutOfRangeException("frame < FirstFrame");
			}
			if (frame>LastFrame) {
				throw new ArgumentOutOfRangeException("frame < LastFrame");
			}

			if (node<0) {
				throw new ArgumentOutOfRangeException("node < 0");
			}
			if (node>=NodeCount) {
				throw new ArgumentOutOfRangeException("node >= NodeCount");
			}

			animData[ Address( frame - firstFrame, node ) ] = new AnimationKey( transform );

		}


		public AnimationKey[] GetPose( int frame )
		{
			var pose = new AnimationKey[NodeCount];
			var key  = new AnimationKey();

			for (int i=0; i<NodeCount; i++)
			{
				GetKey( frame, i, out key );
				pose[i] = key;
			}

			return pose;
		}


		public void GetPose( int frame, AnimationBlendMode blendMode, AnimationKey[] pose )
		{
			if (pose.Length<NodeCount) throw new ArgumentException("Length of the pose array is less than number of nodes");

			var key  = new AnimationKey();

			for (int i=0; i<NodeCount; i++)
			{
				if (blendMode==AnimationBlendMode.Override)	GetKey( frame, i, out key ); else
				if (blendMode==AnimationBlendMode.Additive)	GetDeltaKey( frame, i, out key ); else
				throw new ArgumentException("blendMode");
					
				pose[i] = key;
			}
		}


		/// <summary>
		/// Sets anim key
		/// </summary>
		/// <param name="frame"></param>
		/// <param name="node"></param>
		/// <param name="transform"></param>
		public void GetKey ( int frame, int node, out Matrix transform )
		{
			if (frame<FirstFrame) {
				throw new ArgumentOutOfRangeException("frame < FirstFrame");
			}
			if (frame>LastFrame) {
				throw new ArgumentOutOfRangeException("frame < LastFrame");
			}

			if (node<0) {
				throw new ArgumentOutOfRangeException("node < 0");
			}
			if (node>=NodeCount) {
				throw new ArgumentOutOfRangeException("node >= NodeCount");
			}

			transform  = animData[ Address( frame - firstFrame, node ) ].Transform;
		}


		/// <summary>
		/// Sets anim key
		/// </summary>
		/// <param name="frame"></param>
		/// <param name="node"></param>
		/// <param name="transform"></param>
		public void GetKey ( int frame, int node, out AnimationKey key )
		{
			if (frame<FirstFrame) {
				throw new ArgumentOutOfRangeException("frame < FirstFrame");
			}
			if (frame>LastFrame) {
				throw new ArgumentOutOfRangeException("frame < LastFrame");
			}

			if (node<0) {
				throw new ArgumentOutOfRangeException("node < 0");
			}
			if (node>=NodeCount) {
				throw new ArgumentOutOfRangeException("node >= NodeCount");
			}

			key  = animData[ Address( frame - firstFrame, node ) ];
		}


		public Matrix GetKey( int frame, int node )
		{
			Matrix t;
			GetKey( frame, node, out t );
			return t;
		}


		/// <summary>
		/// Sets anim key
		/// </summary>
		/// <param name="frame"></param>
		/// <param name="node"></param>
		/// <param name="transform"></param>
		public void GetDeltaKey ( int frame, int node, out AnimationKey key )
		{
			if (frame<FirstFrame) {
				throw new ArgumentOutOfRangeException("frame < FirstFrame");
			}
			if (frame>LastFrame) {
				throw new ArgumentOutOfRangeException("frame < LastFrame");
			}

			if (node<0) {
				throw new ArgumentOutOfRangeException("node < 0");
			}
			if (node>=NodeCount) {
				throw new ArgumentOutOfRangeException("node >= NodeCount");
			}

			key  = animDelta[ Address( frame - firstFrame, node ) ];
		}


		/// <summary>
		/// 
		/// </summary>
		public void ComputeDeltaAnimation ()
		{
			var initialPose = new Matrix[ NodeCount ];
			Evaluate( FirstFrame, AnimationWrapMode.Clamp, initialPose );

			for ( int i=0; i < NodeCount; i++ ) 
			{
				initialPose[i]	=	Matrix.Invert( initialPose[i] );
			}

			for ( int frame = FirstFrame; frame <= LastFrame; frame++ ) 
			{
				for ( int node = 0; node<NodeCount; node++ ) 
				{
					int addr = Address( frame - firstFrame, node );
					animDelta[ addr ] = new AnimationKey( initialPose[node] * animData[ addr ].Transform );
				}
			}
		}


		public void RecordTake( int node, Func<int,float,Matrix> action )
		{
			float d = 1.0f / frameCount;

			for (int i=firstFrame; i<=lastFrame; i++)
			{
				SetKey( i, node, action(i, i*d) );
			}

			ComputeDeltaAnimation();
		}


		#region	GetAnimSnapshot
		#if false
		/// <summary>
		/// Get local matricies for each node for given animation frame.
		/// First, this method copies node's local matricies
		/// then, it replace this matrices by node's animation track values.
		/// </summary>
		/// <param name="frame"></param>
		/// <returns></returns>
		public void GetAnimSnapshot ( float frame, int firstFrame, int lastFrame, AnimationWrapMode animMode, Matrix[] destination )
		{
			if ( animData==null ) {
				throw new InvalidOperationException("Animation data is not created");
			}

			if (destination.Length<Nodes.Count) {
				throw new ArgumentOutOfRangeException("destination.Length must be greater of equal to Nodes.Count");
			}

			if ( firstFrame < FirstFrame || firstFrame > LastFrame ) {
				throw new ArgumentOutOfRangeException("firstFrame");
			}
			if ( lastFrame < FirstFrame || lastFrame > LastFrame ) {
				throw new ArgumentOutOfRangeException("firstFrame");
			}
			if ( firstFrame > lastFrame ) {
				throw new ArgumentOutOfRangeException("firstFrame > lastFrame");
			}


			int frame0	=	(int)Math.Floor( frame );
			int frame1	=	frame0 + 1;
			var factor	=	(frame >= 0) ? (frame%1) : (1 + frame%1);

			if (animMode==AnimationWrapMode.Repeat) {
				frame0	=	MathUtil.Wrap( frame0, firstFrame, lastFrame );
				frame1	=	MathUtil.Wrap( frame1, firstFrame, lastFrame );
			} else if (animMode==AnimationWrapMode.Clamp) {
				frame0	=	MathUtil.Clamp( frame0, firstFrame, lastFrame );
				frame1	=	MathUtil.Clamp( frame1, firstFrame, lastFrame );
			}

			for (int i=0; i<Nodes.Count; i++) {
				var node = Nodes[i];

				if (node.TrackIndex<0) {
					destination[i] = node.Transform;
				} else {

					var x0	=	GetAnimKey( frame0, node.TrackIndex );
					var x1	=	GetAnimKey( frame1, node.TrackIndex );

					Quaternion q0, q1;
					Vector3 t0, t1;
					Vector3 s0, s1;

					x0.Decompose( out s0, out q0, out t0 );
					x1.Decompose( out s1, out q1, out t1 );

					var q	=	Quaternion.Slerp( q0, q1, factor );
					var t	=	Vector3.Lerp( t0, t1, factor );
					var s	=	Vector3.Lerp( s0, s1, factor );

					var x	=	Matrix.Scaling( s ) * Matrix.RotationQuaternion( q ) * Matrix.Translation( t );

					destination[i] = x;
				}
			}
		}
		#endif
		#endregion


		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		public static AnimationTake Read( BinaryReader reader )
		{
			int nodeCount	=	reader.ReadInt32();
			int firstFrame	=	reader.ReadInt32();
			int lastFrame	=	reader.ReadInt32();
			int dummy		=	reader.ReadInt32();

			var name		=	reader.ReadString();

			var take		=	new AnimationTake( name, nodeCount, firstFrame, lastFrame );

			var data		=	new Matrix[ take.animData.Length ];

			reader.Read<Matrix>( data, take.animData.Length );

			for (int i=0; i<take.animData.Length; i++)
			{
				take.animData[i] = new AnimationKey( data[i] );
			}

			take.ComputeDeltaAnimation();

			return take;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="writer"></param>
		public static void Write( AnimationTake animTake, BinaryWriter writer )
		{
			writer.Write( animTake.NodeCount );
			writer.Write( animTake.FirstFrame );
			writer.Write( animTake.LastFrame );
			writer.Write( (int)0 );

			writer.Write( animTake.Name );

			writer.Write( animTake.animData.Select( key => key.Transform ).ToArray() );
		}
	}
}
