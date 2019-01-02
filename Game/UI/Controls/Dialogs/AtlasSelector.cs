using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Engine.Common;
using Fusion.Engine.Frames;
using Fusion.Engine.Graphics;
using System.Reflection;
using Fusion.Core.Mathematics;
using Fusion;
using Fusion.Core.Input;
using System.IO;
using Fusion.Build;
using Fusion.Core.Content;
using Fusion.Engine.Frames.Layouts;
using IronStar.UI.Controls;
using Fusion.Core;

namespace IronStar.UI.Controls.Dialogs {

	static public class AtlasSelector {

		const int DialogWidth	= 640 + 4 + 4 + 4;
		const int DialogHeight	= 480 + 2 + 2 + 14 + 2 + 20 + 2;

		static public void ShowDialog ( FrameProcessor fp, string atlasName, string oldImageName, Action<string> setImageName )
		{
			var atlasSelector	=	new AtlasSelectorFrame( fp, atlasName, oldImageName, setImageName );

			fp.RootFrame.Add( atlasSelector );
			fp.ModalFrame = atlasSelector;

			atlasSelector.CenterFrame();
		}


		class AtlasSelectorFrame : Panel {

			readonly Action<string> setImageName;
			readonly string		oldImageName;

			GaleryLayout	galery;
			Label		labelDir;
			Label		labelStatus;
			TextBox		filterBox;
			ScrollBox	scrollBox;
			Button		buttonAccept;
			Button		buttonClose;
			Button		buttonZoomIn;
			Button		buttonZoomOut;
			Frame		imageList;


			public AtlasSelectorFrame ( FrameProcessor fp, string atlasName, string oldImageName, Action<string> setImageName ) 
			: base ( fp, 0,0, DialogWidth, DialogHeight )
			{
				this.oldImageName	=	oldImageName;
				this.setImageName	=	setImageName;

				AllowDrag		=	true;

				var pageLayout	=	new PageLayout();

				pageLayout.AddRow(  17, new[] { -1f } );
				pageLayout.AddRow(  17, new[] { -1f } );
				pageLayout.AddRow( -1f, new[] { -1f } );
				pageLayout.AddRow(  25, new[] { -1f, -1f, -1f, -1f } );
				pageLayout.AddRow(	17, new[] { -1f } );

				Layout			=	pageLayout;


				labelDir		=	new Label( fp, 0,0,0,0, "Atlas: " + atlasName ) { TextAlignment = Alignment.MiddleLeft };
				labelStatus		=	new Label( fp, 0,0,0,0, "....") { TextAlignment = Alignment.MiddleLeft };

				filterBox		=	new TextBox( fp, null ) { TextAlignment = Alignment.MiddleLeft };
				filterBox.Text	=	"";
				filterBox.TypeWrite += FilterBox_TypeWrite;

				scrollBox				=	new ScrollBox( fp, 2, 14, 640+4+4, 480+4 );
				scrollBox.Border		=	1;
				scrollBox.BorderColor	=	MenuTheme.BorderColorLight;

				buttonAccept	=	new Button( fp, "Accept",  0,0,0,0, ()=>Accept(null) );
				buttonClose		=	new Button( fp, "Close",   0,0,0,0, ()=>Close() ) { RedButton = true };
				buttonZoomIn	=	new Button( fp, "ZoomIn",  0,0,0,0, ()=>Zoom++ );
				buttonZoomOut	=	new Button( fp, "ZoomOut", 0,0,0,0, ()=>Zoom-- );

				imageList		=	CreateImageList( atlasName );

				//Add( buttonAccept );
				Add( labelDir );
				Add( filterBox );
				Add( scrollBox );
				Add( buttonAccept );
				Add( buttonZoomIn );
				Add( buttonZoomOut );
				Add( buttonClose );
				scrollBox.Add( imageList );
				Add( labelStatus );

				Missclick += FileSelectorFrame_Missclick;
			}


			private void FilterBox_TypeWrite(object sender, KeyEventArgs e)
			{
				ApplyFilter();
			}


			void ApplyFilter ()
			{
				if (string.IsNullOrWhiteSpace(filterBox.Text)) {
					foreach( var child in imageList.Children ) {
						child.Visible = true;
					}
				} else {

					var text = filterBox.Text;

					foreach( var child in imageList.Children ) {
						child.Visible = child.Text.ToLowerInvariant().Contains( text.ToLowerInvariant() );
					}
				}
			}


