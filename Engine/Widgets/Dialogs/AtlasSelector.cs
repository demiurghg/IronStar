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
using Fusion.Widgets;
using Fusion.Core;

namespace Fusion.Widgets.Dialogs {

	static public class AtlasSelector 
	{
		const int DialogWidth	= 640;
		const int DialogHeight	= 480;

		static public void ShowDialog ( FrameProcessor fp, string atlasName, string oldImageName, Action<string> setImageName )
		{
			var atlasSelector	=	new AtlasSelectorFrame( fp, atlasName, oldImageName, setImageName );

			fp.ShowDialogCentered( atlasSelector );
		}


		public class AtlasSelectorFrame : Panel 
		{
			readonly Action<string> setImageName;
			readonly string		oldImageName;
			readonly int[] sizeList = new[] { 16,32,48,64,96,128,192,256,384,512};

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
				AllowResize		=	true;

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
				scrollBox.BorderColor	=	ColorTheme.BorderColorLight;

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

				Zoom	=	3;
			}


			private void FilterBox_TypeWrite(object sender, KeyEventArgs e)
			{
				ApplyFilter();
			}


			void ApplyFilter ()
			{
				if (string.IsNullOrWhiteSpace(filterBox.Text)) 
				{
					foreach( var child in imageList.Children ) 
					{
						child.Visible = true;
					}
				} 
				else 
				{
					var text = filterBox.Text;

					foreach( var child in imageList.Children ) 
					{
						child.Visible = child.Text.ToLowerInvariant().Contains( text.ToLowerInvariant() );
					}
				}
			}


			public int Zoom 
			{
				get 
				{
					return zoom;
				}
				set {
					if (zoom!=value) 
					{
						zoom				=	MathUtil.Clamp( value, 0, sizeList.Length-1 );
						var size			=	sizeList[ zoom ];
						galery.ItemWidth	=	size;
						galery.ItemHeight	=	size;

						imageList.MakeLayoutDirty();

						foreach (var child in imageList.Children) 
						{
							child.ImageDstRect = new Rectangle(0,0, galery.ItemWidth, galery.ItemHeight);
						}
					}
				}
			}
			int zoom = 0;


			Frame CreateImageList ( string atlasName )
			{
				var atlas	= Frames.Game.Content.Load<TextureAtlas>(atlasName);

				var panel		=	new Frame( Frames, 0,0, 0, 0, "", Color.Zero );
				galery			=	new GaleryLayout(128,128,0);
				panel.Layout	=	galery;
				panel.Tag		=	atlas;

				var names	= atlas.GetClipNames().OrderBy( n => n ).ToArray();

				for ( int i=0; i<names.Length; i++ ) 
				{
					var name	=	names[i];
					var button	=	new AtlasButton( Frames, atlas, name, 32 );
					button.Text	=	name;

					button.Click+= (s,e) => Accept(name);

					panel.Add( button );
				}

				panel.RunLayout();

				return panel;
			}


			public void Accept(string name)
			{
				setImageName(name);
				Close();
			}
		}
	}
}
