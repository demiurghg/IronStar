using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Engine.Graphics;

namespace IronStar.UI.Controls {

	public static class MenuTheme {

		//	Fire in the sky https://www.color-hex.com/color-palette/48018
		//	Red Fire		https://www.color-hex.com/color-palette/67224

		public static SpriteFont BigFont = null;
		public static SpriteFont NormalFont = null;
		public static SpriteFont HeaderFont = null;
		public static SpriteFont SmallFont = null;
		public static SpriteFont Monospaced = null;

		public static DiscTexture ArrowDown = null;

		//	Base colors :
		public static readonly Color	BackColor				=	new Color(   0,   0,   0, 255 );
		public static readonly Color	TextColor				=	new Color( 255, 255, 255, 255 );
		public static readonly Color	ElementColor			=	new Color( 255, 255, 255,  16 );
		public static readonly Color	AccentColor				=	new Color( 255,   0,   0, 255 );
		public static readonly Color	SelectColor				=	new Color( 255, 255, 255,  32 );
		public static readonly Color	Transparent				=	new Color(   0,   0,   0,   0 );
		public static readonly Color	ShadowColor				=	new Color(   0,   0,   0,   0 );

		public static readonly Color	ColorPositive			=	new Color( 128, 255, 128, 224 );
		public static readonly Color	ColorNegative			=	new Color( 255, 128, 128, 224 );

		public static readonly Color	ImageColor				=	new Color( 64, 64, 64, 255 );

		//	Base metrics :
		public static readonly int		CaptionHeight			=	15;
		public static readonly int		ScrollSize				=	10;
		public static readonly int		ElementHeight			=	40;
		public static readonly int		Margin					=	4;
		public static readonly int		SmallContentPadding		=	10;
		public static readonly int		PanelContentPadding		=	20;
		public static readonly int		MainContentPadding		=	60;


		//	Derived colors :

		public static readonly Color	ScrollMarkerColor		=	ElementColor;

		public static readonly Color	TextColorNormal			=	TextColor;
		public static readonly Color	TextColorDimmed			=	Color.Lerp( TextColorNormal, Color.Black, 0.5f );
		public static readonly Color	TextColorHovered		=	TextColor;
		public static readonly Color	TextColorPushed			=	TextColor;

		public static readonly Color	ElementLineHighlight	=	Average( Transparent, SelectColor );
		
		public static readonly Color	ElementColorNormal		=	Transparent;
		public static readonly Color	ElementColorHovered		=	SelectColor;
		public static readonly Color	ElementColorPushed		=	AlphaMul2( SelectColor );
		
		public static readonly Color	ButtonColorNormal		=	AlphaDiv2( SelectColor );
		public static readonly Color	ButtonColorHovered		=	SelectColor;
		public static readonly Color	ButtonColorPushed		=	AlphaMul2( SelectColor );
		
		public static readonly Color	BigButtonColorNormal	=	Transparent;
		public static readonly Color	BigButtonColorHovered	=	SelectColor;
		public static readonly Color	BigButtonColorPushed	=	AlphaMul2( SelectColor );
																	   
		public static readonly Color	DropdownColor			=	new Color( 15, 15, 15, 240);
		public static readonly Color	DropdownButtonNormal	=	new Color(  0,  0,  0,   0);
		public static readonly Color	DropdownButtonHovered	=	new Color( 64, 64, 64, 192);
		public static readonly Color	DropdownButtonPushed	=	new Color( 96, 96, 96, 192);


		//	to remove...


		static Color Alpha( Color color, int alpha )
		{
			return new Color( color.R, color.G, color.B, (byte)(MathUtil.Clamp(alpha,0,255) ) );
		}

		static Color Average( Color colorA, Color colorB )
		{
			return Color.Lerp( colorA, colorB, 0.5f );;
		}

		static Color AlphaMul2( Color color )
		{
			return Alpha( color, color.A * 2 );
		}

		static Color AlphaDiv2( Color color )
		{
			return Alpha( color, color.A / 2 );
		}
	}
}
