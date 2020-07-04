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
			var frames	=	owner.Frames;
			var panel	=	new Panel( frames, 0, 0, 640, 560 );
			var header	=	new Frame( frames );

			panel.AllowDrag			=	true;
			panel.Image				=	frames.Game.Content.Load<DiscTexture>(@"ui\options");
			panel.ImageColor		=	MenuTheme.ImageColor;
			panel.ImageMode			=	FrameImageMode.Stretched;

			var layout		=	new PageLayout();
			layout.Margin	=	MenuTheme.Margin;
			layout.AddRow( MenuTheme.ElementHeight, new float[] { -1 } );
			layout.AddRow( MenuTheme.ElementHeight, new float[] { -1, -1, -1, -1 } );
			layout.AddRow(						-1, new float[] { -1 } );
			layout.AddRow( MenuTheme.ElementHeight, new float[] { -1, -1 } );

			panel.Layout	=	layout;

			//	Header :

			header			=	panel.CreateHeader("OPTIONS");
			
			//	Property grid :
		
			var grid			=	new PropertyGrid( owner.Frames );
			grid.TargetObject	=	video;

			//	Selector buttons :

			var buttonVideo		=	new Button( owner.Frames, "VIDEO"	, 0,0,0,0, ()=> grid.TargetObject = video );
			var buttonAudio		=	new Button( owner.Frames, "AUDIO"	, 0,0,0,0, ()=> grid.TargetObject = audio );
			var buttonGameplay	=	new Button( owner.Frames, "GAMEPLAY", 0,0,0,0, ()=> grid.TargetObject = gameplay );
			var buttonControls	=	new Button( owner.Frames, "CONTROLS", 0,0,0,0, ()=> grid.TargetObject = controls );

			//	Scrollbox for property grid :

			var scrollBox				=	new ScrollBox( owner.Frames, 0,0,0,0 );
			scrollBox.ScrollMarkerSize	=	MenuTheme.ScrollSize;
			scrollBox.ScrollMarkerColor	=	MenuTheme.ScrollMarkerColor;
			scrollBox.MarginTop			=	MenuTheme.Margin;
			scrollBox.MarginBottom		=	MenuTheme.Margin;

			//	OK/Cancel buttons :

			var buttonOK		=	new Button( owner.Frames, "OK",		0,0,0,0, null );
			var buttonCancel	=	new Button( owner.Frames, "Cancel", 0,0,0,0, null );

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

			var ctxt = frames.ShowDialogCentered( panel );

			buttonOK.Click		+= (s,e) => { frames.Stack.PopUIContext( ref ctxt ); grid.CommitChanges(); };
			buttonCancel.Click	+= (s,e) => { frames.Stack.PopUIContext( ref ctxt ); };
		}
	}
}
