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
using IronStar.Editor.Commands;

namespace IronStar.Editor {

	public partial class MapEditor : GameComponent {

		void RegisterCommands ()
		{
			Game.Invoker.RegisterCommand("editorSave"	, () => new EditorSave(this) );
			Game.Invoker.RegisterCommand("editorSaveAs"	, () => new EditorSaveAs(this) );
			Game.Invoker.RegisterCommand("editorPrefab" , () => new EditorPrefabCommand(this) );
		}


		void UnregisterCommands ()
		{
			Game.Invoker.UnregisterCommand("editor*");
		}



		class CreateAsset : ICommand 
		{
			readonly MapEditor mapEditor;

			public CreateAsset ( MapEditor mapEditor, ArgList args )
			{
				this.mapEditor	=	mapEditor;
			}

			public object Execute()
			{
				mapEditor.SaveMap();
				return null;
			}
		}
	}
}
