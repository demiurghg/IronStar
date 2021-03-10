using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
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
using Fusion.Core;

namespace IronStar.Editor {

	/// <summary>
	/// World represents entire game state.
	/// </summary>
	public partial class MapEditor {

		readonly Stack<HistorySnapshot> undoStack = new Stack<HistorySnapshot>();
		readonly Stack<HistorySnapshot> redoStack = new Stack<HistorySnapshot>();


		class HistorySnapshot 
		{
			public HistorySnapshot( Game game, IEnumerable<MapNode> selection, Map map )
			{
				SelectedNodes = selection.Select( node => node.Name ).ToArray();
				SerializedMap = JsonUtils.ExportJsonString( map );
			}

			public readonly string[] SelectedNodes;
			public readonly string SerializedMap;
		}


		public void Do ()
		{
			//undoStack.Push( new HistorySnapshot( Game, GetSelection(), this.Map ) );
			//redoStack.Clear();
		}


		void Undo()
		{
			//if (undoStack.Any())
			//{
			//	var snapshot = undoStack.Pop();
			//	redoStack.Push( snapshot );

			//	LoadSnapshot( snapshot );
			//}
			//else
			//{
			//	Log.Warning("Undo stack is empty");
			//}
		}


		void Redo()
		{
			//if (redoStack.Any())
			//{
			//	var snapshot = redoStack.Pop();
			//	undoStack.Push( snapshot );

			//	LoadSnapshot( snapshot );
			//}
			//else
			//{
			//	Log.Warning("Redo stack is empty");
			//}
		}


		void LoadSnapshot(HistorySnapshot snapshot)
		{
			map.Nodes.Clear();
			ResetWorld();

			map = (Map)JsonUtils.ImportJsonString( snapshot.SerializedMap );
			ResetWorld();

			selection.Clear();
			selection.AddRange( snapshot.SelectedNodes.Select( name => GetNodeByName(name) ) );
		}


		MapNode GetNodeByName(string name)
		{
			return map.Nodes.SingleOrDefault( node => node.Name == name );
		}
	}
}
