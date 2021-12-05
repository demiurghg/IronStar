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
	public enum	HudAlignment	 
	{
		Left,
		Right
	}

	public class HudIndicator : Frame 
	{
		public int Value
		{
			get { return value; }
			set 
			{ 
				if (this.value!=value)
				{
					this.value	=	value; 
					big.Text	=	value.ToString();
					MakeLayoutDirty();
				}
			}
		}

		public int MaxValue
		{
			get { return maxValue; }
			set 
			{ 
				if (this.maxValue!=value)
				{
					this.maxValue	=	value; 
					small.Text		=	"/" + value.ToString();
					MakeLayoutDirty();
				}
			}
		}

		public Color Color
		{
			get { return color; } 
			set 
			{ 
				if (color!=value)
				{
					color = value; 
					icon.ImageColor	=	color;
				}
			}
		}

		public string IconName 
		{
			get { return iconName; } 
			set 
			{ 
				if (iconName!=value)
				{
					iconName = value; 
					icon.Image = ui.Game.Content.Load<DiscTexture>(iconName);
				}
			}
		}

		int value = int.MinValue;
		int maxValue = int.MinValue;
		string iconName = null;
		Color color = Color.Zero;

		HudAlignment	alignment;

		Frame		icon;
		Frame		big;
		Frame		small;
		BarFrame	bar;

		public HudIndicator ( UIState ui, HudAlignment alignment, int x, int y, int value, int maxValue, string iconName, Color color ) : base( ui )
		{
			this.alignment	=	alignment;
			this.X			=	x;
			this.Y			=	y;
			this.Width		=	128;
			this.Height		=	32;
			this.BackColor	=	Color.Zero;

			var textAlign	=	alignment==HudAlignment.Left ? Alignment.BaselineLeft : Alignment.BaselineRight;

			icon	=	new Frame(ui,  0,0, 32,32, "", Color.Zero);

			big		=	new Frame(ui, 32,0, 32,24, "", Color.Zero) { 
				ForeColor		=	HudColors.TextColor, 
				Font			=	HudColors.HeaderFont,
				PaddingLeft		=	4,
				TextAlignment	=	textAlign,
				TextOffsetY		=	20,
				ShadowColor		=	HudColors.ShadowColor,
				ShadowOffset	=	new Vector2(1,1)
			};

			small	=	new Frame(ui, 64,0, 32,24, "", Color.Zero) { 
				ForeColor		=	HudColors.TextColorDim, 
				Font			=	HudColors.SmallFont,
				PaddingRight	=	4,
				TextAlignment	=	textAlign,
				TextOffsetY		=	20,
				ShadowColor		=	HudColors.ShadowColor,
				ShadowOffset	=	new Vector2(1,1)
			};

			int barPosX		=	alignment==HudAlignment.Left ? 32+4 : 0;
			bar				=	new BarFrame(ui, this, barPosX, 24, 128-32-4, 4, color);

			this.Add( icon );
			this.Add( big );
			this.Add( small );
			this.Add( bar );

			this.Value		=	value;
			this.MaxValue	=	maxValue;
			this.IconName	=	iconName;
			this.Color		=	color;
		}


		public override void RunLayout()
		{
			icon.Adjust(true, true);
			big.Adjust(true, false);
			small.Adjust(true, false);

			if (alignment==HudAlignment.Left)
			{
				icon.X	=	0;
				big.X	=	icon.Width;
				small.X	=	big.X + big.Width;
			}
			else
			{
				icon.X	=	Width - icon.Width;
				small.X	=	icon.X - small.Width;
				big.X	=	small.X - big.Width;
			}
		}


		class BarFrame : Frame 
		{
			Color color;
			HudIndicator indicator;

			public BarFrame( UIState ui, HudIndicator indicator, int x, int y, int w, int h, Color color ) : base(ui, x, y, w, h, "", Color.Zero)
			{
				this.indicator	=	indicator;
				this.color		=	color;
				BorderLeft		=	1;
				BorderRight		=	1;
				BorderColor		=	color;
			}


			int GetBarWidth()
			{
				if (indicator.MaxValue==0 || indicator.Value==0) return 0;
				return GlobalRectangle.Width * indicator.Value / indicator.MaxValue;
			}


			protected override void DrawFrame( GameTime gameTime, SpriteLayer spriteLayer, int clipRectIndex )
			{
				base.DrawFrame( gameTime, spriteLayer, clipRectIndex );

				var r			=	GlobalRectangle;
					r.X			=	r.X + 1;
					r.Width		=	r.Width - 2;
				int barWidth	=	GetBarWidth();

				var c1		=	color;
				var c0		=	color;
					c0.A	=	0;

				if (indicator.alignment==HudAlignment.Left)
				{
					var rect	=	new Rectangle( r.X, r.Y, barWidth, r.Height );
					spriteLayer.DrawGradient( rect, c0, c1, c0, c1, clipRectIndex );
					spriteLayer.Draw( null, rect.X + rect.Width, rect.Y, 1, rect.Height, HudColors.TextColor, clipRectIndex );
				}
				else
				{
					var rect	=	new Rectangle( r.X+r.Width - barWidth, r.Y, barWidth, r.Height );
					spriteLayer.DrawGradient( rect, c1, c0, c1, c0, clipRectIndex );
					spriteLayer.Draw( null, r.X+r.Width - barWidth, rect.Y, 1, rect.Height, HudColors.TextColor, clipRectIndex );
				}
			}
		}
	}
}
