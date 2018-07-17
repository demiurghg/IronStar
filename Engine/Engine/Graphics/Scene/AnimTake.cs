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

namespace Fusion.Engine.Graphics {

	public class AnimTake {

		readonly string		name;

		readonly Matrix[]	animData;
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
		public AnimTake ( string name, int nodeCount, int firstFrame, int lastFrame )
		{
			this.name		=	name;
			this.nodeCount	=	nodeCount;

			this.firstFrame	=	firstFrame;
			this.lastFrame	=	lastFrame;
			this.frameCount	=	lastFrame - firstFrame + 1;

			this.animData	=	new Matrix[ nodeCount * frameCount ];
		}



		/// <summary>
		/// Evaluate animation for given take
		/// </summary>
		/// <param name="frame"></param>
		/// <param name="mode"></param>
		public void Evaluate ( int frame, AnimationMode mode, Matrix[] destination )
		{
			if (destination==null) {
				throw new ArgumentNullException("destination");
			}

			if (destination.Length<nodeCount) {
				throw new ArgumentOutOfRangeException("destination.Length");
			}

			switch (mode) {
				case AnimationMode.Clamp:	frame = MathUtil.Clamp( frame, FirstFrame, LastFrame ); break;
				case AnimationMode.Repeat:	frame = MathUtil.Wrap ( frame, FirstFrame, LastFrame ); break;
				default: throw new ArgumentException("mode");
			}
				
			frame = frame - FirstFrame;

			for (int i=0; i<nodeCount; i++) {
				destination[i] = animData[ Address( frame, i ) ];
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

			animData[ Address( frame - firstFrame, node ) ] = transform;

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
		public void GetAnimSnapshot ( float frame, int firstFrame, int lastFrame, AnimationMode animMode, Matrix[] destination )
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

			if (animMode==AnimationMode.Repeat) {
				frame0	=	MathUtil.Wrap( frame0, firstFrame, lastFrame );
				frame1	=	MathUtil.Wrap( frame1, firstFrame, lastFrame );
			} else if (animMode==AnimationMode.Clamp) {
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
		public static AnimTake Read( BinaryReader reader )
		{
			int nodeCount	=	reader.ReadInt32();
			int firstFrame	=	reader.ReadInt32();
			int lastFrame	=	reader.ReadInt32();
			int dummy		=	reader.ReadInt32();

			var name		=	reader.ReadString();

			var take		=	new AnimTake( name, nodeCount, firstFrame, lastFrame );

			reader.Read<Matrix>( take.animData, take.animData.Length );

			return take;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="writer"></param>
		public static void Write( AnimTake animTake, BinaryWriter writer )
		{
			writer.Write( animTake.NodeCount );
			writer.Write( animTake.FirstFrame );
			writer.Write( animTake.LastFrame );
			writer.Write( (int)0 );

			writer.Write( animTake.Name );

			writer.Write( animTake.animData );
		}
	}
}
