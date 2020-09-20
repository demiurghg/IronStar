using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Core;
using Fusion.Drivers.Graphics;
using Fusion.Engine.Common;

namespace Fusion.Engine.Graphics {
	
	public class Decal {

		/// <summary>
		/// Decal basis matrix
		/// </summary>
		public Matrix DecalMatrix;

		public Matrix DecalMatrixInverse;

		/// <summary>
		/// Image index in decal atlas
		/// </summary>
		public RectangleF ImageRectangle;

		/// <summary>
		/// Image index in decal atlas
		/// </summary>
		public Size2 ImageSize;

		/// <summary>
		/// Decal emission intensity
		/// </summary>
		public Color4 Emission = Color4.Zero;

		/// <summary>
		/// Decal base color
		/// </summary>
		public Color4 BaseColor = new Color4(1,1,1,1);

		/// <summary>
		/// Decal roughness
		/// </summary>
		public float Roughness = 0.5f;

		/// <summary>
		/// Decal meatllic
		/// </summary>
		public float Metallic = 0.5f;

		/// <summary>
		/// Color blend factor [0,1]
		/// </summary>
		public float ColorFactor = 1.0f;

		/// <summary>
		/// Roughmess and specular blend factor [0,1]
		/// </summary>
		public float SpecularFactor = 1.0f;

		/// <summary>
		/// Normalmap blend factor [-1,1]
		/// </summary>
		public float NormalMapFactor = 1.0f;

		/// <summary>
		/// 
		/// </summary>
		public float FalloffFactor = 0.0f;

		/// <summary>
		///	Instance group to apply decal to.
		/// </summary>
		public InstanceGroup Group = InstanceGroup.Static;

		/// <summary>
		/// Defines characteristic size of the decal, used to cull reletively small decals
		/// </summary>
		public float CharacteristicSize = 1;

		internal bool	Visible;
		internal Int3	MinExtent;
		internal Int3	MaxExtent;


		public Vector4 GetScaleOffset ()
		{
			float ax = ImageRectangle.Width;
			float ay = ImageRectangle.Height;
			float bx = ImageRectangle.Left;
			float by = ImageRectangle.Top;

			float x		=	0.5f * ax;
			float y		=  -0.5f * ay;
			float z		=   0.5f * ax + bx;
			float w		=	0.5f * ay + by;

			return new Vector4(x,y,z,w);
		}



		/// <summary>
		/// 
		/// </summary>
		public Decal ()
		{
		}
	}
}
