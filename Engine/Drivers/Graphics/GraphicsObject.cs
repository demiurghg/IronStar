using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct3D;
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
using Fusion.Core;
using Fusion.Drivers.Graphics;
using Fusion.Drivers.Graphics.Display;
using Fusion.Core.Mathematics;
using System.Threading;
using System.Diagnostics;

namespace Fusion.Drivers.Graphics {

	public class GraphicsObject : DisposableBase {

		static volatile int trackCounter = 0;
		static readonly HashSet<GraphicsObject> resourceTracker = new HashSet<GraphicsObject>();

		public object Owner { get; set; }

		public static void ReportLiveObjects ()
		{
			if (resourceTracker.Any()) {

				Log.Warning("");
				Log.Warning("Live graphics objects detected!");
				Log.Warning("----------------------------------------------------");

				var list =	resourceTracker
							.OrderBy( obj1 => obj1.creationOrder )
							.ToList();

				foreach ( var obj in list ) {
					Log.Warning("[{0}] : {1}\r\n{2}", obj.creationOrder, obj.GetType().Name, obj.creationStackTrace );
				}

				Log.Warning("----------------------------------------------------");
				Log.Warning("{0} live objects\r\n", resourceTracker.Count);
				
			} else {
				Log.Message("No live graphics resources detected.");
			}
		}



		/// <summary>
		/// Gets the GraphicsDevice associated with this GraphicsResource.
		/// </summary>
		public GraphicsDevice GraphicsDevice {
			get {
				return device;
			}
		}				


		protected readonly GraphicsDevice device;
		private readonly string creationStackTrace;
		private readonly int creationOrder;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="device"></param>
		public GraphicsObject ( GraphicsDevice device )
		{
			this.device	=	device;

			var stackTrace = new StackTrace(1, true);

			var stackTraceText = 
					string.Join("",	
					 stackTrace
					.GetFrames()
					.Select( sf => string.Format("  {0,40} -- {1,40}({2})\r\n", 
						sf.GetMethod().DeclaringType.Name + "." + sf.GetMethod().Name, 
						Path.GetFileName( sf.GetFileName() ), 
						sf.GetFileLineNumber() )	
						)
					);

			this.creationStackTrace = stackTraceText;
			this.creationOrder		= Interlocked.Increment( ref trackCounter );
			resourceTracker.Add(this);
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose ( bool disposing )
		{
			resourceTracker.Remove(this);
			base.Dispose( disposing );
		}
	}
}
