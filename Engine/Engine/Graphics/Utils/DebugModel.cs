using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Core;
using Fusion.Drivers.Graphics;
using Fusion.Engine.Common;
using System.Runtime.InteropServices;
using Fusion.Engine.Graphics.Ubershaders;

namespace Fusion.Engine.Graphics 
{
	public class DebugModel : DisposableBase 
	{
		IndexBuffer ib;
		VertexBuffer vb;

		public object Tag;

		public IndexBuffer Indices { get { return ib; } }
		public VertexBuffer Vertices { get { return vb; } }

		public Matrix World { get; set; } = Matrix.Identity;
		public Color Color { get; set; } = Color.White;

		readonly int vertexCount;
		readonly int indexCount;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="dr"></param>
		/// <param name="vertices"></param>
		/// <param name="indices"></param>
		public DebugModel ( Game game, Vector3[] vertices, int[] indices )
		{
			ib	=	new IndexBuffer( game.GraphicsDevice, indices.Length );
			vb	=	new VertexBuffer( game.GraphicsDevice, typeof(DebugVertex), vertices.Length, VertexBufferOptions.Dynamic );

			ib.SetData( indices );
			vb.SetData( vertices.Select( v => new DebugVertex(v) ).ToArray() );

			vertexCount	=	vertices.Length;
			indexCount	=	indices.Length;
		}


		internal void Draw ( GraphicsDevice device )
		{
			device.SetupVertexInput( vb, ib );

			device.DrawIndexed( indexCount, 0, 0 );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose( bool disposing )
		{
			if (disposing) 
			{
				SafeDispose( ref ib );
				SafeDispose( ref vb );
			}

			base.Dispose( disposing );
		}

	}
}
