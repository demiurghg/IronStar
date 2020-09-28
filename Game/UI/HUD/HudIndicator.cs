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
				}
			}
		}

		public int MaxValue
		{
			get { return maxValue; }
			set 
			{ 
				if (this.value!=value)
				{
					this.maxValue	=	value; 
					small.Text		=	"/" + value.ToString();
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
					icon.Image = Frames.Game.Content.Load<DiscTexture>(@"ui\icons\" + iconName);
				}
			}
		}

		int value = int.MinValue;
		int maxValue = int.MinValue;
		string iconName = null;
		Color color = Color.Zero;

		Frame	icon;
		Frame	big;
		Frame	small;

		public HudIndicator ( FrameProcessor ui, int x, int y, int value, int maxValue, string iconName, Color color ) : base( ui )
		{
			this.X			=	x;
			this.Y			=	y;
			this.Width		=	128;
			this.Height		=	32;
			this.BackColor	=	Color.Zero;

			icon	=	new Frame(ui,  0,0, 32,32, "", Color.Zero);

			big		=	new Frame(ui, 32,0, 32,20, "", Color.Zero) { 
				ForeColor		=	HudColors.TextColor, 
				Font			=	HudColors.HeaderFont,
				PaddingLeft		=	4,
				TextAlignment	=	Alignment.BaselineLeft,
				TextOffsetY		=	18,
				ShadowColor		=	HudColors.ShadowColor,
				ShadowOffset	=	new Vector2(1,1)
			};

			small	=	new Frame(ui, 64,0, 32,20, "", Color.Zero) { 
				ForeColor		=	HudColors.TextColorDim, 
				Font			=	HudColors.SmallFont,
				TextAlignment	=	Alignment.BaselineLeft,
				TextOffsetY		=	18,
				ShadowColor		=	HudColors.ShadowColor,
				ShadowOffset	=	new Vector2(1,1)
			};

			this.Add( icon );
			this.Add( big );
			this.Add( small );

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

			icon.X	=	0;
			big.X	=	icon.Width;
			small.X	=	big.X + big.Width;
		}
	}
}
