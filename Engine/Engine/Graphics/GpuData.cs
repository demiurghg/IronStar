using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace Fusion.Engine.Graphics
{
	public static class GpuData
	{
		[StructLayout(LayoutKind.Sequential, Pack=4)]
		public struct CAMERA 
		{														  
			public Matrix	Projection;            
			public Matrix	View;     
			
			public Matrix	ViewProjection;   
			public Matrix	ViewInverted;

			public Vector4	CameraForward;
			public Vector4	CameraRight;
			public Vector4	CameraUp;
			public Vector4	CameraPosition;

			public float	LinearizeDepthScale;
			public float	LinearizeDepthBias;
			public float	FarDistance;
			public float	Pad0;

			public float	CameraTangentX;
			public float	CameraTangentY;
			public float	Pad1;
			public float	Pad2;
		}


		[StructLayout(LayoutKind.Sequential, Pack=4)]
		public struct DIRECT_LIGHT 
		{
			public Vector4	DirectLightDirection;
			public Color4	DirectLightIntensity;

			public float	DirectLightAngularSize;
			public float	Pad0;
			public float	Pad1;
			public float	Pad2;
		}
	}
}
