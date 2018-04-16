using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace Fusion.Core.Mathematics {

	public struct HSVColor {

		public float H;
		public float S;
		public float V;

		public HSVColor(float h, float s, float v)
		{
			this.H = h;
			this.S = s;
			this.V = v;
		}

		public bool Equals(HSVColor hsv)
		{
			return (this.H == hsv.H) && (this.S == hsv.S) && (this.V == hsv.V);
		}		



		/// <summary>
		/// Converts RGB [0..1] to HSV where H=[0..360] and S,V=[0..1].
		/// </summary>
		/// <param name="color"></param>
		/// <returns></returns>
		public static HSVColor ConvertRgbToHsv(Color4 color)
		{
			return ConvertRgbToHsv( new Color3( color.Red, color.Green, color.Blue ) );
		}



		/// <summary>
		/// Converts HSV color and Alpha to RGBA
		/// </summary>
		/// <param name="hsv"></param>
		/// <param name="alpha"></param>
		/// <returns></returns>
		public static Color4 ConvertHsvToRgb(HSVColor hsv, float alpha)
		{
			return new Color4( ConvertHsvToRgb( hsv ), alpha );
		}


		/// <summary>
		/// Converts RGB [0..1] to HSV where H=[0..360] and S,V=[0..1].
		/// </summary>
		/// <param name="color"></param>
		/// <returns></returns>
		public static HSVColor ConvertRgbToHsv(Color3 color)
		{
			float R		=	MathUtil.Clamp( color.Red	, 0, 1 );
			float G		=	MathUtil.Clamp( color.Green	, 0, 1 );
			float B		=	MathUtil.Clamp( color.Blue	, 0, 1 );
			float max	=	Math.Max( Math.Max( R, G ), B );
			float min	=	Math.Min( Math.Min( R, G ), B );
			float delta	=	max - min;

			float H		=	0;
			float S		=	0;
			float V		=	0;

			if (max==min) {
				H = 0;
			} else {
				if ( max==R && G >= B ) H = 60 * (G-B) / delta + 0;
				if ( max==R && G <  B ) H = 60 * (G-B) / delta + 360;
				if ( max==G			  ) H = 60 * (B-R) / delta + 120;
				if ( max==B			  ) H = 60 * (R-G) / delta + 240;
			}

			if (max==0) {
				S = 0;
			} else {
				S = 1.0f - min/max;
			}

			V = max;

			return new HSVColor( H, S, V );
		}


		/// <summary>
		/// Converts HSV color to RGB
		/// </summary>
		/// <param name="hsv"></param>
		/// <returns></returns>
		public static Color3 ConvertHsvToRgb(HSVColor hsv)
		{
			var H		=	MathUtil.Clamp( hsv.H, 0, 360 );
			var S		=	MathUtil.Clamp( hsv.S, 0, 1   );
			var V		=	MathUtil.Clamp( hsv.V, 0, 1   );

			var Hi		=	((int)Math.Truncate( H / 60f )) % 6;

			var Vmin	=	(1 - S) * V;
			var a		=	(V - Vmin) * ((H % 60f)/60f);
			var Vinc	=	Vmin + a;
			var Vdec	=	V - a;

			switch (Hi) {
				case 0: return new Color3( V   , Vinc, Vmin );
				case 1: return new Color3( Vdec, V   , Vmin );
				case 2: return new Color3( Vmin, V   , Vinc );
				case 3: return new Color3( Vmin, Vdec, V    );
				case 4: return new Color3( Vinc, Vmin, V    );
				case 5: return new Color3( V   , Vmin, Vdec );
				default: return Color3.Black;
			}
		}
	}
}
