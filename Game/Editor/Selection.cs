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
using System.Collections;

namespace IronStar.Editor 
{
	public class Selection : ICollection<MapNode>
	{
		readonly List<MapNode> list;

		public int Count { get { return list.Count; } }
		public bool IsReadOnly { get { return ( (ICollection<MapNode>)list ).IsReadOnly; } }

		public event EventHandler	Changed;


		public Selection ()
		{
			this.list	=	new List<MapNode>();
		}

		public IEnumerator<MapNode> GetEnumerator()
		{
			return ( (IEnumerable<MapNode>)list ).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ( (IEnumerable<MapNode>)list ).GetEnumerator();
		}

		public void Add( MapNode item )
		{
			//	remove if already added and move to the end of the list
			list.Remove(item);
			list.Add( item );
			Changed?.Invoke(this, EventArgs.Empty);
		}

		public void Clear()
		{
			list.Clear();
			Changed?.Invoke(this, EventArgs.Empty);
		}

		public bool Remove( MapNode item )
		{
			if (list.Remove( item )) 
			{
				Changed?.Invoke(this, EventArgs.Empty);
				return true;
			}
			return false;
		}

		public void AddRange( IEnumerable<MapNode> items )
		{
			foreach ( var item in items )
			{
				list.Remove(item);
				list.Add( item );
			}
			Changed?.Invoke(this, EventArgs.Empty);
		}

		public void RemoveRange( IEnumerable<MapNode> items )
		{
			bool changed = false;

			foreach ( var item in items )
			{
				changed |= list.Remove(item);
			}

			if (changed)
			{
				Changed?.Invoke(this, EventArgs.Empty);
			}
		}

		public void SetRange( IEnumerable<MapNode> items )
		{
			list.Clear();
			list.AddRange( items );
			Changed?.Invoke(this, EventArgs.Empty);
		}

		public void Toggle( MapNode item )
		{
			if (item==null) return;

			if (list.Contains(item))
			{
				list.Remove( item );
			}
			else
			{
				list.Add( item );
			}
			Changed?.Invoke(this, EventArgs.Empty);
		}

		public bool Contains( MapNode item )
		{
			return list.Contains( item );
		}

		public void CopyTo( MapNode[] array, int arrayIndex )
		{
			list.CopyTo( array, arrayIndex );
		}
	}
}
