using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Extensions;
using Fusion.Core.Mathematics;
using Fusion.Drivers.Graphics;
using Fusion.Engine.Frames;
using Fusion.Engine.Graphics.Ubershaders;
using Fusion.Engine.Graphics.Scenes;
using System.Runtime.InteropServices;
using Fusion.Build.Mapping;
using Fusion.Core.Collection;

namespace Fusion.Engine.Graphics.GUI
{
	[RequireShader("gui", true)]
	public partial class GuiRenderer : RenderComponent
	{
		LRUImageCache<Gui>	guiCache;
		ConcurrentPriorityQueue<int,Gui> renderQueue;


		void AllocateGUIsAndEnqueue()
		{
			foreach ( var gui in guis )
			{
				if (gui.Visible)
				{
					var size = Math.Max( gui.Root.Width, gui.Root.Height ) >> gui.Lod;

					guiCache.T
				}
			}
		}


		public void AllocGUI( Gui gui )
		{
			bool isLodChanged;
			var size2 = gui.ComputeLodSize();
			var size  = Math.Max( size2.Width, size2.Height );

			if ( IsGuiAllocated(gui, out isLodChanged) )
			{
				if (isLodChanged)
				{
					guiCache.Remove( gui.AtlasRegion );
					var region = guiCache.Add( size, gui );
					gui.AtlasRegion = region;;
				}
			}
			else
			{
				var region = guiCache.Add( size, gui );
				gui.AtlasRegion = region;
			}
		}


		bool IsGuiAllocated( Gui gui, out bool isLodChanged )
		{
			bool isAllocated	=	false;
			isLodChanged		=	false;

			if (!gui.AtlasRegion.IsEmpty)
			{
				Gui inCacheGUI;

				if (guiCache.TryGet( gui.AtlasRegion, out inCacheGUI ))
				{
					if (inCacheGUI==gui)
					{
						isAllocated = true;
					}
				}
			}

			if (isAllocated)
			{	
				isLodChanged = gui.ComputeLodSize() != gui.AtlasRegion.Size;
			}

			return isAllocated;
		}
	}
}
