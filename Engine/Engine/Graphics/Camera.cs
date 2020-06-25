using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Drivers.Graphics;
using Fusion.Drivers.Graphics.Display;
using Fusion.Drivers.Input;
using Fusion.Engine.Common;
using Fusion.Core;

namespace Fusion.Engine.Graphics 
{
	/// <summary>
	/// --------------------------------
	/// Right-handed perspective matrix
	/// --------------------------------
	/// 2*zn/w  0       0              0
	/// 0       2*zn/h  0              0
	/// 0       0       zf/(zn-zf)    -1
	/// 0       0       zn*zf/(zn-zf)  0
	/// --------------------------------
	/// Left-handed perspective matrix
	/// --------------------------------
	/// 2*zn/w  0       0              0
	/// 0       2*zn/h  0              0
	/// 0       0       zf/(zf-zn)     1
	/// 0       0       zn*zf/(zn-zf)  0
	/// --------------------------------
	/// </summary>
	public class Camera : DisposableBase, ICameraDataProvider 
	{
		private Matrix	inputViewMatrix;
		private Matrix	inputProjMatrix;

		private Matrix	cameraMatrix;

		private bool		dirty	=	true;
		GpuData.CAMERA		cameraData;
		ConstantBuffer		cbCamera;
		ConstantBuffer		cbPlanes;
		BoundingFrustum		boundingFrustum;
		Plane[]				frustumPlanes;

		Matrix				historyViewProjection;

		public readonly string	Name;
		public uint frameCounter = 0;

		/// <summary>
		/// 
		/// </summary>
		public Camera ( RenderSystem rs, string name )
		{
			Name		=	name;

			frustumPlanes	=	new Plane[6];

			cameraData	=	new GpuData.CAMERA();
			cbPlanes	=	new ConstantBuffer( rs.Device, typeof(Plane), 6 );

			cameraData	=	new GpuData.CAMERA();
			cbCamera	=	new ConstantBuffer( rs.Device, typeof(GpuData.CAMERA) );

			SetView(Matrix.Identity);
			SetProjection(Matrix.Identity);
		}


		protected override void Dispose( bool disposing )
		{
			if (disposing)
			{
				SafeDispose( ref cbCamera );
				SafeDispose( ref cbPlanes );
			}

			base.Dispose( disposing );
		}



		public void SetView ( Matrix viewMatrix )
		{
			dirty			=	true;
			inputViewMatrix	=	viewMatrix;
			cameraMatrix	=	Matrix.Invert( viewMatrix );
		}


		public void SetProjection ( Matrix projMatrix )
		{
			dirty			=	true;
			inputProjMatrix	=	projMatrix;
		}



		void UpdateCameraStateIfNecessary()
		{
			if (!dirty) 
			{
				return;
			}

			frameCounter++;
			//	decompose perspective matrix :

			float m43	=	inputProjMatrix.M43;
			float m33	=	inputProjMatrix.M33;
			float zn	=	Math.Abs( m43 / m33 );
			float zf	=	zn * m43 / ( m43 + zn );
			float w		=	2 * zn / inputProjMatrix.M11;
			float h		=	2 * zn / inputProjMatrix.M22;


			bool isPerspective				=	inputProjMatrix.M44 == 0;
			var viewProjMatrix				=	ViewMatrix * ProjectionMatrix;

			boundingFrustum					=	new BoundingFrustum( inputViewMatrix * inputProjMatrix );	

			cameraData.View					=	ViewMatrix;
			cameraData.Projection			=	ProjectionMatrix;

			cameraData.ViewProjection		=	viewProjMatrix;
			cameraData.ViewInverted			=	cameraMatrix;
			
			cameraData.CameraForward			=	new Vector4( cameraMatrix.Forward	, 0 );
			cameraData.CameraRight			=	new Vector4( cameraMatrix.Right		, 0 );
			cameraData.CameraUp				=	new Vector4( cameraMatrix.Up		, 0 );
			cameraData.CameraPosition		=	new Vector4( cameraMatrix.TranslationVector	, 1 );

			cameraData.ReprojectionMatrix	=	ComputeReprojectionMatrix();

			cameraData.FarDistance			=	isPerspective ? zf : 1;

			if (isPerspective)
			{
				cameraData.LinearizeDepthBias	=	1 / zn;
				cameraData.LinearizeDepthScale	=	1 / zf - 1 / zn;
				cameraData.CameraTangentX		=	w / zn / 2.0f;
				cameraData.CameraTangentY		=	h / zn / 2.0f;
			}
			else
			{
				cameraData.LinearizeDepthBias	=	0;
				cameraData.LinearizeDepthScale	=	1;
				cameraData.CameraTangentX		=	0;
				cameraData.CameraTangentY		=	0;
			}


			for (int i=0; i<6; i++)
			{
				var plane			=	boundingFrustum.GetPlane(i);
				frustumPlanes[i]	=	plane * (-1);
			}

			cbPlanes.SetData( frustumPlanes );
			cbCamera.SetData( ref cameraData );
			
			dirty	=	false;
		}


		Matrix ComputeReprojectionMatrix()
		{
			return historyViewProjection;
			//var currentVP = ViewMatrix * ProjectionMatrix;
			//var historyVP = historyViewProjection;
			//return Matrix.Invert( currentVP ) * historyVP;
		}


		public ConstantBuffer CameraData
		{
			get 
			{ 
				UpdateCameraStateIfNecessary();
				return cbCamera; 
			}
		}

