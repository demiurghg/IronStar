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
using Fusion.Engine.Input;

namespace IronStar.Editor2.Controls {
	partial class ColorPicker : Frame {

		static ColorPicker colorPicker;

		static public void ShowDialog ( FrameProcessor fp, int x, int y, Color initialColor, Action<Color> setColor )
		{
			colorPicker = new ColorPicker( fp, initialColor, setColor );

			colorPicker.X	=	Math.Max( 10, Math.Min( x, fp.RootFrame.Width  - DialogWidth  - 10 ) );
			colorPicker.Y	=	Math.Max( 10, Math.Min( y, fp.RootFrame.Height - DialogHeight - 10 ) );

			fp.RootFrame.Add( colorPicker );
			fp.ModalFrame = colorPicker;
		}


		const int DialogWidth	=	80 + 256 + 3 + 2*2;
		const int DialogHeight	=	244;


		Color targetColor;
		Color initColor;
		Action<Color> setColor;

		ColorField colorField;

		Frame oldColorSample;
		Frame newColorSample;

		float channelRed;
		float channelGreen;
		float channelBlue;
		float channelAlpha;

		float channelHue;
		float channelSaturation;
		float channelBrightness;

		Frame sliderRed		;
		Frame sliderGreen	;
		Frame sliderBlue	;
		Frame sliderAlpha	;

		void UpdateFromColor ()
		{
			channelRed		=	targetColor.R;
			channelGreen	=	targetColor.G;
			channelBlue		=	targetColor.B;
			channelAlpha	=	targetColor.A;

			UpdateSliders();
		}


		void UpdateFromRGBA ()
		{
			targetColor = new Color( new Vector4( channelRed / 256.0f, channelGreen / 256.0f, channelBlue / 256.0f, channelAlpha / 256.0f ) );
			newColorSample.BackColor = targetColor;

			UpdateSliders();

			setColor( targetColor );
		}


		void UpdateSliders ()
		{
			sliderRed	.Text	=	channelRed		.ToString();
			sliderGreen	.Text	=	channelGreen	.ToString();
			sliderBlue	.Text	=	channelBlue		.ToString();
			sliderAlpha	.Text	=	channelAlpha	.ToString();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="fp"></param>
		private ColorPicker ( FrameProcessor fp, Color initColor, Action<Color> setColor ) : base(fp)
		{
			this.initColor		=	initColor;
			this.targetColor	=	initColor;
			this.setColor		=	setColor;

			Width	=	DialogWidth;
			Height	=	DialogHeight;	

			Padding			=	1;
			Border			=	1;
			BorderColor		=	ColorTheme.ColorBorder;
			BackColor		=	ColorTheme.ColorBackground;

			oldColorSample	=	AddColorButton(  2,   2, 80, 30, "", initColor, ()=> { targetColor = initColor; UpdateFromColor(); } );
			newColorSample	=	AddColorButton(  2,  32, 80, 30, "", initColor, null );

			this.Missclick +=ColorPicker_Missclick;

			/*AddColorButton(  2,  72, 40, 10, "", Color.White,	null );
			AddColorButton( 42,  72, 40, 10, "", Color.Black,	null );

			AddColorButton(  2,  82, 40, 10, "", Color.Red,		null );
			AddColorButton( 42,  82, 40, 10, "", Color.Cyan,	null );
			AddColorButton(  2,  92, 40, 10, "", Color.Lime,	null );
			AddColorButton( 42,  92, 40, 10, "", Color.Magenta, null );
			AddColorButton(  2, 102, 40, 10, "", Color.Blue,	null );
			AddColorButton( 42, 102, 40, 10, "", Color.Yellow,	null );*/

			colorField	=	new ColorField( Frames, 80+3, 2, 180+2, 128+2, null );
			Add( colorField );

			AddLabel( 2, 140+3, "Red"	);
			AddLabel( 2, 151+3, "Green" );
			AddLabel( 2, 162+3, "Blue"	);
			AddLabel( 2, 173+3, "Alpha"	);

			AddLabel( 2, 190+3, "Hue"	);
			AddLabel( 2, 201+3, "Saturation" );
			AddLabel( 2, 212+3, "Brightness" );

			AddLabel( 2, 230,   "Temp, (K)" );


			sliderRed	=	new Slider( 
				Frames, 
				()=>channelRed, 
				(r)=> { channelRed = r; UpdateFromRGBA(); UpdateSliders(); },
				0, 255, 16, 1 ) {
					X = 80+3,
					Y = 140+3,
					Width = 256+2,
					Height = 10,
					Border = 1,
					BackColor = new Color(0,0,0,64),
					SliderColor = new Color(255,0,0,255),
					ForeColor = new Color(0,0,0,160),
				};

			sliderGreen	=	new Slider( 
				Frames, 
				()=>channelGreen, 
				(r)=> { channelGreen = r; UpdateFromRGBA(); UpdateSliders(); },
				0, 255, 16, 1 ) {
					X = 80+3,
					Y = 151+3,
					Width = 256+2,
					Height = 10,
					Border = 1,
					BackColor = new Color(0,0,0,64),
					SliderColor = new Color(0,255,0,255),
					ForeColor = new Color(0,0,0,160),
				};

			sliderBlue	=	new Slider( 
				Frames, 
				()=>channelBlue, 
				(r)=> { channelBlue = r; UpdateFromRGBA(); UpdateSliders(); },
				0, 255, 16, 1 ) {
					X = 80+3,
					Y = 162+3,
					Width = 256+2,
					Height = 10,
					Border = 1,
					BackColor = new Color(0,0,0,64),
					SliderColor = new Color(0,0,255,255),
					ForeColor = new Color(0,0,0,160),
				};

			sliderAlpha	=	new Slider( 
				Frames, 
				()=>channelAlpha, 
				(r)=> { channelAlpha = r; UpdateFromRGBA(); UpdateSliders(); },
				0, 255, 16, 1 ) {
					X = 80+3,
					Y = 173+3,
					Width = 256+2,
					Height = 10,
					Border = 1,
					BackColor = new Color(0,0,0,64),
					SliderColor = new Color(128,128,128,255),
					ForeColor = new Color(0,0,0,160),
				};


			Add( sliderRed );
			Add( sliderGreen );
			Add( sliderBlue );
			Add( sliderAlpha );

			UpdateFromColor();
			UpdateSliders();

		}

		private void ColorPicker_Missclick( object sender, EventArgs e )
		{
			Frames.RootFrame.Remove( this );
			Frames.ModalFrame = null;
		}

		Frame AddColorButton ( int x, int y, int w, int h, string text, Color color, Action action )
		{
			var frame = new Frame( Frames, x,y,w,h, text, color );
			
			frame.Border		=	1;
			frame.BorderColor	=	Color.Black;
			frame.ForeColor		=	new Color(0,0,0,64);

			Add( frame );

			if (action!=null) {
				frame.Click += (s,e) => action();
			}

			return frame;
		}


		void AddLabel( int x, int y, string text )
		{
			var frame = new Frame( Frames, x,y, text.Length * 8+2, 10, text, Color.Zero );
			
			frame.ForeColor		=	ColorTheme.TextColorNormal;
			frame.TextAlignment	=	Alignment.MiddleLeft;
			frame.ShadowColor	=	new Color(0,0,0,64);
			frame.ShadowOffset	=	new Vector2(1,1);

			Add( frame );
		}

		
	}
}
