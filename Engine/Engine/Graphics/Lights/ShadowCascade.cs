using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Configuration;
using Fusion.Engine.Common;
using Fusion.Drivers.Graphics;
using System.Runtime.InteropServices;
using Fusion.Build.Mapping;
using Fusion.Engine.Graphics.Lights;

namespace Fusion.Engine.Graphics 
{
	public class ShadowCascade : IShadowProvider
	{
		public readonly int SizeInTexels;
		public readonly int Index;
		public readonly Color Color;
		readonly int lod;

		Rectangle shadowRegion = new Rectangle(0,0,0,0);
		Vector4 regionScaleTranslate;


		public ShadowCascade ( int index, int sizeInTexels, int lod, Color color )
		{
			this.Index			=	index;
			this.SizeInTexels	=	sizeInTexels;
			this.Color			=	color;
			this.lod			=	lod;
		}


		public bool IsVisible {	get { return true; } }

		public int ShadowLod { get { return lod; } }

		public bool IsShadowDirty { get { return true; } set {} }
		
		public int SkipCounter { get; set; } = 0;

		public RenderList ShadowCasters { get { return shadowCasters; } }

		public string ShadowMaskName { get { return null; } }

		readonly RenderList shadowCasters = new RenderList();

		public Matrix ViewMatrix { get; set; }

		public Matrix ProjectionMatrix { get; set; }

		public Matrix ShadowViewProjection { get; set; }

		public Matrix ComputeGradientMatrix () 
		{
			var matrix	=	ViewMatrix * ProjectionMatrix;
				matrix	=	Matrix.Invert	( matrix );
				matrix	=	Matrix.Transpose( matrix );

			var size	=	(float)SizeInTexels;

				matrix	=	matrix * Matrix.Scaling( -2.0f/size, 2.0f/size, 1 );

			return matrix;
		}


		public Rectangle ShadowRegion 
		{ 
			get { return shadowRegion; }
		}

		public Vector4 RegionScaleTranslate
		{
			get { return regionScaleTranslate; }
		}

		public void SetShadowRegion( Rectangle region, int shadowMapSize )
		{
			shadowRegion			=	region;
			regionScaleTranslate	=	shadowRegion.GetMadOpScaleOffsetOffCenterProjectToNDC( shadowMapSize, shadowMapSize );
		}
	}
}
