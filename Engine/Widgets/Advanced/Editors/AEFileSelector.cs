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
using System.IO;

namespace Fusion.Widgets.Advanced
{
	public enum AEFileNameMode 
	{
		NoExtension = 0x0001,
		FileNameOnly = 0x0002,
	}
	
	public class AEFileNameAttribute : AEEditorAttribute 
	{
		readonly string dir;
		readonly string ext;
		readonly AEFileNameMode mode;

		public AEFileNameAttribute( string dir, string ext, AEFileNameMode mode )
		{
			this.dir	=	dir;
			this.ext	=	ext;
			this.mode	=	mode;
		}

		public override Frame CreateEditor( AEPropertyGrid grid, string name, IValueBinding binding )
		{
			return new AEFileSelector( grid, name, dir, ext, mode, binding );
		}
	}

	
	class AEFileSelector : AEBaseEditor 
	{
		readonly StringBindingWrapper binding;
		readonly string dir;
		readonly string ext;
		readonly AEFileNameMode mode;

		Frame fileButton;
		TextBox textBox;
		OpenFileDialog fileSelector;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="grid"></param>
		/// <param name="bindingInfo"></param>
		public AEFileSelector ( AEPropertyGrid grid, string name, string dir, string ext, AEFileNameMode mode, IValueBinding binding ) : base(grid, name)
		{ 
			this.binding	=	new StringBindingWrapper(binding);
			this.dir		=	dir;
			this.ext		=	ext;
			this.mode		=	mode;
				
			Width			=	grid.Width;
			Height			=	ComputeItemHeight() + 23;

			fileSelector	=	new OpenFileDialog( Frames, dir, ext );

			StatusChanged	+=	AEFileSelector_StatusChanged;

			textBox	=	new TextBox( Frames, binding ) 
			{ 
				TextAlignment = Alignment.MiddleLeft, 
				Height = 23,
			};

			Add( textBox );

			fileButton					=	new Button( Frames, this.binding.GetValue(), 0,0,0,0, OpenDialog); 
			fileButton.Text				=	"Select File"; 
			fileButton.Border			=	1;
			fileButton.TextAlignment	=	Alignment.MiddleCenter;
			fileButton.PaddingLeft		=	3;
			fileButton.PaddingRight		=	3;

			Add( fileButton );

			Update(GameTime.Zero);
		}

		
		private void OpenDialog()
		{
			var fileName = binding.GetValue();
			var dirName  = dir;

			if (!string.IsNullOrWhiteSpace(fileName))
			{
				/*var fileDirName = Path.GetDirectoryName(fileName);
				if (Directory.Exists(fileDirName))
				{ */
					dirName = Path.GetDirectoryName(fileName);
				//}*/
			}

			fileSelector	=	new OpenFileDialog( Frames, dirName, ext );
			fileSelector.Show( (openFile) => binding.SetValue( openFile, ValueSetMode.Default ) );
			//FileSelector.ShowDialog( Frames, dir, ext, binding.GetValue(), (fnm)=>binding.SetValue(fnm, ValueSetMode.Default) );
		}


		private void AEFileSelector_StatusChanged( object sender, StatusEventArgs e )
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

			textBox.X			=	Width/2;
			textBox.Width		=	Width/2;
			textBox.Height		=	ComputeItemHeight();

			fileButton.X		=	Width/2;
			fileButton.Y		=	textBox.Y + textBox.Height + 1;
			fileButton.Width	=	Width/4;
			fileButton.Height	=	23;
		}


		protected override void DrawFrame( GameTime gameTime, SpriteLayer spriteLayer, int clipRectIndex )
		{
			base.DrawFrame( gameTime, spriteLayer, clipRectIndex );
		}
	}
}
