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

		/// <summary>
		/// Gets take name
		/// </summary>
		public string Name { get { return name; } }

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
		public AnimationTake ( string name, int nodeCount, int frameCount )
		{
			this.name		=	name;
			this.nodeCount	=	nodeCount;

			this.frameCount	=	frameCount;

			this.animData	=	new AnimationKey[ nodeCount * frameCount ];
			this.animDelta	=	new AnimationKey[ nodeCount * frameCount ];

			for ( int i=0; i<animData.Length; i++)
			{
				animData[i]		=	AnimationKey.Identity;
				animDelta[i]	=	AnimationKey.Identity;
			}
		}


		public int Clamp( int frame )
		{
			return MathUtil.Clamp( frame, 0, FrameCount-1 );
		}


		public int Wrap( int frame )
		{
			return MathUtil.Wrap( frame, 0, FrameCount-1 );
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
				case AnimationWrapMode.Clamp:	frame = MathUtil.Clamp( frame, 0, frameCount-1 ); break;
				case AnimationWrapMode.Repeat:	frame = MathUtil.Wrap ( frame, 0, frameCount ); break;
				default: throw new ArgumentException("mode");
			}
				
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
			if (frame<0) { throw new ArgumentOutOfRangeException("frame < 0");	}
			if (frame>frameCount) {	throw new ArgumentOutOfRangeException("frame < FrameCount");	}
			if (node<0) { throw new ArgumentOutOfRangeException("node < 0"); }
			if (node>=NodeCount) {	throw new ArgumentOutOfRangeException("node >= NodeCount");		}

			animData[ Address( frame, node ) ] = new AnimationKey( transform );

		}


		public void SetKey ( int frame, int node, AnimationKey key )
		{
			if (frame<0) { throw new ArgumentOutOfRangeException("frame < 0");	}
			if (frame>frameCount) {	throw new ArgumentOutOfRangeException("frame < FrameCount");	}
			if (node<0) { throw new ArgumentOutOfRangeException("node < 0"); }
			if (node>=NodeCount) {	throw new ArgumentOutOfRangeException("node >= NodeCount");		}

			animData[ Address( frame, node ) ] = key;

		}


		public AnimationKey[] GetPose( int frame )
		{
			var pose = new AnimationKey[NodeCount];
			var key  = new AnimationKey();

			for (int i=0; i<NodeCount; i++)
			{
				GetKey( frame, i, ref key );
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
				if (blendMode==AnimationBlendMode.Override)	GetKey( frame, i, ref key ); else
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
			if (frame<0) { throw new ArgumentOutOfRangeException("frame < 0");	}
			if (frame>frameCount) {	throw new ArgumentOutOfRangeException("frame < FrameCount");	}
			if (node<0) { throw new ArgumentOutOfRangeException("node < 0"); }
			if (node>=NodeCount) {	throw new ArgumentOutOfRangeException("node >= NodeCount");		}

			transform  = animData[ Address( frame, node ) ].Transform;
		}


		/// <summary>
		/// Sets anim key
		/// </summary>
		/// <param name="frame"></param>
		/// <param name="node"></param>
		/// <param name="transform"></param>
		public void GetKey ( int frame, int node, ref AnimationKey key )
		{
			if (frame<0) { throw new ArgumentOutOfRangeException("frame < 0");	}
			if (frame>frameCount) {	throw new ArgumentOutOfRangeException("frame < FrameCount");	}
			if (node<0) { throw new ArgumentOutOfRangeException("node < 0"); }
			if (node>=NodeCount) {	throw new ArgumentOutOfRangeException("node >= NodeCount");		}

			key  = animData[ Address( frame, node ) ];
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
			if (frame<0) { throw new ArgumentOutOfRangeException("frame < 0");	}
			if (frame>frameCount) {	throw new ArgumentOutOfRangeException("frame < FrameCount");	}
			if (node<0) { throw new ArgumentOutOfRangeException("node < 0"); }
			if (node>=NodeCount) {	throw new ArgumentOutOfRangeException("node >= NodeCount");		}

			key  = animDelta[ Address( frame, node ) ];
		}


		/// <summary>
		/// 
		/// </summary>
		public void ComputeDeltaAnimation ()
		{
			var initialPose = new Matrix[ NodeCount ];
			Evaluate( 0, AnimationWrapMode.Clamp, initialPose );

			for ( int i=0; i < NodeCount; i++ ) 
			{
				initialPose[i]	=	Matrix.Invert( initialPose[i] );
			}

			for ( int frame = 0; frame < frameCount; frame++ ) 
			{
				for ( int node = 0; node<NodeCount; node++ ) 
				{
					int addr = Address( frame, node );
					animDelta[ addr ] = new AnimationKey( initialPose[node] * animData[ addr ].Transform );
				}
			}
		}


		public void RecordTake( int node, Func<int,float,Matrix> action )
		{
			float d = 1.0f / (frameCount - 1);

			for (int frame=0; frame < frameCount; frame++)
			{
				SetKey( frame, node, action(frame, frame*d) );
			}

			ComputeDeltaAnimation();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		public static AnimationTake Read( BinaryReader reader )
		{
			int nodeCount	=	reader.ReadInt32();
			int frameCount	=	reader.ReadInt32();

			var name		=	reader.ReadString();

			var take		=	new AnimationTake( name, nodeCount, frameCount );

			var data		=	new Matrix[ take.animData.Length ];

			reader.Read( data, take.animData.Length );

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
			writer.Write( animTake.FrameCount );

			writer.Write( animTake.Name );

			writer.Write( animTake.animData.Select( key => key.Transform ).ToArray() );
		}
	}
}
