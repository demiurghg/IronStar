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
using Fusion;

namespace IronStar.UI.Controls.Dialogs {
	public class LevelBox : Panel {

		Frame gallery;

		public LevelBox ( FrameProcessor frames ) : base(frames, 0,0,900-68,600-4)
		{
			var layout		=	new PageLayout();
			layout.Margin	=	MenuTheme.Margin;
			layout.AddRow( MenuTheme.ElementHeight, new float[] { -1 } );
			layout.AddRow(						-1, new float[] { -1 } );
			layout.AddRow( MenuTheme.ElementHeight, new float[] { -1,-1,-1,-1 } );

			this.Layout	=	layout;

			//	Header :

			var header	=	new Frame( frames );

			header.Font			=	MenuTheme.HeaderFont;
			header.Text			=	"START NEW GAME";
			header.ForeColor	=	MenuTheme.TextColorNormal;
			header.BackColor	=	MenuTheme.Transparent;
			header.Padding		=	4;
			header.Ghost		=	true;

			//	Property grid :
		
			gallery				=	new Frame( frames );
			gallery.BackColor	=	MenuTheme.Transparent;

			gallery.Layout		=	new GaleryLayout( 3, 256, 144, MenuTheme.Margin );

			PopulateGallery();

			//	Scrollbox for property grid :

			var scrollBox				=	new ScrollBox( frames, 0,0,0,0 );
			scrollBox.ScrollMarkerSize	=	MenuTheme.ScrollSize;
			scrollBox.ScrollMarkerColor	=	MenuTheme.ScrollMarkerColor;
			scrollBox.MarginTop			=	MenuTheme.Margin;
			scrollBox.MarginBottom		=	MenuTheme.Margin;

			//	OK/Cancel buttons :

			var buttonStart		=	new Button( frames, "Start",		0,0,0,0, StartSelectedLevel );
			var buttonCreate	=	new Button( frames, "Create",		0,0,0,0, ()=> { Log.Warning("Not implemented"); } );
			var buttonEmpty		=	Frame.CreateEmptyFrame( frames );
			var buttonCancel	=	new Button( frames, "Cancel", 0,0,0,0, ()=> { Close(); } );

			//	Construct all :

			this.Add( header );

			this.Add( scrollBox );
				scrollBox.Add( gallery );

			this.Add( buttonCancel );
			this.Add( buttonEmpty );
			this.Add( buttonCreate );
			this.Add( buttonStart );
		}


		void StartSelectedLevel ()
		{
			Close();
			Game.Invoker.ExecuteString("map testMonsters");
		}


		void PopulateGallery ()
		{
			Random rand = new Random();

			for (int i=0; i<12; i++) {
				
				var levelImage			=	new Frame( Frames, 0,0,0,0, "Level 01", Color.Black );
				var imagePath			=	@"ui\levelArt\level0" + rand.Next(1,6).ToString();

				levelImage.Image		=	Frames.Game.Content.Load<DiscTexture>( imagePath );
				levelImage.ImageMode	=	FrameImageMode.Stretched;

				levelImage.Font			=	MenuTheme.SmallFont;
				levelImage.Padding		=	MenuTheme.SmallContentPadding;

				levelImage.TextAlignment	=	Alignment.BottomCenter;

				levelImage.StatusChanged+=LevelImage_StatusChanged;

				gallery.Add( levelImage );
			}
		}

		private void LevelImage_StatusChanged( object sender, StatusEventArgs e )
		{
			var frame = sender as Frame;

			switch ( e.Status ) {
				case FrameStatus.None:		frame.ImageColor	=	Color.Gray;			break;
				case FrameStatus.Hovered:	frame.ImageColor	=	Color.LightGray;	break;
				case FrameStatus.Pushed:	frame.ImageColor	=	Color.White;		break;
			}
		}
	}
}
