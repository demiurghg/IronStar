using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.ECS;
using Fusion.Engine.Graphics;

namespace IronStar.ECSGraphics
{
	public class BillboardComponent : IComponent
	{
		public ParticleFX	Effect;

		public Color		Color;

		public float		Alpha;

		public float		Exposure;

		public float		Roughness;

		public float		Metallic;

		public float		Scattering;

		public uint			MaterialERMS;

		public float		IntensityEV;

		public float		Size;

		public float		Rotation;

		public string		ImageName;
	}
}
