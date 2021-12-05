using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Engine.Frames;
using Fusion.Engine.Frames.Layouts;
using IronStar.UI.Controls.Advanced;
using Fusion.Engine.Graphics;
using Fusion.Core.Shell;

namespace IronStar.UI.Controls.Dialogs 
{
	public static class OptionsBox 
	{
		[Obsolete("Use FrameProcessor.ShowDialog")]
		static public void ShowDialog ( Frame owner, object video, object audio, object gameplay, object controls )
		{
			var ui	=	owner.ui;
			var panel	=	new Panel( ui, 0, 0, 640, 560 );
			var header	=	new Frame( ui );

			panel.AllowDrag			=	true;
			panel.Image				=	ui.Game.Content.Load<DiscTexture>(@"ui\options");
			panel.ImageColor		=	MenuTheme.ImageColor;
			panel.ImageMode			=	FrameImageMode.Stretched;

			panel.Layout	=	new PageLayout()
					.Margin( MenuTheme.Margin )
					.AddRow( MenuTheme.ElementHeight, new float[] { -1 } )
					.AddRow( MenuTheme.ElementHeight, new float[] { -1, -1, -1, -1 } )
					.AddRow(						-1, new float[] { -1 } )
					.AddRow( MenuTheme.ElementHeight, new float[] { -1, -1 } )
					;

			//	Header :

			header			=	panel.CreateHeader("OPTIONS");
			
			//	Property grid :
		
			var grid			=	new PropertyGrid( owner.ui );
			grid.TargetObject	=	video;

			//	Selector buttons :

			var buttonVideo		=	new Button( owner.ui, "VIDEO"	, 0,0,0,0, ()=> grid.TargetObject = video );
			var buttonAudio		=	new Button( owner.ui, "AUDIO"	, 0,0,0,0, ()=> grid.TargetObject = audio );
			var buttonGameplay	=	new Button( owner.ui, "GAMEPLAY", 0,0,0,0, ()=> grid.TargetObject = gameplay );
			var buttonControls	=	new Button( owner.ui, "CONTROLS", 0,0,0,0, ()=> grid.TargetObject = controls );

			//	Scrollbox for property grid :

			var scrollBox				=	new ScrollBox( owner.ui, 0,0,0,0 );
			scrollBox.ScrollMarkerSize	=	MenuTheme.ScrollSize;
			scrollBox.ScrollMarkerColor	=	MenuTheme.ScrollMarkerColor;
			scrollBox.MarginTop			=	MenuTheme.Margin;
			scrollBox.MarginBottom		=	MenuTheme.Margin;

			//	OK/Cancel buttons :

			var buttonOK		=	new Button( owner.ui, "OK",		0,0,0,0, null );
			var buttonCancel	=	new Button( owner.ui, "Cancel", 0,0,0,0, null );

			//	Construct all :

			panel.Add( header );

			panel.Add( buttonVideo );
			panel.Add( buttonAudio );
			panel.Add( buttonGameplay );
			panel.Add( buttonControls );

			panel.Add( scrollBox );
				scrollBox.Add( grid );

			panel.Add( buttonOK );
			panel.Add( buttonCancel );

			//	Settle option's box :

			var ctxt = ui.ShowDialogCentered( panel );

			buttonOK.Click		+= (s,e) => { ui.Stack.PopUIContext( ref ctxt ); grid.CommitChanges(); };
			buttonCancel.Click	+= (s,e) => { ui.Stack.PopUIContext( ref ctxt ); };
		}
	}
}
