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

namespace IronStar.UI.Controls.Dialogs {
	public partial class ColorPicker : Frame {

		static ColorPicker colorPicker;

		static public void ShowDialog ( FrameProcessor fp, int x, int y, Color initialColor, Action<Color> setColor )
		{
			colorPicker = new ColorPicker( fp, initialColor, setColor );

			colorPicker.X	=	x;
			colorPicker.Y	=	y;

			fp.RootFrame.Add( colorPicker );
			fp.ModalFrame = colorPicker;

			colorPicker.ConstrainFrame(10);
		}


		const int DialogWidth	=	80 + 256 + 3 + 2*2;
		const int DialogHeight	=	165 + 4*7-1;


		Color targetColor;
		Color initColor;
		Action<Color> setColor;

		ColorField colorField;

		Frame oldColorSample;
		Frame newColorSample;
		float temperature	=	6600;

		Color4		colorRGBA;
		HSVColor	colorHSV;

		Slider sliderRed	;
		Slider sliderGreen	;
		Slider sliderBlue	;
		Slider sliderAlpha	;
		Slider sliderSat	;
		Slider sliderTemp	;

		void UpdateFromColor ()
		{
			colorRGBA	=	new Color4( targetColor.R/255f, targetColor.G/255f, targetColor.B/255f, targetColor.A/255f );
			colorHSV	=	HSVColor.ConvertRgbToHsv( colorRGBA );
			newColorSample.BackColor = targetColor;
			UpdateSliders();

			setColor( targetColor );
		}


		void UpdateFromRGBA ()
		{
			targetColor =	new Color( colorRGBA );
			colorHSV	=	HSVColor.ConvertRgbToHsv( colorRGBA );
			newColorSample.BackColor = targetColor;

			UpdateSliders();

			setColor( targetColor );
		}

		void UpdateFromHSV ()
		{
			colorRGBA	= HSVColor.ConvertHsvToRgb( colorHSV, colorRGBA.Alpha );
			targetColor = new Color( colorRGBA );
			newColorSample.BackColor = targetColor;

			UpdateSliders();

			setColor( targetColor );
		}


