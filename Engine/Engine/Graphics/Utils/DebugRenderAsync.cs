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
using System.Collections.Concurrent;

namespace Fusion.Engine.Graphics 
{
	public class DebugRenderAsync : DebugRender
	{
		public List<DebugVertex> writeBuffer = new List<DebugVertex>();
		public List<DebugVertex> readBuffer = new List<DebugVertex>();
		readonly DebugRenderImpl dr;

		readonly object writeLock = new object();
		readonly object readLock = new object();

		ConcurrentQueue<DebugModel>					addQueue		=	new ConcurrentQueue<DebugModel>();
		ConcurrentQueue<DebugModel>					removeQueue		=	new ConcurrentQueue<DebugModel>();
		ConcurrentQueue<Tuple<DebugModel,Matrix>>	transformQueue	=	new ConcurrentQueue<Tuple<DebugModel,Matrix>>();



		public DebugRenderAsync( DebugRenderImpl drImpl ) : base( drImpl.Game )
		{
			dr = drImpl;
		}


		public override void AddModel( DebugModel model )
		{
			if (model!=null)
			{
				addQueue.Enqueue( model );
			}
		}


		public override void RemoveModel( DebugModel model )
		{
			if (model!=null)
			{
				removeQueue.Enqueue( model );
			}
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
			DebugModel model;
			Tuple<DebugModel,Matrix> transform;

			while (addQueue.TryDequeue(out model)) dr.AddModel(model);
			while (removeQueue.TryDequeue(out model)) dr.RemoveModel(model);
			
			while (transformQueue.TryDequeue(out transform)) 
			{
				transform.Item1.World = transform.Item2;
			}

			lock (readLock)
			{
				for ( int i=0; i<readBuffer.Count; i++ )
				{
					dr.PushVertex( readBuffer[i] );
				}
			}
		}


		public void SetTransform( DebugModel model, Matrix world )
		{
			transformQueue.Enqueue( Tuple.Create( model, world ) );
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
