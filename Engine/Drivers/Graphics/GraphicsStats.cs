using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.Windows;
using SharpDX.DXGI;
using D3D = SharpDX.Direct3D11;
using DXGI = SharpDX.DXGI;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;
using Native.NvApi;
using Device = SharpDX.Direct3D11.Device;
using System.IO;

namespace Fusion.Drivers.Graphics 
{
	public sealed class GraphicsStats 
	{
		public int VertexCount;
		public int DrawCalls;
		public int Dispatches;

		public void Reset()
		{
			VertexCount	=	0;
			DrawCalls	=	0;
			Dispatches	=	0;
		}
	}
}
