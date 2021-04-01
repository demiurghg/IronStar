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
using Fusion.Widgets.Binding;

namespace Fusion.Widgets.Advanced
{
	public abstract class AEDropDownValueProviderAttribute : AEEditorAttribute 
	{
		protected abstract string[] GetValues(Game game);

		public override Frame CreateEditor( AEPropertyGrid grid, string name, IValueBinding binding )
		{
			return new AEDropDown( grid, name, GetValues(grid.Game), binding ); 
		}
	}


	class AEDropDown : AEBaseEditor 
	{
		DropDown dropDown;

		public AEDropDown ( AEPropertyGrid grid, string name, IEnumerable<string> values, IValueBinding binding ) : base(grid, name)
		{ 
			Width			=	grid.Width;
			Height			=	ComputeItemHeight();

			this.StatusChanged +=AEDropDown_StatusChanged;

			dropDown		=	new DropDown( Frames, values, binding ) 
			{
				PaddingLeft		=	AEPropertyGrid.HorizontalPadding,
				PaddingRight	=	AEPropertyGrid.HorizontalPadding,
				PaddingTop		=	AEPropertyGrid.VerticalPadding,
				PaddingBottom	=	AEPropertyGrid.VerticalPadding,
			};

			Add( dropDown );

			Update(new GameTime());
		}



		private void AEDropDown_StatusChanged( object sender, StatusEventArgs e )
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
			base.RunLayout();

			dropDown.X		=	Width/2;
			dropDown.Width	=	Width/2;
			dropDown.Height	=	ComputeItemHeight();
		}


		protected override void DrawFrame( GameTime gameTime, SpriteLayer spriteLayer, int clipRectIndex )
		{
			base.DrawFrame( gameTime, spriteLayer, clipRectIndex );
		}
	}
}
