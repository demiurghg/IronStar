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
using System.Diagnostics;
using Fusion.Core.Extensions;
using Fusion.Engine.Graphics.Ubershaders;

namespace Fusion.Engine.Graphics {

	partial class Fog {

		[StructLayout(LayoutKind.Sequential, Size=1024)]
		[ShaderStructure]
		struct PARAMS {
			public Matrix	View;
			public Matrix	Projection;
			public Matrix	ViewProjection;
			public Matrix	CameraMatrix;

			public Matrix	InvertedViewMatrix;

			public Matrix	CascadeViewProjection0	;
			public Matrix	CascadeViewProjection1	;
			public Matrix	CascadeViewProjection2	;
			public Matrix	CascadeViewProjection3	;
			public Vector4	CascadeScaleOffset0		;
			public Vector4	CascadeScaleOffset1		;
			public Vector4	CascadeScaleOffset2		;
			public Vector4	CascadeScaleOffset3		;

			public Vector4	DirectLightDirection;
			public Color4	DirectLightIntensity;

			public Vector4	CameraForward;
			public Vector4	CameraRight;
			public Vector4	CameraUp;
			public Vector4	CameraPosition;

			public float	CameraTangentX;
			public float	CameraTangentY;
		} 
	}
}