		public ConstantBuffer FrustumPlanes
		{
			get 
			{ 
				UpdateCameraStateIfNecessary();
				return cbPlanes; 
			}
		}


		public void UpdateHistory( GameTime gameTime )
		{
			historyViewProjection = ViewMatrix * ProjectionMatrix;
			dirty = true;
		}


		/*-----------------------------------------------------------------------------------------------
		 * 
		 *	Helper functions :
		 * 
		-----------------------------------------------------------------------------------------------*/

		public void LookAt ( Vector3 origin, Vector3 target, Vector3 up )
		{
			LookAtRH( origin, target, up );
		}

		

		public void LookAtRH ( Vector3 origin, Vector3 target, Vector3 up )
		{
			SetView( Matrix.LookAtRH( origin, target, up ) );
		}

		

		public void LookAtLH ( Vector3 origin, Vector3 target, Vector3 up )
		{
			SetView( Matrix.LookAtLH( origin, target, up ) );
		}

		

		public void SetPerspective( float width, float height, float near, float far )
		{
			SetProjection( Matrix.PerspectiveOffCenterRH( -width/2, width/2, -height/2, height/2, near, far ) );
		}



		public void SetPerspectiveFov( float fov, float near, float far, float aspectRatio )
		{
			float width		=	near * (float)Math.Tan( fov/2 ) * 2;
			float height	=	width / aspectRatio;
			SetPerspective( width, height, near, far );
		}



		public void SetupCameraCubeFaceLH ( Vector3 origin, CubeFace cubeFace, float near, float far )
		{
			Matrix view = Matrix.Identity;
			Matrix proj = Matrix.Identity;

			switch (cubeFace) {
				case CubeFace.FacePosX : LookAtLH( origin,  Vector3.UnitX + origin, Vector3.UnitY ); break;
				case CubeFace.FaceNegX : LookAtLH( origin, -Vector3.UnitX + origin, Vector3.UnitY ); break;
				case CubeFace.FacePosY : LookAtLH( origin, -Vector3.UnitY + origin,-Vector3.UnitZ ); break;
				case CubeFace.FaceNegY : LookAtLH( origin,  Vector3.UnitY + origin, Vector3.UnitZ ); break;
				case CubeFace.FacePosZ : LookAtLH( origin, -Vector3.UnitZ + origin, Vector3.UnitY ); break;
				case CubeFace.FaceNegZ : LookAtLH( origin,  Vector3.UnitZ + origin, Vector3.UnitY ); break;
			}
			
			SetPerspective( 2*near, 2*near, near, far );
		}



		public void SetupCameraCubeFaceRH ( Vector3 origin, CubeFace cubeFace, float near, float far )
		{
			Matrix view = Matrix.Identity;
			Matrix proj = Matrix.Identity;

			switch (cubeFace) {
				case CubeFace.FacePosX : LookAtRH( origin,  Vector3.UnitX + origin, Vector3.UnitY ); break;
				case CubeFace.FaceNegX : LookAtRH( origin, -Vector3.UnitX + origin, Vector3.UnitY ); break;
				case CubeFace.FacePosY : LookAtRH( origin, -Vector3.UnitY + origin,-Vector3.UnitZ ); break;
				case CubeFace.FaceNegY : LookAtRH( origin,  Vector3.UnitY + origin, Vector3.UnitZ ); break;
				case CubeFace.FacePosZ : LookAtRH( origin, -Vector3.UnitZ + origin, Vector3.UnitY ); break;
				case CubeFace.FaceNegZ : LookAtRH( origin,  Vector3.UnitZ + origin, Vector3.UnitY ); break;
			}
			
			SetPerspective( 2*near, 2*near, near, far );
		}



		public void SetupCameraCubeFaceLH ( Matrix basis, CubeFace cubeFace, float near, float far )
		{
			Matrix view = Matrix.Identity;
			Matrix proj = Matrix.Identity;
			var origin	= basis.TranslationVector;
			var unitX	= basis.Right;
			var unitY	= basis.Up;
			var unitZ	= basis.Backward;

			switch (cubeFace) {
				case CubeFace.FacePosX : LookAtLH( origin,  unitX + origin, unitY ); break;
				case CubeFace.FaceNegX : LookAtLH( origin, -unitX + origin, unitY ); break;
				case CubeFace.FacePosY : LookAtLH( origin, -unitY + origin,-unitZ ); break;
				case CubeFace.FaceNegY : LookAtLH( origin,  unitY + origin, unitZ ); break;
				case CubeFace.FacePosZ : LookAtLH( origin, -unitZ + origin, unitY ); break;
				case CubeFace.FaceNegZ : LookAtLH( origin,  unitZ + origin, unitY ); break;
			}
			
			SetPerspective( 2*near, 2*near, near, far );
		}



		public Matrix ViewMatrix 
		{
			get { return inputViewMatrix; }
			set { SetView( value ); }
		}


		public Matrix ProjectionMatrix
		{
			get { return inputProjMatrix; }
			set { SetProjection( value ); }
		}


		public Matrix CameraMatrix
		{
			get { return cameraMatrix; }
		}


		public Vector3 CameraPosition
		{
			get { return cameraMatrix.TranslationVector; }
		}


		public Vector4 GetCameraPosition4 ( StereoEye stereoEye )
		{
			return new Vector4(cameraMatrix .TranslationVector, 1);
		}


		public BoundingFrustum Frustum 
		{
			get 
			{
				return new BoundingFrustum( inputViewMatrix * inputProjMatrix );
			}
		}
	}
}
