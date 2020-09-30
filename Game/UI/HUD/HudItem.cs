using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion.Core.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using BEPUphysics;
using BEPUphysics.Character;
using Fusion.Engine.Frames;

namespace IronStar.UI.HUD 
{
	public class HudItem : Frame 
	{
		public string IconName 
		{
			get { return iconName; } 
			set 
			{ 
				if (iconName!=value)
				{
					iconName = value; 
					icon.Image = Frames.Game.Content.Load<DiscTexture>(iconName);
				}
			}
		}

		public string Text1 { get { return label1.Text; } set { label1.Text = value; } }
		public string Text2 { get { return label2.Text; } set { label2.Text = value; } }

		string iconName = null;
		Color color = Color.Zero;

		HudAlignment	alignment;

		Frame	icon;
		Frame	label1;
		Frame	label2;

		public HudItem ( FrameProcessor ui, HudAlignment alignment, int x, int y, string text1, string text2, string iconName, Color color ) : base( ui )
		{
			this.alignment	=	alignment;
			this.X			=	x;
			this.Y			=	y;
			this.Width		=	128;
			this.Height		=	32;
			this.BackColor	=	Color.Zero;
			this.ImageColor	=	color;

			bool alignLeft	=	alignment==HudAlignment.Left;

			icon	=	new Frame(ui, alignLeft ? 0 : 128-32, 0, 32,32, "", Color.Zero) {
				ImageColor	=	color,
			};

			label1	=	new Frame(ui, alignLeft ? 32 : 0, 0, 96,16, "", Color.Zero) {
				Font			=	HudColors.TinyFont,
				ForeColor		=	HudColors.TextColor,
				//ShadowColor		=	HudColors.ShadowColor,
				//ShadowOffset	=	Vector2.One,
				TextAlignment	=	alignLeft ? Alignment.BaselineLeft : Alignment.BaselineRight,
				Text			=	text1,
				TextLeading		=	8,
				TextOffsetY		=	7,
				PaddingLeft		=	4,
				PaddingRight	=	4,
			};

			label2	=	new Frame(ui, alignLeft ? 32 : 0,16, 96,16, "", Color.Zero) {
				Font			=	HudColors.TinyFont,
				ForeColor		=	HudColors.TextColorDim,
				//ShadowColor		=	HudColors.ShadowColor,
				//ShadowOffset	=	Vector2.One,
				TextAlignment	=	alignLeft ? Alignment.BaselineLeft : Alignment.BaselineRight,
				Text			=	text2,
				TextLeading		=	8,
				TextOffsetY		=	7,
				PaddingLeft		=	4,
				PaddingRight	=	4,
			};

			Add( icon );
			Add( label1 );
			Add( label2 );

			Text1		=	text1;
			Text2		=	text2;
			IconName	=	iconName;
		}
	}
}
