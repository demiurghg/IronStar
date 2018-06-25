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

namespace IronStar.Editor.Controls {

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

				Layout			=	new PageLayout(12,12,1,20,4,12,1);

				labelDir		=	new Label( fp, 0,0,0,0, "Atlas: " + atlasName ) { TextAlignment = Alignment.MiddleLeft };
				labelStatus		=	new Label( fp, 0,0,0,0, "....") { TextAlignment = Alignment.MiddleLeft };

				filterBox		=	new TextBox( fp, null, null ) { TextAlignment = Alignment.MiddleLeft };
				filterBox.Text	=	"";
				filterBox.TypeWrite += FilterBox_TypeWrite;

				scrollBox				=	new ScrollBox( fp, 2, 14, 640+4+4, 480+4 );
				scrollBox.Border		=	1;
				scrollBox.BorderColor	=	ColorTheme.BorderColorLight;

				buttonAccept	=	new Button( fp, "Accept",  0,0,0,0, ()=>Accept(null) );
				buttonClose		=	new Button( fp, "Close",   0,0,0,0, ()=>Close() ) { RedButton = true };
				buttonZoomIn	=	new Button( fp, "ZoomIn",  0,0,0,0, ()=>Zoom-- );
				buttonZoomOut	=	new Button( fp, "ZoomOut", 0,0,0,0, ()=>Zoom++ );

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


			Frame CreateImageList ( string atlasName )
			{
				var size	= 128;
				var atlas	= Frames.Game.Content.Load<TextureAtlas>(atlasName);
				var colNum	= 5;
				var rowNum  = MathUtil.IntDivRoundUp( atlas.Count, colNum );
				var width	= (size)*colNum;
				var height	= (size)*rowNum;

				var panel		= new Frame( Frames, 0,0, width, height, "", Color.Zero );
				galery			=	new GaleryLayout(5,128,128,0);
				panel.Layout	=	galery;

				var names	= atlas.GetSubImageNames().OrderBy( n => n ).ToArray();

				for ( int i=0; i<names.Length; i++ ) {
					var name   = names[i];
					var rect   = atlas.GetAbsoluteRectangleByName(name);
					var label  = string.Format("{0}\r\n{1}x{2}", name, rect.Width, rect.Height);
					var button = new Frame( Frames, 0,0,0,0, label, Color.Zero );
					button.TextAlignment = Alignment.BottomCenter;
					button.Padding		 = 3;
					button.Border		 = 1;
					button.Image		 = atlas.Texture;
					button.ImageColor	 = Color.White;
					button.ImageMode	 = FrameImageMode.Manual;
					button.ImageDstRect	 = new Rectangle(0,0,size,size);
					button.ImageSrcRect	 = atlas.GetAbsoluteRectangleByName(name);
					button.Click		+= (s,e) => Accept(name);

					button.StatusChanged += Button_StatusChanged;

					panel.Add( button );
				}

				return panel;
			}


			private void Button_StatusChanged(object sender, StatusEventArgs e)
			{
				var button = (Frame)sender;
				
				switch ( e.Status ) {
					case FrameStatus.None:		
						button.ForeColor	=	ColorTheme.TextColorNormal;	
						button.BackColor	=	ColorTheme.ButtonColorNormal;	
						button.BorderColor	=	Color.Black;
						button.TextOffsetX	=	0;
						button.TextOffsetY	=	0;	
						break;
					case FrameStatus.Hovered:	
						button.ForeColor	=	ColorTheme.TextColorHovered;	
						button.BackColor	=	ColorTheme.ButtonColorHovered;	
						button.BorderColor	=	ColorTheme.TextColorHovered;
						button.TextOffsetX	=	0;
						button.TextOffsetY	=	0;	
						break;
					case FrameStatus.Pushed:	
						button.ForeColor	=	ColorTheme.TextColorPushed;	
						button.BackColor	=	ColorTheme.ButtonColorPushed;		
						button.BorderColor	=	ColorTheme.TextColorPushed;
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
