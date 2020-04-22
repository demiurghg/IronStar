﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Fusion.Core.Mathematics;


namespace Fusion.Engine.Imaging {
	public partial class GenericImageMips<TColor> {

		readonly GenericImage<TColor>[] images;


		public GenericImage<TColor> TopLevelMip 
		{
			get 
			{
				return images[0];
			}
		}


		public int Width
		{
			get { return TopLevelMip.Width; }
		}


		public int Height
		{
			get { return TopLevelMip.Height; }
		}


		public GenericImageMips( int width, int height, int mipCount, TColor fillColor )
		{
			if (width<0) throw new ArgumentOutOfRangeException("width < 0");
			if (height<0) throw new ArgumentOutOfRangeException("height < 0");
			if (mipCount<0) throw new ArgumentOutOfRangeException("mipCount < 1");

			var maxMip = Drivers.Graphics.ShaderResource.CalculateMipLevels( width, height );

			if (mipCount==0)
			{
				mipCount = maxMip;
			}

			mipCount = Math.Min( mipCount, maxMip );

			images = new GenericImage<TColor>[mipCount];

			for (int mip=0; mip<mipCount; mip++)
			{
				images[mip]	= new GenericImage<TColor>( width >> mip, height >> mip, fillColor );	
			}
		}


		public GenericImage<TColor> this[int mip] 
		{
			get 
			{
				if (mip<0) throw new ArgumentOutOfRangeException("mip < 0");
				if (mip>=images.Length) throw new ArgumentOutOfRangeException("mip >= max mips");
				return images[mip];
			}
		}


		public TColor this[int x, int y] {
			get {
				return TopLevelMip.Sample(x, y, false);
			}
			
			set {
				TopLevelMip.SetPixel(x,y, value, false);
			}	

		}


		public TColor this[Int2 xy] {
			get {
				return TopLevelMip.Sample(xy.X, xy.Y, false);
			}
			
			set {
				TopLevelMip.SetPixel(xy.X, xy.Y, value, false);
			}	

		}


		public static implicit operator GenericImage<TColor>(GenericImageMips<TColor> image) 
		{
			return image[0];
		}
	}

}
