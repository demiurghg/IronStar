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
		private Allocator2D<TTag> allocator;
		private LinkedList<Rectangle> lruList;

		Action<Rectangle,TTag> discard;
		
		public LRUImageCache( int size, Action<Rectangle,TTag> discard )
		{
			allocator		=	new Allocator2D<TTag>(size);
			lruList			=	new LinkedList<Rectangle>();
			this.discard	=	discard;
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
				if (allocator.TryAlloc(size, tag, out region))
				{
					lruList.AddLast( region );
					return true;
				}
				else
				{
					var removedRegion = lruList.First.Value;
					TTag removedTag;
					
					lruList.RemoveFirst();
					allocator.Free( removedRegion, out removedTag );

					OnDiscard( removedRegion, removedTag );
				}
			}
		}


		void OnDiscard( Rectangle region, TTag tag )
		{
			discard?.Invoke( region, tag );
		}

		
		public bool TryGet( Rectangle region, out TTag tag )
		{
			if (allocator.TryGet( region, out tag ))
			{
				lruList.Remove(region);
				lruList.AddLast(region);
				return true;
			}
			else
			{
				tag = default(TTag);
				return false;
			}
		}


		public bool Remove( Rectangle region, out TTag tag )
		{
			if (allocator.Free( region, out tag ))
			{
				lruList.Remove(region);
				return true;
			}
			return false;
		}


		public bool Remove( Rectangle region )
		{
			TTag dummy;
			return Remove(region, out dummy);
		}
		/*public bool TryGet(   out Rectangle region )
		{
			region = default(Rectangle);

			var node = FindNodeByTag( tag );

			if (node!=null)
			{
				lruList.Remove(node);
				lruList.AddLast(node);
				region = node.Value.Key;
				return true;
			}

			return false;
		}  */


		public void Clear()
		{
			lruList.Clear();
			allocator.FreeAll();
		}


		public IEnumerable<TTag> GetContent()
		{
			return allocator.GetAllocatedBlockInfo().Select( block => block.Tag );
		}
	}
}
