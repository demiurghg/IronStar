using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Engine.Common;
using Fusion.Engine.Frames;
using Fusion.Engine.Graphics;
using System.Reflection;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Widgets;
using Fusion.Widgets.Dialogs;
using Fusion.Widgets.Binding;
using static Fusion.Widgets.Dialogs.AtlasSelector;
using Fusion.Engine.Frames.Layouts;

namespace Fusion.Widgets.Advanced
{
	public class AERotationAttribute : AEEditorAttribute 
	{
		public override Frame CreateEditor( AEPropertyGrid grid, string name, IValueBinding binding )
		{
			return new AERotationEditor( grid, name, binding );
		}
	}

	
	class AERotationEditor : AEBaseEditor 
	{
		readonly QuaternionBindingWrapper binding;

		Label	labelYaw;
		Label	labelPitch;
		Label	labelRoll;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="grid"></param>
		/// <param name="bindingInfo"></param>
		public AERotationEditor ( AEPropertyGrid grid, string name, IValueBinding binding ) : base(grid, name)
		{ 
			this.binding	=	new QuaternionBindingWrapper(binding);
				
			Width			=	grid.Width;
			Height			=	78;

			Layout			=	new PageLayout()
							.AddRow(23, 0.25f, 0.25f, 0.25f, 0.25f )
							.AddRow(17, 40, -1)
							.AddRow(17, 40, -1)
							.AddRow(17, 40, -1)
							;


			this.StatusChanged  +=	AEAtlasSelector_StatusChanged;

			labelYaw			=	new Label( Frames, 0,0,0,0, "") { TextAlignment = Alignment.MiddleRight, PaddingRight = 3 };
			labelPitch			=	new Label( Frames, 0,0,0,0, "") { TextAlignment = Alignment.MiddleRight, PaddingRight = 3 };
			labelRoll			=	new Label( Frames, 0,0,0,0, "") { TextAlignment = Alignment.MiddleRight, PaddingRight = 3 };

			Add( new Button( Frames,   "0", 0,0,0,0, () => MakeAngle(  0) ) );
			Add( new Button( Frames,  "90", 0,0,0,0, () => MakeAngle( 90) ) );
			Add( new Button( Frames, "180", 0,0,0,0, () => MakeAngle(180) ) );
			Add( new Button( Frames, "270", 0,0,0,0, () => MakeAngle(270) ) );
			Add( new Label( Frames, 0,0,0,0, "Yaw") );		Add( labelYaw	 );
			Add( new Label( Frames, 0,0,0,0, "Pitch") );	Add( labelPitch	 );
			Add( new Label( Frames, 0,0,0,0, "Roll") );		Add( labelRoll	 );

			Update(GameTime.Zero);
		}


		void MakeIdentity()
		{
			binding.SetQuaternion( Quaternion.Identity, ValueSetMode.Default );
		}


		void MakeAngle(float angle)
		{
			var yaw = MathUtil.DegreesToRadians(angle);
			binding.SetQuaternion( Quaternion.RotationYawPitchRoll(yaw, 0, 0), ValueSetMode.Default );
		}


		private void AEAtlasSelector_StatusChanged( object sender, StatusEventArgs e )
		{
			switch ( e.Status ) 
			{
				case FrameStatus.None:		ForeColor	=	ColorTheme.TextColorNormal; break;
				case FrameStatus.Hovered:	ForeColor	=	ColorTheme.TextColorHovered; break;
				case FrameStatus.Pushed:	ForeColor	=	ColorTheme.TextColorPushed; break;
			}
		}


		public override void RunLayout()
		{
			PaddingLeft			=	Width / 2;

			base.RunLayout();

			/*buttonIdentity.X		=	Width/2;
			buttonIdentity.Width	=	64;
			buttonIdentity.Height	=	32;

			labelYaw.X		=	a.Yaw.Degrees.ToString();
			labelPitch.Text	=	a.Pitch.Degrees.ToString();
			labelRoll.Text	=	a.Roll.Degrees.ToString();*/
		}



		protected override void DrawFrame( GameTime gameTime, SpriteLayer spriteLayer, int clipRectIndex )
		{
			var q = binding.GetQuaternion();
			var a = EulerAngles.RotationQuaternion(q);

			labelYaw.Text	=	Math.Round( a.Yaw.Degrees   , 2 ).ToString("0.00");
			labelPitch.Text	=	Math.Round( a.Pitch.Degrees , 2 ).ToString("0.00");
			labelRoll.Text	=	Math.Round( a.Roll.Degrees  , 2 ).ToString("0.00");

			base.DrawFrame( gameTime, spriteLayer, clipRectIndex );
		}
	}
}
