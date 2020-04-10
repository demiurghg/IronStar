﻿using System;
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

		private bool			dirty	=	true;
		GpuData.CAMERA		constData;
		ConstantBuffer			constBuffer;
		BoundingFrustum			boundingFrustum;

		public readonly string	Name;


		/// <summary>
		/// 
		/// </summary>
		public Camera ( RenderSystem rs, string name )
		{
			Name		=	name;

			constData	=	new GpuData.CAMERA();
			constBuffer	=	new ConstantBuffer( rs.Device, typeof(GpuData.CAMERA) );

			SetView(Matrix.Identity);
			SetProjection(Matrix.Identity);
		}


		protected override void Dispose( bool disposing )
		{
			if (disposing)
			{
				SafeDispose( ref constBuffer );
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

			//	decompose perspective matrix :

			float m43	=	inputProjMatrix.M43;
			float m33	=	inputProjMatrix.M33;
			float zn	=	Math.Abs( m43 / m33 );
			float zf	=	zn * m43 / ( m43 + zn );
			float w		=	2 * zn / inputProjMatrix.M11;
			float h		=	2 * zn / inputProjMatrix.M11;


			bool isPerspective				=	inputProjMatrix.M44 == 0;
			var viewProjMatrix				=	ViewMatrix * ProjectionMatrix;

			boundingFrustum					=	new BoundingFrustum( inputViewMatrix * inputProjMatrix );	

			constData.View					=	ViewMatrix;
			constData.Projection			=	ProjectionMatrix;

			constData.ViewProjection		=	viewProjMatrix;
			constData.ViewInverted			=	cameraMatrix;
			
			constData.CameraForward			=	new Vector4( cameraMatrix.Forward	, 0 );
			constData.CameraRight			=	new Vector4( cameraMatrix.Right		, 0 );
			constData.CameraUp				=	new Vector4( cameraMatrix.Up		, 0 );
			constData.CameraPosition		=	new Vector4( cameraMatrix.TranslationVector	, 1 );

			constData.FarDistance			=	isPerspective ? zf : 1;
			constData.LinearizeDepthBias	=	1 / zn;
			constData.LinearizeDepthScale	=	1 / zf - 1 / zn;

			constData.CameraTangentX		=	w / zn / 2.0f;
			constData.CameraTangentY		=	h / zn / 2.0f;

			constBuffer.SetData( ref constData );
			
			dirty	=	false;
		}


		public ConstantBuffer CameraData
		{
			get 
			{ 
				UpdateCameraStateIfNecessary();
				return constBuffer; 
			}
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
