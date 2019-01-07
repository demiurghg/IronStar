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

	public class FrameStack {

		readonly FrameProcessor frames;
		Stack<FrameStackEntry> modalFrames = new Stack<FrameStackEntry>();


		/// <summary>
		/// Creates instance of frame stack
		/// </summary>
		public FrameStack( FrameProcessor frames )
		{
			this.frames	=	frames;

			modalFrames.Push( new FrameStackEntry( frames.RootFrame, frames.RootFrame ) );
		}



		public Frame GetModalFrame ()
		{
			return modalFrames.Peek().Modal;
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
		public void PushModalFrame ( Frame modalFrame, Frame targetFrame, Frame ownerFrame )
		{
			if (modalFrame.Parent!=null) {
				throw new InvalidOperationException("PushModalFrame : frame must not be added to another frame");
			}

			(ownerFrame ?? frames.RootFrame).Add( modalFrame );

			modalFrames.Peek().Modal?.OnDeactivate();
			modalFrames.Push( new FrameStackEntry( modalFrame, targetFrame ) );
			modalFrames.Peek().Modal?.OnActivate();
		}


		/// <summary>
		/// Pop given modal frame from stack
		/// </summary>
		/// <param name="modalFrame"></param>
		public void PopModalFrame ( Frame modalFrame )
		{
			if (modalFrames.Peek().Modal!=modalFrame) {
				throw new InvalidOperationException("PopModalFrame : can not pop not top-level modal frame");
			}

			modalFrames.Peek().Modal?.OnDeactivate();
			modalFrames.Pop().Modal?.Close();
			modalFrames.Peek().Modal?.OnActivate();
		}


		/// <summary>
		/// Checks whether given frame is presented in stack of modal frames.
		/// </summary>
		/// <param name="frame"></param>
		/// <returns></returns>
		public bool IsModalFrame ( Frame frame )
		{
			return modalFrames.Any( sf => sf.Modal == frame );
		}


		/// <summary>
		/// Checks whether given frame is on top of stack of modal frames.
		/// </summary>
		/// <param name="frame"></param>
		/// <returns></returns>
		public bool IsTopLevelModalFrame ( Frame frame )
		{
			return (frame == modalFrames.Peek().Modal);
		}


#if false
		public	Frame TargetFrame { 
			get { 
				return targetFrame;
			}
			set {				
				SetTargetFrame( value );
			}
		}
		
		Frame targetFrame = null;
		Frame activeFrame = null;

		void SetTargetFrame ( Frame newTargetFrame )
		{
			if (newTargetFrame==RootFrame) {
				newTargetFrame = null;
			}

			if (targetFrame!=newTargetFrame) {

				var oldActivationTracker	=	GetActivationTracker( targetFrame );
				var newActivationTracker	=	GetActivationTracker( newTargetFrame );

				targetFrame?.OnLeave();

				if (oldActivationTracker!=newActivationTracker) {
					oldActivationTracker?.OnDeactivate();
					newActivationTracker?.OnActivate();
				}

				newTargetFrame?.OnEnter();

				targetFrame	=	newTargetFrame;
			}
		}


		Frame GetActivationTracker ( Frame frame )
		{
			while (frame!=null) {
				if (frame.TrackActivation) {
					return frame;
				} else {
					frame = frame.Parent;
				}
			}
			return null;
		}
#endif
	}
}
