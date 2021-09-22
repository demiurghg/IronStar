﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Engine.Graphics;

namespace Fusion.Widgets 
{
	// #TODO #UI #REFACTOR -- Replace with some kind of IColorTheme interface
	public static class ColorTheme 
	{
		public static SpriteFont NormalFont = null;
		public static SpriteFont BoldFont = null;
		public static SpriteFont Monospaced = null;

		public static readonly int		ScrollSize				=	3;
		public static readonly int		ScrollVelocity			=	3;

		public static readonly int		HorizontalPadding		=	5;
		public static readonly int		VerticalPadding			=	1;

		public static readonly Color	AccentColor				=	Color.LightGray;
		public static readonly Color	AccentBorder			=	Color.Lerp( BorderColor, AccentColor, 0.5f );

		public static readonly Color	Transparent				=	new Color(  0,  0,  0, 0);

		public static readonly Color	ShadowColor				=	new Color(  0,  0,  0, 0);

		public static readonly Color	BorderColor				=	new Color( 10, 10, 10, 192);
		public static readonly Color	BorderColorLight		=	new Color( 15, 15, 15, 192);
		public static readonly Color	BackgroundColor			=	new Color( 30, 30, 30, 192);
		public static readonly Color	BackgroundColorDark		=	new Color( 15, 15, 15, 192);
		public static readonly Color	BackgroundColorLight	=	new Color( 45, 45, 45, 192);
		public static readonly Color	ScrollMarkerColor		=	new Color(120,120,120, 192);

		public static readonly Color	HighlightColor			=	new Color(150,150,150, 32 );
		public static readonly Color	FocusColor				=	new Color( 85, 85, 85, 192);
		
		public static readonly Color	TextColorNormal			=	new Color(210,210,210, 230);
		public static readonly Color	TextColorHovered		=	new Color(230,230,230, 230);
		public static readonly Color	TextColorPushed			=	new Color(250,250,250, 230);
		
		public static readonly Color	ElementColorNormal		=	new Color(120,120,120, 192);
		public static readonly Color	ElementColorHovered		=	new Color(150,150,150, 192);
		public static readonly Color	ElementColorPushed		=	new Color(180,180,180, 192);
		
		public static readonly Color	ButtonColorDark			=	new Color( 60, 60, 60, 192);
		public static readonly Color	ButtonColorNormal		=	new Color( 90, 90, 90, 192);
		public static readonly Color	ButtonColorHovered		=	new Color(120,120,120, 192);
		public static readonly Color	ButtonColorPushed		=	new Color(150,150,150, 192);
		public static readonly Color	ButtonBorderColor		=	new Color( 20, 20, 20, 192);
		
		public static readonly Color	ButtonRedColorNormal	=	new Color( 90, 45, 45, 192);
		public static readonly Color	ButtonRedColorHovered	=	new Color(120, 60, 60, 192);
		public static readonly Color	ButtonRedColorPushed	=	new Color(150, 75, 75, 192);

		public static readonly Color	ColorWhite				=	new Color(180,180,180, 224);
		public static readonly Color	ColorGreen				=	new Color(144,239,144, 224);
		public static readonly Color	ColorRed				=	new Color(239,144,144, 224);

		public static readonly Color	BackgroundColorRed		=	new Color(130, 30, 30, 192);

		public static readonly Color	DropdownColor			=	new Color( 15, 15, 15, 240);
		public static readonly Color	DropdownButtonNormal	=	new Color(  0,  0,  0,   0);
		public static readonly Color	DropdownButtonHovered	=	new Color( 64, 64, 64, 192);
		public static readonly Color	DropdownButtonPushed	=	new Color( 96, 96, 96, 192);
	}
}
