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
using IronStar.Editor.Controls;
using IronStar.Editor.Manipulators;
using Fusion.Engine.Frames;
using Fusion.Core.Shell;
using Fusion.Core.Configuration;

namespace IronStar.Editor {

	public partial class MapEditor : GameComponent {

		void RegisterCommands ()
		{
			Game.Invoker.RegisterCommand("editorSave"	, () => new EditorSave(this) );
			Game.Invoker.RegisterCommand("editorSaveAs"	, () => new EditorSaveAs(this) );
		}


		void UnregisterCommands ()
		{
			Game.Invoker.UnregisterCommand("editorSave"		);
			Game.Invoker.UnregisterCommand("editorSaveAs"	);
		}



		class CreateAsset : CommandNoHistory 
		{
			readonly MapEditor mapEditor;

			public CreateAsset ( MapEditor mapEditor, ArgList args )
			{
				this.mapEditor	=	mapEditor;
			}

			public override object Execute()
			{
				mapEditor.SaveMap();
				return null;
			}
		}



		class EditorSave : CommandNoHistory
		{
			readonly MapEditor mapEditor;

			public EditorSave ( MapEditor mapEditor )
			{
				this.mapEditor	=	mapEditor;
			}

			public override object Execute()
			{
				mapEditor.SaveMap();
				return null;
			}
		}


		class EditorSaveAs : CommandNoHistory
		{
			readonly MapEditor mapEditor;

			[CommandLineParser.Required]
			[CommandLineParser.Name("newMapName")]
			public string NewMapName { get; set; }

			public EditorSaveAs ( MapEditor mapEditor )
			{
				this.mapEditor	=	mapEditor;
			}

			public override object Execute()
			{
				mapEditor.SaveMapAs(NewMapName);
				return null;
			}
		}
	}
}
