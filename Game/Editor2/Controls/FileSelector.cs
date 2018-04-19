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
	partial class FileSelector : Frame {

		static FileSelector fileSelector;

		const int DialogWidth	= 640;
		const int DialogHeight	= 480;


		static public void ShowDialog ( FrameProcessor fp, string oldFileName, Action<string> setFileName )
		{
			fileSelector = new FileSelector( fp, oldFileName, setFileName );

			fp.RootFrame.Add( fileSelector );
			fp.ModalFrame = fileSelector;

			FrameUtils.CenterFrame( fileSelector );
		}



		string oldFileName;
		Action<string> setFileName;



		/// <summary>
		/// 
		/// </summary>
		/// <param name="fp"></param>
		private FileSelector ( FrameProcessor fp, string oldFileName, Action<string> setFileName ) : base(fp)
		{
			this.oldFileName	=	oldFileName;
			this.setFileName	=	setFileName;

			Width	=	DialogWidth;
			Height	=	DialogHeight;

			Missclick +=FileSelector_Missclick;

		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void FileSelector_Missclick( object sender, EventArgs e )
		{
			Frames.RootFrame.Remove( this );
			Frames.ModalFrame = null;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="w"></param>
		/// <param name="h"></param>
		/// <param name="text"></param>
		/// <param name="color"></param>
		/// <param name="action"></param>
		/// <returns></returns>
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
