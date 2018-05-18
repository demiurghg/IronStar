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
	}
}
