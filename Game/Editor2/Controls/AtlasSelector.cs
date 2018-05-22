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

namespace IronStar.Editor2.Controls {

	static public class AtlasSelector {

		const int DialogWidth	= 640 + 4 + 4 + 4;
		const int DialogHeight	= 480 + 2 + 2 + 14 + 2 + 20 + 2;


		static public void ShowDialog ( FrameProcessor fp, string atlasName, string oldImageName, Action<string> setImageName )
		{
			var atlasSelector	=	new AtlasSelectorFrame( fp, atlasName, oldImageName, setImageName );

			fp.RootFrame.Add( atlasSelector );
			fp.ModalFrame = atlasSelector;

			FrameUtils.CenterFrame( atlasSelector );
		}


		class AtlasSelectorFrame : Panel {

			readonly Action<string> setImageName;
			readonly string		oldImageName;

			Label		labelDir;
			ScrollBox	scrollBox;
			Button		buttonAccept;
			Button		buttonClose;
			Frame		imageList;



			public AtlasSelectorFrame ( FrameProcessor fp, string atlasName, string oldImageName, Action<string> setImageName ) 
			: base ( fp, 0,0, DialogWidth, DialogHeight )
			{
				this.oldImageName	=	oldImageName;
				this.setImageName	=	setImageName;

				labelDir		=	new Label( fp, 2, 3, DialogWidth - 4, 10, "Atlas: " + atlasName );

				scrollBox				=	new ScrollBox( fp, 2, 14, 640+4+4, 480+4 );
				scrollBox.Border		=	1;
				scrollBox.BorderColor	=	ColorTheme.BorderColorLight;

				buttonAccept	=	new Button( fp, "Accept", DialogWidth - 140 - 2, DialogHeight - 2 - 20, 140, 20, ()=>Accept(null) );
				buttonClose		=	new Button( fp, "Close",  2,                     DialogHeight - 2 - 20, 140, 20, ()=>Close() );

				imageList		=	CreateImageList( atlasName );

				//Add( buttonAccept );
				Add( buttonClose );
				Add( scrollBox );
				Add( labelDir );
				scrollBox.Add( imageList );

				Missclick += FileSelectorFrame_Missclick;
			}



			Frame CreateImageList ( string atlasName )
			{
				var size	= 128;
				var atlas	= Frames.Game.Content.Load<TextureAtlas>(atlasName);
				var colNum	= 5;
				var rowNum  = MathUtil.IntDivRoundUp( atlas.Count, colNum );
				var width	= (size)*colNum;
				var height	= (size)*rowNum;

				var panel	= new Frame( Frames, 0,0, width, height, "", Color.Zero );

				var names	= atlas.GetSubImageNames().OrderBy( n => n ).ToArray();

				for ( int i=0; i<names.Length; i++ ) {
					var name   = names[i];
					var rect   = atlas.GetAbsoluteRectangleByName(name);
					var label  = string.Format("{0}\r\n{1}x{2}", name, rect.Width, rect.Height);
					var row    = i / colNum;
					int col    = i % colNum;
					var button = new Frame( Frames, col*size, row*size, size, size, label, Color.Zero );
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

			public void Close ()
			{
				Frames.RootFrame.Remove( this );
				Frames.ModalFrame = null;
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
