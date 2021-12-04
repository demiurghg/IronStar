using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Engine.Frames;
using Fusion.Core.Extensions;

namespace Fusion.Engine.Graphics.GUI
{
	public class Gui
	{
		public Frame	Root;
		public Matrix	Transform;

		public float	DotsPerUnit = 128;

		public bool		Visible;

		public TimeSpan	GlitchTimer;
		public uint		GlitchSeed;

		public float	Vignette;
		public float	Saturation;
		public float	Interference;
		public float	Noise;
		public float	Glitch;

		public float	Abberation;
		public float	Scanline;


		public Gui()
		{
			UpdateGlitch(GameTime.Zero);
		}


		public BoundingBox ComputeBounds()
		{
			float halfWidth		=	0.5f * Root.Width / DotsPerUnit;
			float halfHeight	=	0.5f * Root.Height / DotsPerUnit;

			var vertices = new Vector3[] 
			{
				Vector3.TransformCoordinate( new Vector3(  halfWidth,  halfHeight, 0 ), Transform ),
				Vector3.TransformCoordinate( new Vector3(  halfWidth, -halfHeight, 0 ), Transform ),
				Vector3.TransformCoordinate( new Vector3( -halfWidth, -halfHeight, 0 ), Transform ),
				Vector3.TransformCoordinate( new Vector3( -halfWidth,  halfHeight, 0 ), Transform ),
			};

			return BoundingBox.FromPoints( vertices );
		}

		
		public void UpdateGlitch(GameTime gameTime)
		{
			GlitchTimer -= gameTime.Elapsed;

			if (GlitchTimer<=TimeSpan.Zero)
			{
				GlitchTimer = TimeSpan.FromMilliseconds(Math.Max(0,MathUtil.Random.GaussDistribution(200,100)));
				GlitchSeed = (uint)MathUtil.Random.Next();
			}
		}

		/*
		public Size2 ComputeLodSize()
		{
			var w = Root.Width;
			var h = Root.Height;

			return new Size2( w >> Lod, h >> Lod );
		}
		*/
	}
}
