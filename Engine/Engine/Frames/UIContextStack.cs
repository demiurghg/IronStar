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

namespace Fusion.Engine.Frames 
{
	public class UIContextStack 
	{
		readonly UIState ui;
		Stack<UIContext> modalFrames = new Stack<UIContext>();


		/// <summary>
		/// Creates instance of frame stack
		/// </summary>
		public UIContextStack( UIState ui, Frame rootFrame )
		{
			this.ui	=	ui;

			modalFrames.Push( new UIContext( rootFrame, rootFrame ) );
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
			modalFrames.Peek().Target = frame;
			
			/*var newTargetFrame = frame;

			var oldTargetFrame = modalFrames.Peek().Target;
			oldTargetFrame?.OnDeactivate();

			modalFrames.Peek().Target	=	newTargetFrame;
			newTargetFrame?.OnActivate();*/
		}


		/// <summary>
		/// Pushes modal frame onto stack
		/// </summary>
		/// <param name="modalFrame"></param>
		/// <param name="targetFrame"></param>
		/// <param name="ownerFrame"></param>
		//	#TODO #UI -- PushUIContext must return UIContext object, the only object to close context
		public UIContext PushUIContext ( Frame modalFrame, Frame targetFrame, Frame ownerFrame )
		{
			if (modalFrame.Parent!=null) {
				throw new InvalidOperationException("PushModalFrame : frame must not be added to another frame");
			}

			(ownerFrame ?? ui.RootFrame).Add( modalFrame );

			//	deactivate top context's frames :
			CallContextDeactivation( modalFrames.Peek().Root );

			//	create and push new context
			var context = new UIContext( modalFrame, targetFrame );
			modalFrames.Push( context );

			//	activate top context's frames :
			CallContextActivation( modalFrames.Peek().Root );

			return context;
		}


		/// <summary>
		/// Pushes modal frame onto stack
		/// </summary>
		/// <param name="modalFrame"></param>
		/// <param name="targetFrame"></param>
		/// <param name="ownerFrame"></param>
		public UIContext PushUIContext ( Frame modalFrame )
		{
			return PushUIContext( modalFrame, modalFrame, null );
		}


		/// <summary>
		/// Pop given modal frame from stack
		/// </summary>
		/// <param name="modalFrame"></param>
		public bool PopUIContext ( ref UIContext uiContext )
		{
			if (modalFrames.Any() && modalFrames.Peek()==uiContext)
			{
				CallContextDeactivation( modalFrames.Peek().Root );

				modalFrames.Pop().Root?.Close();

				CallContextActivation( modalFrames.Peek().Root );

				uiContext = null;

				return true;
			}

			Log.Warning("PopUIContext: Attempt to close non top-level UIContext");
			return false;
		}


		bool PopUIContext( UIContext uiContext )
		{
			return PopUIContext( ref uiContext );
		}


		void CallContextActivation(Frame root)
		{
			if (root!=null)
			{
				Frame.BFSForeach(root, (f) => f.OnContextActivate() );
			}
		}


		void CallContextDeactivation(Frame root)
		{
			if (root!=null)
			{
				Frame.BFSForeach(root, (f) => f.OnContextDeactivate() );
			}
		}


		internal bool UnwindAllUIContextsUpToModalFrameInclusive ( Frame modalFrame )
		{
			if (IsModalFrame(modalFrame)) 
			{
				while (true)
				{
					var top = modalFrames.Peek();
					PopUIContext(top);

					if (top.Root==modalFrame) break;
				}

				return true;
			} 
			else
			{
				return false;
			}
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
