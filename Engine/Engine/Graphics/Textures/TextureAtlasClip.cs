#define DIRECTX
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
using Fusion.Core.Content;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Drivers.Graphics;
using Fusion.Engine.Common;


namespace Fusion.Engine.Graphics {

	/// <summary>
	/// Represents texture atlas.
	/// </summary>
	public class TextureAtlasClip {

		public readonly string Name;
		public readonly int FirstIndex;
		public readonly int Length;
		
		public TextureAtlasClip ( string name, int first, int length )
		{
			this.Name			=	name;
			this.FirstIndex		=	first;
			this.Length			=	length;
		}
	}
}
