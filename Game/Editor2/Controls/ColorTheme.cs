using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace IronStar.Editor2.Controls {
	static class ColorTheme {

		public static readonly Color	ShadowColor				=	new Color(  0,  0,  0, 64);

		public static readonly Color	BorderColor				=	new Color( 10, 10, 10, 192);
		public static readonly Color	BorderColorLight		=	new Color( 10, 10, 10, 192);
		public static readonly Color	BackgroundColor			=	new Color( 30, 30, 30, 192);
		public static readonly Color	BackgroundColorDark		=	new Color( 15, 15, 15, 192);
		public static readonly Color	BackgroundColorLight	=	new Color( 45, 45, 45, 192);
		public static readonly Color	ScrollMarkerColor		=	new Color(120,120,120, 192);

		public static readonly Color	HighlightColor			=	new Color(150,150,150, 32 );
		
		public static readonly Color	TextColorNormal			=	new Color(150,150,150, 192);
		public static readonly Color	TextColorHovered		=	new Color(200,200,200, 192);
		public static readonly Color	TextColorPushed			=	new Color(220,220,220, 192);
		
		public static readonly Color	ElementColorNormal		=	new Color(120,120,120, 192);
		public static readonly Color	ElementColorHovered		=	new Color(150,150,150, 192);
		public static readonly Color	ElementColorPushed		=	new Color(180,180,180, 192);
		
		public static readonly Color	ButtonColorNormal		=	new Color( 90, 90, 90, 192);
		public static readonly Color	ButtonColorHovered		=	new Color(120,120,120, 192);
		public static readonly Color	ButtonColorPushed		=	new Color(150,150,150, 192);
		public static readonly Color	ButtonBorderColor		=	new Color( 20, 20, 20, 192);
		
		public static readonly Color	ColorWhite				=	new Color(180,180,180, 224);
		public static readonly Color	ColorGreen				=	new Color(144,239,144, 224);
		public static readonly Color	ColorRed				=	new Color(239,144,144, 224);
	}
}
