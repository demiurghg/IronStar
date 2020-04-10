﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Core;
using Fusion.Drivers.Graphics;
using Fusion.Engine.Common;

namespace Fusion.Engine.Graphics {
	public class LightProbe {

		public readonly Guid Guid;

		/// <summary>
		/// Environment light position
		/// </summary>
		public Matrix	ProbeMatrix { get; set; }

		public float	NormalizedWidth { get; set; }
		public float	NormalizedHeight { get; set; }
		public float	NormalizedDepth { get; set; }

		/// <summary>
		/// Unique image index in range [0..255]
		/// </summary>
		public int		ImageIndex = -1;


		internal bool	Visible = true;
		internal Int3	MinExtent;
		internal Int3	MaxExtent;

		/// <summary>
		/// Bounding box for sorting
		/// </summary>
		public BoundingBox BoundingBox;


		/// <summary>
		/// Creates instance of EnvLight
		/// </summary>
		public LightProbe ()
		{
		}



		/// <summary>
		/// Creates instance of EnvLight
		/// </summary>
		/// <param name="position"></param>
		/// <param name="innerRadius"></param>
		/// <param name="outerRadius"></param>
		public LightProbe ( Guid guid, int imageIndex )
		{
			this.Guid	=	guid;
			ImageIndex	=	imageIndex;
		}
		
	}
}