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
		const float ENGAGEMENT_DISTANCE = 10.0f;

		public readonly UIState	UI;
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

		public Frame Root { get { return UI.RootFrame; } }


		public Gui( UIState ui )
		{
			this.UI = ui;
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


		public float ComputeDiagonal()
		{
			float w		=	Root.Width / DotsPerUnit;
			float h		=	Root.Height / DotsPerUnit;

			return new Vector2(w,h).Length();
		}


		public bool IsUserEngaged( Ray viewRay, out int x, out int y )
		{
			x = Root.Width / 2;
			y = Root.Height / 2;

			var engageDistance	=	ENGAGEMENT_DISTANCE;
			var distance		=	Vector3.Distance( viewRay.Position, Transform.TranslationVector );

			if (distance < engageDistance)
			{
				var screenPlane		=	new Plane( Transform.TranslationVector, Transform.Forward );
				var invTransform	=	Matrix.Invert( Transform );
						
				Vector3 hitPoint;
						
				if (viewRay.Intersects(ref screenPlane, out hitPoint))
				{
					var projection	=	Vector3.TransformCoordinate( hitPoint, invTransform );

					int w = Root.Width;
					int h = Root.Height;
					x = (int)( projection.X * DotsPerUnit) + w / 2;
					y = (int)(-projection.Y * DotsPerUnit) + h / 2;

					if (x>=0 && x<Root.Width && y>=0 && y<Root.Height)
					{
						Root.Game.RenderSystem.RenderWorld.Debug.DrawPoint( hitPoint, 0.3f, Color.Red );
						return true;
					}
				}
			}

			return false;
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
