using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Core.Configuration;
using Fusion.Core.Extensions;


namespace Fusion.Engine.Graphics {

	public sealed class VTSegment {

		public static readonly VTSegment Empty = new VTSegment();

		private VTSegment () 
		{
			MaxMipLevel = 0;
			Region = new RectangleF(0,0,0,0);
			AverageColor = Color.Gray;
			Transparent = false;
		}

		public VTSegment ( string name, int x, int y, int w, int h, Color color, bool transparent ) 
		{
			Name		=	name;
			var fx		=   x / (float)VTConfig.TextureSize;
			var fy		=   y / (float)VTConfig.TextureSize;
			var fw		=   w / (float)VTConfig.TextureSize;
			var fh		=   h / (float)VTConfig.TextureSize;
			Region		=	new RectangleF( fx, fy, fw, fh );
			MaxMipLevel	=	MathUtil.LogBase2( w / VTConfig.PageSize );
			Transparent	=	transparent;
			AverageColor=	color;
		}

		public readonly string Name;
		public readonly RectangleF Region;
		public readonly int MaxMipLevel;
		public bool Transparent;
		public Color AverageColor;
	}

}
