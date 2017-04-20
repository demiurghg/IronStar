using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace Fusion.Engine.Graphics {
	public class Take {

		readonly Quaternion[,] rotationData;
		readonly Vector3[,] positionData;
		readonly Vector3[,] scalingData;
		readonly string[] nodeNames;


		public readonly TimeSpan Start;

		public readonly TimeSpan Stop;

		public readonly int FramesPerSecond;

		public readonly int NodeCount;


		public Take ( Scene scene )
		{
			
		}
		

		public bool Evaluate ( TimeSpan location, Vector3[] positions, Vector3[] scaling, Quaternion[] rotations )
		{
			throw new NotImplementedException();
		}
	}
}
