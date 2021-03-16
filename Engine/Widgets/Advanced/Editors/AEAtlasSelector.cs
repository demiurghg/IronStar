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

namespace Fusion.Widgets.Advanced
{
	public class AEAtlasImageAttribute : AEEditorAttribute 
	{
		public readonly string AtlasName;

		public AEAtlasImageAttribute( string atlasName )
		{
			AtlasName = atlasName;
		}

		public override Frame CreateEditor( AEPropertyGrid grid, string name, IValueBinding binding )
		{
			return new AEAtlasSelector( grid, name, AtlasName, binding );
		}
	}

	class AEAtlasSelector : AEBaseEditor 
	{
		readonly StringBindingWrapper binding;
		readonly string atlasName;

		const int imageSize = 64;

		AtlasButton atlasButton;
		TextureAtlas textureAtlas;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="grid"></param>
		/// <param name="bindingInfo"></param>
		public AEAtlasSelector ( AEPropertyGrid grid, string name, string atlasName, IValueBinding binding ) : base(grid, name)
		{ 
			this.atlasName	=	atlasName;
			this.binding	=	new StringBindingWrapper(binding);
				
			Width			=	grid.Width;
			Height			=	imageSize;

			this.StatusChanged  +=AEAtlasSelector_StatusChanged;

			if (!Frames.Game.Content.TryLoad( atlasName, out textureAtlas ))
			{
				Log.Warning("Failed to load atlas : {0}", atlasName);
			}

			atlasButton				=	new AtlasButton( Frames, textureAtlas, this.binding.GetValue(), imageSize );
			atlasButton.Border		=	1;

			atlasButton.Click +=AtlasButton_Click;

			Add( atlasButton );

			Update(GameTime.Zero);
		}


		private void AtlasButton_Click( object sender, MouseEventArgs e )
		{
			var button	=	(Frame)sender;
			var rect	=	button.GlobalRectangle;

			ShowDialog( Frames, atlasName, binding.GetValue(), (s) => binding.SetValue(s, ValueSetMode.Default) );
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
			base.RunLayout();

			atlasButton.X		=	Width/2;
			atlasButton.Width	=	imageSize;
			atlasButton.Height	=	imageSize;
		}



		protected override void DrawFrame( GameTime gameTime, SpriteLayer spriteLayer, int clipRectIndex )
		{
			atlasButton.ClipName	=	binding.GetValue();
			base.DrawFrame( gameTime, spriteLayer, clipRectIndex );
		}
	}
}
