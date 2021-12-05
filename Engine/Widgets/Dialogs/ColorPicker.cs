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
using Fusion.Widgets.Binding;
using Fusion.Core;

namespace Fusion.Widgets.Dialogs 
{
	public partial class ColorPicker : Frame 
	{
		static ColorPicker colorPicker;

		static public void ShowDialog ( UIState ui, int x, int y, IValueBinding binding )
		{
			colorPicker = new ColorPicker( ui, binding );

			colorPicker.X	=	x;
			colorPicker.Y	=	y;

			ui.ShowDialog( colorPicker );
			colorPicker.ConstrainFrame(10);
		}


		const int DialogWidth	=	80 + 256 + 3 + 2*2;
		const int DialogHeight	=	165 + 4*7-1;


		Color targetColor;
		Color initColor;
		ColorBindingWrapper binding;

		ColorField colorField;

		Frame oldColorSample;
		Frame newColorSample;

		Color4		colorRGBA;
		HSVColor	colorHSV;

		Slider sliderRed	;
		Slider sliderGreen	;
		Slider sliderBlue	;
		Slider sliderAlpha	;
		Slider sliderSat	;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="fp"></param>
		private ColorPicker ( UIState ui, IValueBinding binding ) : base(ui)
		{
			if (binding.ValueType!=typeof(Color))
			{
				throw new ValueBindingException("ColorPicker does not support value type " + binding.ValueType.ToString());
			}

			this.binding		=	new ColorBindingWrapper(binding);
			this.initColor		=	this.binding.GetRGBValue();
			this.targetColor	=	initColor;

			Width	=	DialogWidth;
			Height	=	DialogHeight;	

			Padding			=	1;
			Border			=	1;
			BorderColor		=	ColorTheme.BorderColor;
			BackColor		=	ColorTheme.BackgroundColor;

			oldColorSample	=	AddColorButton(  2,   2, 80, 30, "", initColor, ()=> { targetColor = initColor; } );
			newColorSample	=	AddColorButton(  2,  32, 80, 30, "", initColor, null );

			this.Missclick +=ColorPicker_Missclick;

			colorField	=	new ColorField( ui, 80+3, 2, 180+2, 100+2, this.binding );
			
			Add( colorField );

			int height	=	ColorTheme.NormalFont.LineHeight;

			AddLabel( 2, 107 + height * 0-1, "Red"	);
			AddLabel( 2, 107 + height * 1-1, "Green" );
			AddLabel( 2, 107 + height * 2-1, "Blue"	);
			AddLabel( 2, 107 + height * 3-1, "Alpha"	);
										 
			AddLabel( 2, 107 + height * 4-1, "Temp, (K)" );

			sliderRed	=	new Slider( 
				ui, 
				this.binding.Red,
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
				ui, 
				this.binding.Green,
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
				ui, 
				this.binding.Blue,
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
				ui, 
				this.binding.Alpha,
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


			/*sliderTemp = new Slider(
				Frames,
				new DelegateBinding<float>(
					()=> temperature,
					(t)=> { temperature = t; 
							targetColor = Temperature.GetColor((int)t);  
							UpdateFromColor(ValueSetMode.Default); 
							UpdateSliders(); 
							sliderTemp.SliderColor = targetColor; 
							sliderTemp.Text = t.ToString() + "K";
						}
					),
					1000, 40000, 100, 1 ) {
					X = 83,
					Y = 107 + height * 4+1,
					Width = 256+2,
					Height = height-2,
					Border = 1,
					BackColor = new Color(0,0,0,64),
					SliderColor = new Color(256,256,256,255),
					ForeColor = new Color(0,0,0,160),
				}; */

			sliderSat = new Slider(
				ui,
				this.binding.Sat,
				0, 1, 1f/8f, 1f/128f ) {
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
		}

		private void ColorPicker_Missclick( object sender, EventArgs e )
		{
			Close();
		}


		protected override void DrawFrame( GameTime gameTime, SpriteLayer spriteLayer, int clipRectIndex )
		{
			sliderRed	.Text	=	binding.GetRGBValue().R.ToString();
			sliderGreen	.Text	=	binding.GetRGBValue().G.ToString();
			sliderBlue	.Text	=	binding.GetRGBValue().B.ToString();
			sliderAlpha	.Text	=	binding.GetRGBValue().A.ToString();

			var sat = (int)(binding.GetHSVColor().S * 100);
			sliderSat	.Text	=	sat.ToString();

			base.DrawFrame( gameTime, spriteLayer, clipRectIndex );
		}


		Frame AddColorButton ( int x, int y, int w, int h, string text, Color color, Action action )
		{
			var frame = new Frame( ui, x,y,w,h, text, color );
			
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

			var frame = new Frame( ui, x,y, rect.Width, rect.Height, text, Color.Zero );
			
			frame.Font			=	ColorTheme.NormalFont;
			frame.ForeColor		=	ColorTheme.TextColorNormal;
			frame.TextAlignment	=	Alignment.MiddleLeft;

			Add( frame );
		}
	}
}
