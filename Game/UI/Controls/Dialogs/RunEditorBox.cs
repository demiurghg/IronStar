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
using Fusion.Build;
using System.IO;

namespace IronStar.UI.Controls.Dialogs {
	public class RunEditorBox : Panel {

		Frame gallery;
		Frame buttonStart;

		string selectedMap = null;

		public RunEditorBox ( FrameProcessor frames ) : base(frames, 0,0,900-64,600-4)
		{
			var layout		=	new PageLayout();
			layout.Margin	=	MenuTheme.Margin;
			layout.AddRow( MenuTheme.ElementHeight, new float[] { -1 } );
			layout.AddRow(						-1, new float[] { -1 } );
			layout.AddRow( MenuTheme.ElementHeight, new float[] { -1,-1,-1 } );

			this.Layout	=	layout;

			//	Header :

			var header	=	new Frame( frames );

			header.Font			=	MenuTheme.HeaderFont;
			header.Text			=	"MAP EDITOR";
			header.ForeColor	=	MenuTheme.TextColorNormal;
			header.BackColor	=	MenuTheme.Transparent;
			header.Padding		=	4;
			header.Ghost		=	true;

			//	Property grid :
		
			gallery				=	new Frame( frames );
			gallery.BackColor	=	MenuTheme.Transparent;

			gallery.Layout		=	new GaleryLayout( 192, 108, MenuTheme.Margin );

			PopulateGallery();

			//	Scrollbox for property grid :

			var scrollBox				=	new ScrollBox( frames, 0,0,0,0 );
			scrollBox.ScrollMarkerSize	=	MenuTheme.ScrollSize;
			scrollBox.ScrollMarkerColor	=	MenuTheme.ScrollMarkerColor;
			scrollBox.MarginTop			=	MenuTheme.Margin;
			scrollBox.MarginBottom		=	MenuTheme.Margin;

			//	OK/Cancel buttons :

				buttonStart			=	new Button( frames, "(Select Level)",		0,0,0,0, StartEditor );
				buttonStart.Enabled	=	false;
				buttonStart.OverallColor = new Color(255,255,255,128);
			var buttonCreate	=	new Button( frames, "Create",		0,0,0,0, CreateNew );
			var buttonEmpty		=	Frame.CreateEmptyFrame( frames );
			var buttonCancel	=	new Button( frames, "Cancel", 0,0,0,0, ()=> { Close(); } );

			//	Construct all :

			this.Add( header );

			this.Add( scrollBox );
				scrollBox.Add( gallery );

			this.Add( buttonCancel );
			this.Add( buttonCreate );
			this.Add( buttonStart );
		}



		void StartEditor ()
		{
			Close();
			Game.Invoker.ExecuteString("map " + selectedMap + " /edit");
		}



		void CreateNew ()
		{
			Frames.ShowDialogCentered( new TextBoxDialog( Frames, "New Map", "Enter new map name:", "untitled", AcceptMapName ) );
			//MessageBox.ShowError(this, "Not implemented", "Use console command: map <name> /edit", () => {} );
			//Frames.ShowDialogCentered( new MessageBox(

		}



		bool AcceptMapName( string name )
		{
			var builder = Game.Services.GetService<Builder>();
			var mapDir	=	Path.Combine( builder.GetBaseInputDirectory(), "maps" );

			if ( string.IsNullOrWhiteSpace(name) ) {
				MessageBox.ShowError(this, "New map", "Map name could not be empty", null);
				return false;
			}

			if ( File.Exists( Path.Combine( mapDir, name + ".json" ) ) ) {
				MessageBox.ShowError(this, "New map", "Map '" + name + "' already exists", null);
				return false;
			} 

			Close();
			Game.Invoker.ExecuteString("map " + name + " /edit");
			return true;
		}



		void PopulateGallery ()
		{
			Random rand =	new Random();

			var content			=	Frames.Game.Content;
			var builder			=	Game.Services.GetService<Builder>();
			var mapDir			=	Path.Combine( builder.GetBaseInputDirectory(), "maps" );
			var defaultPreveiw	=	content.Load<DiscTexture>( @"maps\thumbnails\default" );


			foreach ( var fileName in Directory.EnumerateFiles( mapDir, "*.json" ) ) {

				var mapName				=	Path.GetFileNameWithoutExtension(fileName);
				
				var levelImage			=	new Frame( Frames, 0,0,0,0, mapName.ToUpperInvariant(), Color.Black );

				DiscTexture	mapPreview;
				
				if (!content.TryLoad( @"maps\thumbnails\" + mapName, out mapPreview ) ) {
					mapPreview = defaultPreveiw;
				}

				levelImage.Image		=	mapPreview;
				levelImage.ImageMode	=	FrameImageMode.Stretched;

				levelImage.Font			=	MenuTheme.SmallFont;
				levelImage.Padding		=	MenuTheme.SmallContentPadding;
				levelImage.BorderColor	=	MenuTheme.SelectColor;

				levelImage.TextAlignment	=	Alignment.BottomLeft;

				levelImage.StatusChanged	+=	LevelImage_StatusChanged;

				levelImage.Click			+= 	(s,e) => { 
					selectedMap = mapName;
					buttonStart.Enabled = true;
					buttonStart.Text	= "Start";
					buttonStart.OverallColor = Color.White;
				};

				levelImage.Tick += (s,e) => {
					if (mapName==selectedMap) {
						levelImage.Border = 2;
					} else {
						levelImage.Border = 0;
					}
				};

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
