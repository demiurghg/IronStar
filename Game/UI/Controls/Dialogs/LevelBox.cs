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
using System.IO;

namespace IronStar.UI.Controls.Dialogs {
	public class LevelBox : Panel {

		Frame gallery;

		string selectedMap = null;
		Frame buttonStart;

		public class MapAcceptEventArgs : EventArgs
		{
			public MapAcceptEventArgs(string map) { MapName=map; }
			public readonly string MapName;
		}

		public event EventHandler Reject;
		public event EventHandler<MapAcceptEventArgs> Accept;


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

				buttonStart			=	new Button( frames, "Start",		0,0,0,0, ()=>Accept?.Invoke( this, new MapAcceptEventArgs(selectedMap)) );
				buttonStart.Enabled	=	false;
				buttonStart.OverallColor = new Color(255,255,255,128);

			var buttonCancel		=	new Button( frames, "Cancel", 0,0,0,0, ()=>Reject?.Invoke( this, EventArgs.Empty ) );

			//	Construct all :

			this.Add( header );

			this.Add( scrollBox );
				scrollBox.Add( gallery );

			this.Add( buttonCancel );
			this.Add( CreateEmptyFrame( frames ) );
			this.Add( CreateEmptyFrame( frames ) );
			this.Add( buttonStart );
		}


		static public void Show ( FrameProcessor fp )
		{
			var box		=	new LevelBox(fp);
			var ctxt	=	fp.ShowDialogCentered( box );
			
			box.Accept += (s,e) => 
			{
				fp.Stack.PopUIContext( ref ctxt );
				fp.Game.Invoker.ExecuteString("map " + e.MapName);
			};
			
			box.Reject += (s,e) => 
			{
				fp.Stack.PopUIContext( ref ctxt );
			};
		}


		void StartSelectedLevel ()
		{
			Close();
			Game.Invoker.ExecuteString("map " + selectedMap);
		}



		void PopulateGallery ()
		{
			Random rand =	new Random();

			var content			=	Frames.Game.Content;
			var defaultPreveiw	=	content.Load<DiscTexture>( @"maps\thumbnails\default" );


			foreach ( var fileName in content.EnumerateAssets("maps") ) {

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
