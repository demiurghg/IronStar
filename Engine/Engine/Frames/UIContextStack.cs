using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using Fusion;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Drivers.Input;
using Fusion.Drivers.Graphics;
using System.Diagnostics;
using Fusion.Core.Configuration;
using Fusion.Engine.Common;
using Fusion.Engine.Graphics;
using System.IO;
using Fusion.Core.Extensions;

namespace Fusion.Engine.Frames {

	public class UIContextStack {

		readonly FrameProcessor frames;
		Stack<UIContext> modalFrames = new Stack<UIContext>();


		/// <summary>
		/// Creates instance of frame stack
		/// </summary>
		public UIContextStack( FrameProcessor frames )
		{
			this.frames	=	frames;

			modalFrames.Push( new UIContext( frames.RootFrame, frames.RootFrame ) );
		}



		public Frame GetRootFrame ()
		{
			return modalFrames.Peek().Root;
		}


		public Frame GetTargetFrame ()
		{
			return modalFrames.Peek().Target;
		}


		/// <summary>
		/// Sets target frame
		/// </summary>
		/// <param name="frame"></param>
		public void SetTargetFrame ( Frame frame )
		{
			var oldTargetFrame = modalFrames.Peek().Target;
			var newTargetFrame = frame;

			modalFrames.Peek().Target	=	newTargetFrame;
		}


		/// <summary>
		/// Pushes modal frame onto stack
		/// </summary>
		/// <param name="modalFrame"></param>
		/// <param name="targetFrame"></param>
		/// <param name="ownerFrame"></param>
		public void PushUIContext ( Frame modalFrame, Frame targetFrame, Frame ownerFrame )
		{
			if (modalFrame.Parent!=null) {
				throw new InvalidOperationException("PushModalFrame : frame must not be added to another frame");
			}

			(ownerFrame ?? frames.RootFrame).Add( modalFrame );

			modalFrames.Peek().Root?.OnDeactivate();
			modalFrames.Push( new UIContext( modalFrame, targetFrame ) );
			modalFrames.Peek().Root?.OnActivate();
		}


		/// <summary>
		/// Pop given modal frame from stack
		/// </summary>
		/// <param name="modalFrame"></param>
		public void PopUIContext ( Frame modalFrame )
		{
			if (modalFrames.Peek().Root!=modalFrame) {
				throw new InvalidOperationException("PopModalFrame : can not pop not top-level modal frame");
			}

			modalFrames.Peek().Root?.OnDeactivate();
			modalFrames.Pop().Root?.Close();
			modalFrames.Peek().Root?.OnActivate();
		}


		/// <summary>
		/// Checks whether given frame is presented in stack of modal frames.
		/// </summary>
		/// <param name="frame"></param>
		/// <returns></returns>
		internal bool IsModalFrame ( Frame frame )
		{
			return modalFrames.Any( sf => sf.Root == frame );
		}


		/// <summary>
		/// Checks whether given frame is on top of stack of modal frames.
		/// </summary>
		/// <param name="frame"></param>
		/// <returns></returns>
		public bool IsTopLevelModalFrame ( Frame frame )
		{
			return (frame == modalFrames.Peek().Root);
		}
	}
}