		void UpdateSliders ()
		{
			sliderRed	.Text	=	( (int)(255 * colorRGBA.Red		) ).ToString();
			sliderGreen	.Text	=	( (int)(255 * colorRGBA.Green	) ).ToString();
			sliderBlue	.Text	=	( (int)(255 * colorRGBA.Blue	) ).ToString();
			sliderAlpha	.Text	=	( (int)(255 * colorRGBA.Alpha	) ).ToString();

			sliderSat	.Text	=	( (int)(100 * colorHSV.S		) ).ToString();
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
			BorderColor		=	ColorTheme.BorderColor;
			BackColor		=	ColorTheme.BackgroundColor;

			oldColorSample	=	AddColorButton(  2,   2, 80, 30, "", initColor, ()=> { targetColor = initColor; UpdateFromColor(); } );
			newColorSample	=	AddColorButton(  2,  32, 80, 30, "", initColor, null );

			this.Missclick +=ColorPicker_Missclick;


			colorField	=	new ColorField( Frames, 80+3, 2, 180+2, 100+2, 
				() => colorHSV, 
				(hsv) => { colorHSV = hsv; UpdateFromHSV(); }
			);
			Add( colorField );


			int height	=	ColorTheme.NormalFont.LineHeight;


			AddLabel( 2, 107 + height * 0-1, "Red"	);
			AddLabel( 2, 107 + height * 1-1, "Green" );
			AddLabel( 2, 107 + height * 2-1, "Blue"	);
			AddLabel( 2, 107 + height * 3-1, "Alpha"	);
										 
			AddLabel( 2, 107 + height * 4-1, "Temp, (K)" );


			sliderRed	=	new Slider( 
				Frames, 
				()=>colorRGBA.Red * 255, 
				(r)=> { colorRGBA.Red = r/255; UpdateFromRGBA(); UpdateSliders(); },
				0, 255, 16, 1 ) {
					X = 83,
					Y = 107 + height * 0+1,
					Width = 256+2,
					Height = height-2,
					Border = 1,
					BackColor = new Color(0,0,0,64),
					SliderColor = new Color(255,0,0,255),
					ForeColor = new Color(0,0,0,160),
				};

			sliderGreen	=	new Slider( 
				Frames, 
				()=>colorRGBA.Green*255, 
				(r)=> { colorRGBA.Green = r/255; UpdateFromRGBA(); UpdateSliders(); },
				0, 255, 16, 1 ) {
					X = 83,
					Y = 107 + height * 1+1,
					Width = 256+2,
					Height = height-2,
					Border = 1,
					BackColor = new Color(0,0,0,64),
					SliderColor = new Color(0,255,0,255),
					ForeColor = new Color(0,0,0,160),
				};

			sliderBlue	=	new Slider( 
				Frames, 
				()=>colorRGBA.Blue*255, 
				(r)=> { colorRGBA.Blue = r/255; UpdateFromRGBA(); UpdateSliders(); },
				0, 255, 16, 1 ) {
					X = 83,
					Y = 107 + height * 2+1,
					Width = 256+2,
					Height = height-2,
					Border = 1,
					BackColor = new Color(0,0,0,64),
					SliderColor = new Color(0,0,255,255),
					ForeColor = new Color(0,0,0,160),
				};

			sliderAlpha	=	new Slider( 
				Frames, 
				()=>colorRGBA.Alpha*255, 
				(r)=> { colorRGBA.Alpha = r/255; UpdateFromRGBA(); UpdateSliders(); },
				0, 255, 16, 1 ) {
					X = 83,
					Y = 107 + height * 3+1,
					Width = 256+2,
					Height = height-2,
					Border = 1,
					BackColor = new Color(0,0,0,64),
					SliderColor = new Color(128,128,128,255),
					ForeColor = new Color(0,0,0,160),
				};


			sliderTemp = new Slider(
				Frames,
				()=> temperature,
				(t)=> { temperature = t; 
						targetColor = Temperature.GetColor((int)t);  
						UpdateFromColor(); 
						UpdateSliders(); 
						sliderTemp.SliderColor = targetColor; 
						sliderTemp.Text = t.ToString() + "K";
					},
					1000, 40000, 100, 1 ) {
					X = 83,
					Y = 107 + height * 4+1,
					Width = 256+2,
					Height = height-2,
					Border = 1,
					BackColor = new Color(0,0,0,64),
					SliderColor = new Color(256,256,256,255),
					ForeColor = new Color(0,0,0,160),
				};

			sliderSat = new Slider(
				Frames,
				()=> colorHSV.S * 100f,
				(v)=> { colorHSV.S = v/100f; UpdateFromHSV(); UpdateSliders(); },
				0, 100, 1, 1 ) {
					X = 267,
					Y = 2,
					Width = 30,
					Height = 102,
					Border = 1,
					BackColor = new Color(0,0,0,64),
					SliderColor = new Color(128,128,128,255),
					ForeColor = new Color(0,0,0,160),
					Vertical = true,
				};




			Add( sliderRed );
			Add( sliderGreen );
			Add( sliderBlue );
			Add( sliderAlpha );

			Add( sliderSat );
			Add( sliderTemp );

			UpdateFromColor();
			UpdateSliders();

		}

		private void ColorPicker_Missclick( object sender, EventArgs e )
		{
			Close();
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
			var rect	= ColorTheme.NormalFont.MeasureString( text );

			var frame = new Frame( Frames, x,y, rect.Width, rect.Height, text, Color.Zero );
			
			frame.Font			=	ColorTheme.NormalFont;
			frame.ForeColor		=	ColorTheme.TextColorNormal;
			frame.TextAlignment	=	Alignment.MiddleLeft;

			Add( frame );
		}

		
	}
}
