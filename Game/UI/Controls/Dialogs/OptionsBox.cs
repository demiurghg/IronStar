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

namespace IronStar.UI.Controls.Dialogs {
	public static class OptionsBox {
		
		class Options {
			public bool			Fullscreen	{ get; set; }
			public bool			VSync		{ get; set; }
			public QualityLevel Lighting	{ get; set; }
			public QualityLevel Shadows		{ get; set; }
			public bool			Bloom		{ get; set; }
			public QualityLevel	SSAO		{ get; set; }
			public bool			GI			{ get; set; }
			public QualityLevel	Reflection	{ get; set; }

			[AEDisplayName("Field of view")]
			[AEValueRange(60,140,1,1)]
			public float		FOV { get; set; }

			public QualityLevel SSLR		{ get; set; }
			public QualityLevel DOF			{ get; set; }
			public bool			Stereo		{ get; set; }
			public QualityLevel	Particles	{ get; set; }
			public bool			Antialiasing{ get; set; }
			public QualityLevel	MotionBlur	{ get; set; }
		}


		static public void ShowDialog ( Frame owner, object video, object audio, object gameplay, object controls )
		{
			var frames	=	owner.Frames;
			var panel	=	new Panel( frames, 0, 0, 640, 560 );
			var header	=	new Frame( frames );

			panel.Tag		=	frames.ModalFrame;
			panel.AllowDrag	=	true;

			panel.Closed	+=  (s,e) => frames.ModalFrame = panel.Tag as Frame;

			var layout		=	new PageLayout();
			layout.Margin	=	MenuTheme.Margin;
			layout.AddRow( MenuTheme.ElementHeight, new float[] { -1 } );
			layout.AddRow( MenuTheme.ElementHeight, new float[] { -1, -1, -1, -1 } );
			layout.AddRow(						-1, new float[] { -1 } );
			layout.AddRow( MenuTheme.ElementHeight, new float[] { -1, -1 } );

			panel.Layout	=	layout;

			//	Header :

			header.Font			=	MenuTheme.HeaderFont;
			header.Text			=	"OPTIONS";
			header.ForeColor	=	MenuTheme.TextColorNormal;
			header.BackColor	=	MenuTheme.Transparent;
			header.Padding		=	4;

			//	Selector buttons :

			var buttonVideo		=	new Button( owner.Frames, "VIDEO"	, 0,0,0,0, ()=> {} );
			var buttonAudio		=	new Button( owner.Frames, "AUDIO"	, 0,0,0,0, ()=> {} );
			var buttonGameplay	=	new Button( owner.Frames, "GAMEPLAY", 0,0,0,0, ()=> {} );
			var buttonControls	=	new Button( owner.Frames, "CONTROLS", 0,0,0,0, ()=> {} );

			//	Property grid :
			var scrollBox				=	new ScrollBox( owner.Frames, 0,0,0,0 );
			scrollBox.ScrollMarkerSize	=	MenuTheme.ScrollSize;
			scrollBox.ScrollMarkerColor	=	MenuTheme.ScrollMarkerColor;
			scrollBox.MarginTop			=	MenuTheme.Margin;
			scrollBox.MarginBottom		=	MenuTheme.Margin;

			var grid			=	new PropertyGrid( owner.Frames );
			grid.TargetObject	=	new Options();

			//	OK/Cancel buttons :

			var buttonOK		=	new Button( owner.Frames, "OK",		0,0,0,0, ()=> panel.Close() );
			var buttonCancel	=	new Button( owner.Frames, "Cancel", 0,0,0,0, ()=> panel.Close() );

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

			owner.Add( panel );
			panel.CenterFrame();
			frames.ModalFrame = panel;
		}
	}
}
