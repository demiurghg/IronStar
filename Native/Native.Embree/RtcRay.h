#pragma once

namespace Native {
	namespace Embree {

		public ref class RtcRay
		{
		public:
			float X, Y, Z;
			float Dx, Dy, Dz;
			float TNear;
			float TFar;
			float Time;
			int Mask;

		public:
			float Nx, Ny, Nz;
			float U, V;
			unsigned GeometryId;
			unsigned TriangleId;
			unsigned InstanceId;

		public:

			RtcRay()
			{
			}

			RtcRay(float x, float y, float z, float dx, float dy, float dz, float tnear, float tfar, float time, int mask)
			{
				X = x;
				Y = y;
				Z = z;
				Dx = dx;
				Dy = dy;
				Dz = dz;
				TNear = tnear;
				TFar = tfar;
				Time = time;
				Mask = mask;
				Nx = Ny = Nz = 0;
				U = V = 0;
				GeometryId = RTC_INVALID_GEOMETRY_ID;
				TriangleId = RTC_INVALID_GEOMETRY_ID;
				InstanceId = RTC_INVALID_GEOMETRY_ID;
			}


			RtcRay(float x, float y, float z, float dx, float dy, float dz )
			{
				X = x;
				Y = y;
				Z = z;
				Dx = dx;
				Dy = dy;
				Dz = dz;
				TNear = 0;
				TFar = INFINITY;
				Time = 0;
				Mask = 0xFFFFFFFF;
				Nx = Ny = Nz = 0;
				U = V = 0;
				GeometryId = RTC_INVALID_GEOMETRY_ID;
				TriangleId = RTC_INVALID_GEOMETRY_ID;
				InstanceId = RTC_INVALID_GEOMETRY_ID;
			}
		};
	}
}
