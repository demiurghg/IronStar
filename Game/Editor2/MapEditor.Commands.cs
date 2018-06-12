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

	public partial class MapEditor : GameComponent {

		void RegisterCommands ()
		{
			Game.Invoker.RegisterCommand("editorSave"	, (args) => new EditorSave(this, args) );
			Game.Invoker.RegisterCommand("editorSaveAs"	, (args) => new EditorSaveAs(this, args) );
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

			public EditorSave ( MapEditor mapEditor, ArgList args )
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
			readonly string newMapName;

			public EditorSaveAs ( MapEditor mapEditor, ArgList args )
			{
				this.mapEditor	=	mapEditor;

				args.Usage("editorSaveAs <newMapName>")
					.Require("newMapName", out newMapName )
					.Apply();
			}

			public override object Execute()
			{
				mapEditor.SaveMapAs(newMapName);
				return null;
			}
		}
	}
}
