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

namespace Fusion.Widgets.Dialogs 
{
	public class AtlasButton : Frame 
	{
		readonly TextureAtlas atlas;
		TextureAtlasClip clip;
		bool mouseIn = false;

		string clipName;
		public string ClipName
		{
			get { return clipName; }
			set 
			{
				if (clipName!=value)
				{
					clipName = value;
					clip = (value==null) ? null : atlas?.GetClipByName(clipName);
				}
			}
		}

		
		public AtlasButton ( UIState ui, TextureAtlas atlas, string clipName, int size ) : base(ui)
		{
			this.atlas		=	atlas;
			this.ClipName	=	clipName;

			Font			=	ColorTheme.NormalFont;
			TextAlignment	=	Alignment.BottomCenter;
			Padding			=	3;
			Border			=	1;
			Image			=	atlas.Texture;
			BackColor		=	Color.Zero;
			ImageColor		=	Color.White;
			ImageDstRect	=	new Rectangle(0,0,size,size);

			this.MouseIn	+=	(s,e) => mouseIn = true;
			this.MouseOut	+=	(s,e) => mouseIn = false;

			ImageSrcRect	=	new Rectangle(0,0,0,0);// atlas.GetAbsoluteRectangleByName(clipName);

			StatusChanged   +=AtlasButton_StatusChanged;
		}

		private void AtlasButton_StatusChanged( object sender, StatusEventArgs e )
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

		protected override void Update( GameTime gameTime )
		{
			if (atlas!=null && clip!=null)
			{
				if (mouseIn) 
				{
					var gpr			=	this.GetPaddedRectangle(true);
					int frame		=	(ui.MousePosition.X - gpr.X) * clip.Length / gpr.Width;
						frame		=	MathUtil.Clamp( frame, 0, clip.Length-1 );
					ImageSrcRect	=	atlas.AbsoluteRectangles[ clip.FirstIndex + frame ];
					ImageMode		=	FrameImageMode.Manual;
					Image			=	atlas.Texture;
				} 
				else 
				{
					int frame		=	(int)(gameTime.Current.TotalSeconds * 10) % clip.Length;
					ImageSrcRect	=	atlas.AbsoluteRectangles[ clip.FirstIndex + frame ];
					ImageMode		=	FrameImageMode.Manual;
					Image			=	atlas.Texture;
				}
			}
			else
			{
				Image			=	ui.Game.Content.Load<DiscTexture>("null");
				ImageMode		=	FrameImageMode.Stretched;
			}
		}
	}
}
