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
				var jsonFactory = game.GetService<JsonFactory>();

				SelectedNodes = selection.Select( node => node.NodeGuid ).ToArray();
				SerializedMap = jsonFactory.ExportJsonString( map );
			}

			public readonly Guid[] SelectedNodes;
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

			map = (Map)Game.GetService<JsonFactory>().ImportJsonString( snapshot.SerializedMap );
			ResetWorld();

			selection.Clear();
			selection.AddRange( snapshot.SelectedNodes.Select( guid => GetNodeByGuid(guid) ) );
		}


		MapNode GetNodeByGuid(Guid guid)
		{
			return map.Nodes.SingleOrDefault( node => node.NodeGuid == guid );
		}
	}
}
