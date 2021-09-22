using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Fusion.Core.Mathematics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = Fusion.Core.Mathematics.Color;

namespace Fusion.Engine.Imaging 
{
	public static class ImageText
	{
		static Image<Color> fontImage;

		static ImageText()
		{
			using ( var stream = new MemoryStream( Properties.Resources.conchars ) )
			{
				fontImage   =   ImageLib.LoadTga( stream );
			}
		}


		public static void DrawText ( Image<Color> target, int x, int y, string text )
		{
			for (int i=0; i<text.Length; i++) {

				var ch		=	((int)text[i]) & 0xFF;

				int srcX	=	(ch % 16) * 8;
				int srcY	=	(ch / 16) * 8;
				int dstX	=	x + i * 8;
				int dstY	=	y;

				fontImage.CopySubImageTo( srcX, srcY, 9,8, dstX, dstY, target );
			}
		}

	}
}
