using BEPUphysics.Paths;
using Fusion;
using Fusion.Core.Extensions;
using Fusion.Engine.Frames;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Engine.Frames.Layouts;
using Fusion.Build;
using Fusion.Widgets;
using Fusion.Widgets.Advanced;
using IronStar.Mapping;
using Fusion.Widgets.Binding;
using IronStar.Editor.Commands;
using System.Collections;

namespace IronStar.Editor.Controls 
{
	public class MapOutliner : Panel 
	{
		Label	label;
		TextBox	filter;
		ListBox listBox;
		Button	btnClose;
		MapEditor editor;
		
		public MapOutliner( Frame parent, MapEditor editor, int x, int y, int w, int h ) : base(parent.ui, x,y,w,h)
		{
			this.editor		=	editor;
			var fp			=	parent.ui;
			AllowDrag		=	true;
			AllowResize		=	true;

			var pageLayout = new PageLayout();
			pageLayout.AddRow(20, /**/ -1.0f );
			pageLayout.AddRow(20, /**/ -1.0f );
			pageLayout.AddRow(-1, /**/ -1.0f );
			pageLayout.AddRow(23, /**/ -1.0f );

			Layout	=	pageLayout;

			label		=	new Label( fp, 0,0,0,0, "Outliner") { Padding = 2 };
			filter		=	new TextBox(fp) { TextAlignment = Alignment.MiddleLeft };
			btnClose	=	new Button(fp, "Close", 0,0,0,0, () => Visible = false );

			listBox		=	new ListBox( fp, new MapNode[0], obj => GetDisplayName((MapNode)obj) );  
			listBox.Binding = new MapBinding( editor );
			listBox.AllowMultipleSelection = true;
			listBox.SelectedItemChanged +=ListBox_SelectedItemChanged;
			listBox.Font = ColorTheme.NormalFont;
			editor.Selection.Changed += EditorSelection_Changed;

			Add( label );
			Add( filter );
			Add( listBox );
			Add( btnClose );
		}

		private void EditorSelection_Changed( object sender, EventArgs e )
		{
			suppressSelectedItemChanged = true;

			listBox.DeselectAll();

			foreach ( var mapNode in editor.Selection)
			{
				listBox.SelectItem( mapNode, true );
			}

			suppressSelectedItemChanged = false;
		}


		bool suppressSelectedItemChanged = false;

		private void ListBox_SelectedItemChanged( object sender, EventArgs e )
		{
			if (suppressSelectedItemChanged) return;

			var selectedNodes = listBox.SelectedItems
								.Select( obj => obj as MapNode )
								.Where( node => node!=null )
								.ToArray();

			Game.Invoker.Execute( new SelectNodes( editor, SelectMode.Replace, selectedNodes ) );
		}

		
		string GetDisplayName( MapNode node )
		{
			return node.GetType().Name + " : " + node.Name;
		}


		class MapBinding : IListBinding
		{
			readonly MapEditor editor;

			public MapBinding( MapEditor editor )
			{
				this.editor	=	editor;
			}
			
			public object this[int index] 
			{
				get 
				{
					if (editor.Map!=null)
					{
						return editor.Map.Nodes[index]; 
					}
					throw new ArgumentOutOfRangeException("index");
				}
			}

			public int Count 
			{ 
				get 
				{ 
					if (editor.Map!=null)
					{
						return editor.Map.Nodes.Count; 
					}
					else
					{
						return 0;
					}
				}
			}

			public bool IsReadonly { get	{ return false; } }
			public void Add( object item ) { throw new NotImplementedException(); }

			public IEnumerator GetEnumerator()
			{
				return editor.Map.Nodes.GetEnumerator();
			}

			public void Remove( object item ) { throw new NotImplementedException(); }
		}
	}
}