			public int Zoom {
				get {
					return zoom;
				}
				set {
					if (zoom!=value) {
						zoom = value;
						zoom = MathUtil.Clamp( zoom, 0,2 );
						galery.ItemWidth  =  64 << zoom;
						galery.ItemHeight =  64 << zoom;
						galery.NumColumns =  10 >> zoom;
						imageList.MakeLayoutDirty();

						foreach (var child in imageList.Children) {
							child.ImageDstRect = new Rectangle(0,0, galery.ItemWidth, galery.ItemHeight);
						}
					}
				}
			}
			int zoom = 0;


			class AtlasButton : Frame {

				readonly TextureAtlas atlas;
				readonly TextureAtlasClip clip;
				bool mouseIn = false;

				public AtlasButton ( FrameProcessor frames, TextureAtlas atlas, string clipName, int size ) : base(frames)
				{
					this.atlas	=	atlas;
					this.clip	=	atlas.GetClipByName( clipName );

					Font			=	MenuTheme.NormalFont;
					TextAlignment	=	Alignment.BottomCenter;
					Padding			=	3;
					Border			=	1;
					Image			=	atlas.Texture;
					BackColor		=	Color.Zero;
					ImageColor		=	Color.White;
					ImageMode		=	FrameImageMode.Manual;
					ImageDstRect	=	new Rectangle(0,0,size,size);
					Text			=	clipName;

					this.MouseIn   +=   (s,e) => mouseIn = true;
					this.MouseOut  +=   (s,e) => mouseIn = false;

					ImageSrcRect	=	atlas.GetAbsoluteRectangleByName(clipName);
				}

				protected override void Update( GameTime gameTime )
				{
					if (mouseIn) {
						var gpr	  = this.GetPaddedRectangle(true);
						int frame = (Frames.MousePosition.X - gpr.X) * clip.Length / gpr.Width;
							frame = MathUtil.Clamp( frame, 0, clip.Length-1 );
						ImageSrcRect	=	atlas.AbsoluteRectangles[ clip.FirstIndex + frame ];
					} else {
						int frame = (int)(gameTime.Total.TotalSeconds * 10) % clip.Length;
						ImageSrcRect	=	atlas.AbsoluteRectangles[ clip.FirstIndex + frame ];
					}
				}
			}


			Frame CreateImageList ( string atlasName )
			{
				var size	= 128;
				var atlas	= Frames.Game.Content.Load<TextureAtlas>(atlasName);
				var colNum	= 5;
				var rowNum  = MathUtil.IntDivRoundUp( atlas.Count, colNum );
				var width	= (size)*colNum;
				var height	= (size)*rowNum;

				var panel		=	new Frame( Frames, 0,0, width, height, "", Color.Zero );
				galery			=	new GaleryLayout(5,128,128,0);
				panel.Layout	=	galery;
				panel.Tag		=	atlas;

				var names	= atlas.GetClipNames().OrderBy( n => n ).ToArray();

				for ( int i=0; i<names.Length; i++ ) {

					var name   = names[i];

					var button = new AtlasButton( Frames, atlas, name, size );

					button.Click			+= (s,e) => Accept(name);
					button.StatusChanged	+= Button_StatusChanged;

					panel.Add( button );
				}

				return panel;
			}


			private void Button_StatusChanged(object sender, StatusEventArgs e)
			{
				var button = (Frame)sender;
				
				switch ( e.Status ) {
					case FrameStatus.None:		
						button.ForeColor	=	MenuTheme.TextColorNormal;	
						button.BackColor	=	MenuTheme.ButtonColorNormal;	
						button.BorderColor	=	Color.Black;
						button.TextOffsetX	=	0;
						button.TextOffsetY	=	0;	
						break;
					case FrameStatus.Hovered:	
						button.ForeColor	=	MenuTheme.TextColorHovered;	
						button.BackColor	=	MenuTheme.ButtonColorHovered;	
						button.BorderColor	=	MenuTheme.TextColorHovered;
						button.TextOffsetX	=	0;
						button.TextOffsetY	=	0;	
						break;
					case FrameStatus.Pushed:	
						button.ForeColor	=	MenuTheme.TextColorPushed;	
						button.BackColor	=	MenuTheme.ButtonColorPushed;		
						button.BorderColor	=	MenuTheme.TextColorPushed;
						button.TextOffsetX	=	1;
						button.TextOffsetY	=	0;	
					break;
				}
			}

			public void Accept(string name)
			{
				setImageName(name);
				Close();
			}



			private void FileSelectorFrame_Missclick( object sender, EventArgs e )
			{
				Close();
			}
		}
	}
}
