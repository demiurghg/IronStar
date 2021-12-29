﻿using Fusion.Core.Mathematics;
using Fusion.Core;
using Fusion.Core.Shell;
using Fusion.Core.Configuration;
using Fusion.Widgets.Advanced;
using IronStar.Editor.Manipulators;

namespace IronStar.Editor
{

	public enum AxisMode {
		Global,
		Local,
	}

	public enum SnapMode {
		None,
		Absolute,
		Relative,
	}

	public enum LayerState {
		Default,
		Frozen,
		Hidden,
	}


	/// <summary>
	/// World represents entire game state.
	/// </summary>
	[ConfigClass]
	public partial class MapEditor : GameComponent {

		[Config]
		[AECategory("Camera")]
		[AESlider(10,160,10,1)]
		static public float CameraFov { 
			get { return cameraFov; }
			set { cameraFov = MathUtil.Clamp( value, 10, 160 ); }
		}
		static float cameraFov = 90;

		[Config]
		[AECategory("Camera")]
		static public bool LockAzimuth { get; set; } = false;


		[Config]
		[AECategory("Snapping")]
		static public float MoveToolSnapValue { 
			get {
				return moveToolSnapValue;	
			}
			set {
				moveToolSnapValue = MathUtil.Clamp( value, 1/64.0f, 8.0f );
			}
		}
		static float moveToolSnapValue = 1.0f;


		[Config]
		[AECategory("Snapping")]
		static public float RotateToolSnapValue { 
			get {
				return rotateToolSnapValue;
			}
			set {
				rotateToolSnapValue = MathUtil.Clamp( value, 5, 90 );
			}
		}
		static float rotateToolSnapValue = 1.0f;


		[Config]
		[AECategory("Snapping")]
		static public AxisMode MoveAxisMode { get; set; } = AxisMode.Global;

		[Config]
		[AECategory("Snapping")]
		static public AxisMode RotateAxisMode { get; set; } = AxisMode.Global;

		[AECommand]
		public void ToggleSimulation ()
		{
			EnableSimulation = !EnableSimulation;
		}


		[AECategory("3D-View")]
		[AEDisplayName("Draw Grid")]
		public bool DrawGrid { get; set; } = true;


		[AECategory("Layers")]	public LayerState LayerEntities		{ get; set; }
		[AECategory("Layers")]	public LayerState LayerGeometry		{ get; set; }			
		[AECategory("Layers")]	public LayerState LayerLightSet		{ get; set; }
		[AECategory("Layers")]	public LayerState LayerLightProbes	{ get; set; }
		[AECategory("Layers")]	public LayerState LayerDecals		{ get; set; }
		[AECategory("Layers")]	public LayerState LayerSFX			{ get; set; }
			
		[AECommand]
		[AECategory("Layers")]
		[AEDisplayName("Enable All")]
		public void EnableAll ()
		{
			LayerEntities		=	LayerState.Default;	
			LayerGeometry		=	LayerState.Default;	
			LayerLightSet		=	LayerState.Default;	
			LayerLightProbes	=	LayerState.Default;
			LayerDecals			=	LayerState.Default;
			LayerSFX			=	LayerState.Default;
		}
			
		[AECommand]
		[AECategory("Layers")]
		[AEDisplayName("Freeze All")]
		public void FreezeAll ()
		{
			LayerEntities		=	LayerState.Frozen;	
			LayerGeometry		=	LayerState.Frozen;	
			LayerLightSet		=	LayerState.Frozen;	
			LayerLightProbes	=	LayerState.Frozen;
			LayerDecals			=	LayerState.Frozen;
			LayerSFX			=	LayerState.Frozen;
		}
			
		[AECommand]
		[AECategory("Layers")]
		[AEDisplayName("Hide All")]
		public void HideAll ()
		{
			LayerEntities		=	LayerState.Hidden;	
			LayerGeometry		=	LayerState.Hidden;	
			LayerLightSet		=	LayerState.Hidden;	
			LayerLightProbes	=	LayerState.Hidden;
			LayerDecals			=	LayerState.Hidden;
			LayerSFX			=	LayerState.Hidden;
		}


	}
}
