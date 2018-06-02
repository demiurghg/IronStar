using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Engine.Server;
using Fusion.Engine.Client;
using Fusion.Core.Extensions;
using IronStar.SFX;
using Fusion.Core.IniParser.Model;
using Fusion.Engine.Graphics;
using IronStar.Mapping;
using Fusion.Build;
using BEPUphysics;
using IronStar.Core;
using IronStar.Editor2.Controls;
using IronStar.Editor2.Manipulators;
using Fusion.Engine.Frames;
using Fusion.Core.Shell;
using Fusion.Core.Configuration;

namespace IronStar.Editor2 {

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
	public partial class MapEditor : GameComponent {

		[Config]
		[AECategory("Camera")]
		[AEValueRange(10,160,10,1)]
		public float CameraFov { 
			get { return cameraFov; }
			set { cameraFov = MathUtil.Clamp( value, 10, 160 ); }
		}
		float cameraFov = 90;

		[Config]
		[AECategory("Camera")]
		public bool LockAzimuth { get; set; } = false;


		[Config]
		[AECategory("Move Tool")]
		public bool MoveToolSnapEnable { get; set; } = true;


		[Config]
		[AECategory("Move Tool")]
		public float MoveToolSnapValue { 
			get {
				return moveToolSnapValue;	
			}
			set {
				moveToolSnapValue = MathUtil.Clamp( value, 1/64.0f, 8.0f );
			}
		}
		float moveToolSnapValue = 1.0f;


		[Config]
		[AECategory("Rotate Tool")]
		public bool RotateToolSnapEnable { get; set; } = true;


		[Config]
		[AECategory("Rotate Tool")]
		public float RotateToolSnapValue { 
			get {
				return rotateToolSnapValue;
			}
			set {
				rotateToolSnapValue = MathUtil.Clamp( value, 5, 90 );
			}
		}
		float rotateToolSnapValue = 1.0f;


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
