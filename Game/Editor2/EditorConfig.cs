using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Engine.Graphics;
using Fusion.Core.Mathematics;
using Fusion.Core.Configuration;
using Fusion.Engine.Common;
using Fusion;
using System.ComponentModel;
using Fusion.Core.Shell;

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

	public class EditorConfig {

		readonly MapEditor editor;

		public EditorConfig ( MapEditor editor )
		{
			this.editor	=	editor;
		}



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
			editor.EnableSimulation = !editor.EnableSimulation;
		}

	}
}
