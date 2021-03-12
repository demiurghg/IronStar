using BEPUphysics.Paths;
using Fusion;
using Fusion.Core.Extensions;
using Fusion.Engine.Frames;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Engine.Frames.Layouts;
using Fusion.Build;
using Fusion.Widgets;
using Fusion.Widgets.Advanced;
using IronStar.Mapping;

namespace IronStar.Editor.Controls 
{
	public class Outliner : Panel 
	{
		Label	label;
		TextBox	filter;
		ListBox listBox;
		Button	btnClose;
		
		public Outliner( Frame parent, int x, int y, int w, int h ) : base(parent.Frames, x,y,w,h)
		{
			var fp			=	parent.Frames;
			AllowDrag		=	true;
			AllowResize		=	true;

			var pageLayout = new PageLayout();
			pageLayout.AddRow(17, /**/ -1.0f );
			pageLayout.AddRow(17, /**/ -1.0f );
			pageLayout.AddRow(-1, /**/ -1.0f );
			pageLayout.AddRow(23, /**/ -1.0f );

			Layout	=	pageLayout;

			label		=	new Label( fp, 0,0,0,0, "Outliner");
			filter		=	new TextBox(fp);
			btnClose	=	new Button(fp, "Close", 0,0,0,0, () => Visible = false );

			listBox		=	new ListBox( fp, new MapNode[0], obj => GetDisplayName((MapNode)obj) );  

			Add( label );
			Add( filter );
			Add( listBox );
			Add( btnClose );
		}


		string GetDisplayName( MapNode node )
		{
			return node.GetType().Name + " : " + node.Name;
		}


		public void SetItems( Map map )
		{
			if (map==null) return;

			listBox.SetItems( map.Nodes );
		}
	}
}
