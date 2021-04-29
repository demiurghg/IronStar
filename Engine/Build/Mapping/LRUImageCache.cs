using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace Fusion.Build.Mapping 
{
	/// <summary>
	/// http://stackoverflow.com/questions/754233/is-it-there-any-lru-implementation-of-idictionary
	/// https://medium.com/swlh/design-and-implement-cache-systems-with-least-recently-used-and-least-frequently-used-policies-in-1bedc4c7f328
	/// </summary>
	/// <typeparam name="Key"></typeparam>
	/// <typeparam name="Value"></typeparam>
	public class LRUImageCache<TTag> : IImageCache<TTag>
	{
		private Allocator2D<LinkedListNode<KeyValuePair<Rectangle,TTag>>> allocator;
		private LinkedList<KeyValuePair<Rectangle,TTag>> lruList;

		
		public LRUImageCache( int size )
		{
			allocator	=	new Allocator2D<LinkedListNode<KeyValuePair<Rectangle,TTag>>>(size);
			lruList		=	new LinkedList<KeyValuePair<Rectangle, TTag>>();
		}


		public Rectangle Add( int size, TTag tag )
		{
			Rectangle region;

			if ( TryAdd(size, tag, out region) )
			{
				return region;
			}

			throw new OutOfMemoryException(string.Format("Can not allocate region size {0}x{0} for {1}", size, tag.ToString()));
		}


		public bool TryAdd( int size, TTag tag, out Rectangle region )
		{
			region	=	default(Rectangle);
			
			if (size>allocator.Size) 
			{
				return false;
			}


			while (true)
			{
				var pair = new KeyValuePair<Rectangle,TTag>( region, tag );
				var node = new LinkedListNode<KeyValuePair<Rectangle,TTag>>( pair ); 

				if (allocator.TryAlloc(size, node, out region))
				{
					lruList.AddLast( node );
					return true;
				}
				else
				{
					node = lruList.First;
					
					lruList.RemoveFirst();
					allocator.Free( node.Value.Key );

					OnDiscard( node.Value.Key, node.Value.Value );
				}
			}
		}


		void OnDiscard( Rectangle region, TTag tag )
		{
		}

		
		public bool TryGet( Rectangle region, out TTag tag )
		{
			LinkedListNode<KeyValuePair<Rectangle,TTag>> node;
			
			if (allocator.TryGet( region, out node ))
			{
				lruList.Remove(node);
				lruList.AddLast(node);
				tag = node.Value.Value;
				return true;
			}
			else
			{
				tag = default(TTag);
				return false;
			}
		}

		
		public bool TryGet( TTag tag, out Rectangle region )
		{
			region = default(Rectangle);

			for ( var node = lruList.First; node != null; node = node.Next )
			{
				if (node.Value.Value.Equals(tag))
				{
					lruList.Remove(node);
					lruList.AddLast(node);
					region = node.Value.Key;
					return true;
				}
			}

			return false;
		}

		
		public bool Remove( Rectangle region )
		{
			LinkedListNode<KeyValuePair<Rectangle,TTag>> node;
			
			if (allocator.Free( region, out node ))
			{
				lruList.Remove(node);
				return true;
			}
			else
			{
				return false;
			}
		}


		public void Clear()
		{
			lruList.Clear();
			allocator.FreeAll();
		}


		public IEnumerable<TTag> GetContent()
		{
			return allocator.GetAllocatedBlockInfo().Select( block => block.Tag.Value.Value );
		}
	}
}
