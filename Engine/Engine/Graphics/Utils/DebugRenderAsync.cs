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
using Fusion.Core.Extensions;

namespace Fusion.Engine.Graphics 
{
	public class DebugRenderAsync : DebugRender
	{
		public List<DebugVertex> writeBuffer = new List<DebugVertex>();
		public List<DebugVertex> readBuffer = new List<DebugVertex>();
		readonly DebugRenderImpl dr;

		readonly object writeLock = new object();
		readonly object readLock = new object();


		public DebugRenderAsync( DebugRenderImpl drImpl )
		{
			dr = drImpl;
		}


		public override void AddModel( DebugModel model )
		{
			Log.Warning("DebugRenderAsync does not support debug models");
		}


		public override void RemoveModel( DebugModel model )
		{
			Log.Warning("DebugRenderAsync does not support debug models");
		}


		public override void Submit()
		{
			lock (readLock)
			{
				lock (writeLock)
				{
					Misc.Swap( ref writeBuffer, ref readBuffer );
					writeBuffer.Clear();
				}
			}
		}


		public void Render()
		{
			lock (readLock)
			{
				for ( int i=0; i<readBuffer.Count; i++ )
				{
					dr.PushVertex( readBuffer[i] );
				}
			}
		}


		public override void PushVertex( DebugVertex v )
		{
			lock (writeLock)
			{
				writeBuffer.Add( v );
			}
		}
	}
}
