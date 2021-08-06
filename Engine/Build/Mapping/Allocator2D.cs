using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using System.Diagnostics;
using System.IO;
using Fusion.Engine.Imaging;
using Newtonsoft.Json;

namespace Fusion.Build.Mapping 
{
	/*---------------------------------------------------------------------------------------------
	 *	Generic Allocator Version
	---------------------------------------------------------------------------------------------*/

	/// <summary>
	/// http://www.memorymanagement.org/mmref/alloc.html
	/// </summary>
	public partial class Allocator2D<TTag> 
	{
		public readonly int Size;

		public int Width { get { return Size; } }
		public int Height { get { return Size; } }

		public bool IsEmpty { get { return rootBlock.State==BlockState.Free; } }

		protected Block rootBlock;

		protected Block RootBlock { get { return rootBlock; } }


		/// <summary>
		/// 
		/// </summary>
		/// <param name="size"></param>
		public Allocator2D ( int size = 1024 )
		{
			Size		=	size;
			rootBlock	=	new Block( new Int2(0,0), size, null, null );
		}


		public Rectangle Alloc ( int size, TTag tag )
		{
			Rectangle rect;

			if (!TryAlloc(size, tag, out rect)) 
			{
				throw new OutOfMemoryException(string.Format("No enough space in 2D allocator (size={0})", size));
			} 
			else 
			{
				return rect;
			}
		}


		public bool TryAlloc ( int size, TTag tag, out Rectangle rectangle )
		{
			rectangle = new Rectangle(0,0,0,0);

			if (tag==null) 
			{
				throw new ArgumentNullException("tag");
			}
			if (size<=0) 
			{
				throw new ArgumentOutOfRangeException("size");
			}

			var block	=	GetFreeBlock( MathUtil.RoundUpNextPowerOf2( size ) );

			if (block==null) 
			{
				return false;
			}

			block.Tag	=	tag;
			rectangle	=	new Rectangle( block.Address.X, block.Address.Y, size, size );

			return true;
		}


		public void FreeAll ()
		{
			rootBlock	=	new Block( new Int2(0,0), Size, null, null );
		}


		Block GetFreeBlock ( int size )
		{
			var Q = new Stack<Block>();

			Q.Push( rootBlock );

			while ( Q.Any() ) 
			{
				var block = Q.Pop();

				var state = block.State;

				if (block.Size<size) 
				{
					continue;
				}

				// block is already allocated - skip
				if (state==BlockState.Allocated) 
				{
					continue;
				}

				if (state==BlockState.Split) 
				{
					if (block.Size/2>=size) 
					{
						Q.Push( block.BottomRight );
						Q.Push( block.BottomLeft );
						Q.Push( block.TopRight	 );
						Q.Push( block.TopLeft	 );
					}
					continue;
				}

				if (state==BlockState.Free) 
				{
					if (block.Size/2>=size) 
					{
						block.Split();
						Q.Push( block.BottomRight );
						Q.Push( block.BottomLeft );
						Q.Push( block.TopRight	 );
						Q.Push( block.TopLeft	 );
					} 
					else
					{
						return block;
					}
				}
			}

			return null;
		}


		bool TryFindBlock( Rectangle region, out Block node )
		{
			node = rootBlock;
			var address = new Int2( region.X, region.Y );

			while (true) 
			{
				if (node.Address==address) 
				{
					if (node.State==BlockState.Allocated) 
					{
						return true;
					}
					if (node.State==BlockState.Free)
					{
						return false;
					}
				}

				if (node.IsAddressInside(address)) 
				{
					if ( node.TopLeft.IsAddressInside(address) ) 
					{
						node = node.TopLeft;
						continue;
					}	
					if ( node.TopRight.IsAddressInside(address) ) 
					{
						node = node.TopRight;
						continue;
					}	
					if ( node.BottomLeft.IsAddressInside(address) ) 
					{
						node = node.BottomLeft;
						continue;
					}	
					if ( node.BottomRight.IsAddressInside(address) ) 
					{
						node = node.BottomRight;
						continue;
					}	
				} 
				else 
				{
					node = default(Block);
					return false;
				}
			}
		}


		public bool Free ( Rectangle region, out TTag tag )
		{
			Block node;

			if (TryFindBlock( region, out node ))
			{
				tag = node.Tag;
				node.FreeAndMerge();
				return true;
			}
			else
			{
				tag = default(TTag);
				return false;
			}
		}


		public bool Free ( Rectangle region )
		{
			TTag dummy;
			return Free( region, out dummy );
		}


		public bool TryGet( Rectangle region, out TTag tag )
		{
			tag = default(TTag);
			Block block;

			if (TryFindBlock( region, out block ))
			{
				tag = block.Tag;
				return true;
			}
			else
			{
				return false;
			}
		}


		public IEnumerable<BlockInfo> GetAllocatedBlockInfo ()
		{
			var list = new List<BlockInfo>();

			var S = new Stack<Block>();

			S.Push( rootBlock );

			while (S.Any()) 
			{
				var block = S.Pop();

				if (block.State==BlockState.Allocated) 
				{
					list.Add( new BlockInfo(block.Address, block.Size, block.Tag) );
				}

				if (block.State==BlockState.Split) 
				{
					S.Push( block.BottomRight );
					S.Push( block.BottomLeft );
					S.Push( block.TopRight );
					S.Push( block.TopLeft );
				}
			}

			return list;
		}


		public Size2 GetBlockSize ( Int2 address )
		{
			throw new NotImplementedException();
		}


		static void DrawRectangle ( Image<Color> image, int x, int y, int w, int h, Color color, bool clear )
		{
			for (var i=x; i<x+w; i++) {
				for (var j=y; j<y+h; j++) {
					var c = image.GetPixel(i,j);

					if (!clear && c!=Color.Black) {
						Log.Warning("Overlap!");
					}
					image.SetPixel( i,j, color );	
				}
			}
		}


		public static void RunTest ( int size, int interations, string dir )
		{
			Log.Message("Allocator2D test: {0} {1} {2}", size, interations, dir );

			var dirInfo = Directory.CreateDirectory(dir);
			
			foreach (FileInfo file in dirInfo.GetFiles()) {
				file.Delete(); 
			}

			var alloc	= new Allocator2D(size);
			var image	= new Image<Color>(size,size, Color.Black);
			var rand	= new Random();

			var list    = new List<Rectangle>();

			for (int i=0; i<interations; i++) {

				Log.Message("{0,3:D3}/{1,3:D3}", i,interations);

				try {				

					bool allocNotFree = rand.NextFloat(0,1)<0.5f;
					bool reloadState  = rand.NextFloat(0,1)<0.1f;

					if (allocNotFree) {

						for (int j=0; j<rand.Next(1,16); j++) {
							//var sz = MathUtil.RoundUpNextPowerOf2( rand.Next(1,64) );
							var sz = rand.Next(1,64);

							var tag = string.Format("Block#{0,4:D4}##{1,4:D4}", i,sz);

							var r  = alloc.Alloc(sz, tag);

							list.Add(r);

							DrawRectangle( image, r.X, r.Y, r.Width, r.Height, rand.NextColor(), false );
						}

					} else {

						for (int j=0; j<rand.Next(1,16); j++) {
							
							if (!list.Any()) {
								break;
							}

							var id = rand.Next(list.Count);
							var fa = list[ id ];
							list.RemoveAt( id );

							if(alloc.Free(fa))
							{
								DrawRectangle( image, fa.X, fa.Y, fa.Width, fa.Height, Color.Black, true );
							}
						}
					}

					var imagePath = Path.Combine(dir, string.Format("allocTest{0,4:D4}.tga", i));
					var stackPath = Path.Combine(dir, string.Format("allocTest{0,4:D4}.stk", i));

					ImageLib.SaveTga( image, imagePath );

					if (reloadState) {

						Log.Message("---- STATE RELOAD ----------------------------------");

						using ( var stream = File.OpenWrite( stackPath ) ) {
							Allocator2D.SaveState( stream, alloc );
						}

						using ( var stream = File.OpenRead( stackPath ) ) {
							alloc = Allocator2D.LoadState( stream );
						}
					}

				} catch ( Exception e ) {
					Log.Error("Exception: {0}", e.Message);
					continue;
				}
			}

			Log.Message("Done!");
		}

		/*-----------------------------------------------------------------------------------------------
		 *	Block stuff :
		-----------------------------------------------------------------------------------------------*/

		protected enum BlockState 
		{
			Free = 0,
			Split = 1,
			Allocated = 2,
		}


		public class BlockInfo 
		{
			public BlockInfo( Int2 address, int size, TTag tag )
			{
				Address	=	address;
				Size	=	size;
				Tag		=	tag;
			}

			public readonly Int2	Address;
			public readonly int		Size;
			public readonly TTag	Tag;
			public Rectangle Region 
			{
				get { return new Rectangle( Address.X, Address.Y, Size, Size ); }
			}
		}

		protected class Block 
		{
			public Int2	Address;
			public int	Size;

			public Block TopLeft;
			public Block TopRight;
			public Block BottomLeft;
			public Block BottomRight;
			public Block Parent;

			public TTag Tag 
			{
				get; set;
			}

			public BlockState State 
			{
				get 
				{
					if (TopLeft==null && BottomLeft==null && TopLeft==null && TopLeft==null) 
					{
						if (Tag==null) 
						{
							return BlockState.Free;
						} else 
						{
							return BlockState.Allocated;
						}
					} 
					else 
					{
						if (Tag==null) 
						{
							return BlockState.Split;
						} 
						else 
						{
							throw new InvalidOperationException("Bad block state");
						}
					}
				}
			}


			public Block ( Int2 address, int size, Block parent, string tag )
			{
				Address	=	address;
				Size	=	size;
				Tag		=	default(TTag);
				Parent	=	parent;
			}


			public Block Split ()
			{
				if (State!=BlockState.Free) 
				{
					throw new InvalidOperationException(string.Format("{0} block could not be split", State));
				}

				var addr	=	Address;
				var size	=	Size / 2;

				TopLeft		=	new Block( new Int2( addr.X,		addr.Y			), size, this, null );
				TopRight	=	new Block( new Int2( addr.X + size, addr.Y			), size, this, null );
				BottomLeft	=	new Block( new Int2( addr.X,		addr.Y + size	), size, this, null );
				BottomRight	=	new Block( new Int2( addr.X + size, addr.Y + size	), size, this, null );

				return TopLeft;
			}


			public void FreeAndMerge ()
			{
				if (State==BlockState.Allocated) 
				{
					Tag = default(TTag);

					var parent = Parent;

					while (parent!=null && parent.TryMerge()) 
					{
						parent = parent.Parent;
					}

				}
				else 
				{
					throw new InvalidOperationException(string.Format("Can not free {0} block", State));
				}
			}


			public bool TryMerge ()
			{
				if (State==BlockState.Split) 
				{
					if (   TopLeft.State==BlockState.Free 
						&& TopRight.State==BlockState.Free 
						&& BottomLeft.State==BlockState.Free 
						&& BottomRight.State==BlockState.Free ) 
					{

						TopLeft = null;
						TopRight = null;
						BottomLeft = null;
						BottomRight = null;

						return true;
					}
					else
					{
						return false;
					}
				} 
				else 
				{
					return false;
				}
			}


			public bool IsAddressInside ( Int2 address )
			{
				return ( address.X >= Address.X )
					&& ( address.Y >= Address.Y )
					&& ( address.X < Address.X + Size )
					&& ( address.Y < Address.Y + Size );
			}
		}
	}


	/*---------------------------------------------------------------------------------------------
	 *	String Version
	---------------------------------------------------------------------------------------------*/

	/// <summary>
	/// http://www.memorymanagement.org/mmref/alloc.html
	/// </summary>
	public partial class Allocator2D : Allocator2D<string>
	{
		public Allocator2D( int size ) : base( size )
		{
		}


		public static void SaveState ( Stream targetStream, Allocator2D allocator )
		{
			var writer = new BinaryWriter(targetStream, Encoding.Default, true);
			SaveState( writer, allocator );
		}


		public static Allocator2D LoadState ( Stream sourceStream )
		{
			var reader = new BinaryReader(sourceStream, Encoding.Default, true);
			return LoadState( reader );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="allocator"></param>
		/// <returns></returns>
		public static void SaveState ( BinaryWriter writer, Allocator2D allocator )
		{		
			//	write header:
			writer.WriteFourCC("MLC2");
			writer.WriteFourCC("1.00");

			writer.Write( allocator.Size );

			//	write nodes if depth-first order :
			var S = new Stack<Block>();

			S.Push( allocator.rootBlock );

			while (S.Any()) 
			{
				var block = S.Pop();

				writer.WriteFourCC("BLCK");

				writer.Write( block.Address.X );
				writer.Write( block.Address.Y );
				writer.Write( block.Size );
				writer.Write( (int)block.State );

				if (block.State==BlockState.Allocated) 
				{
					writer.Write( block.Tag );
				}

				if (block.State==BlockState.Split) 
				{
					S.Push( block.BottomRight );
					S.Push( block.BottomLeft );
					S.Push( block.TopRight );
					S.Push( block.TopLeft );
				}
			}
		}



		public static Allocator2D LoadState ( BinaryReader reader )
		{
			//	read header:
			var fourcc  = reader.ReadFourCC();
			var version = reader.ReadFourCC();

			var size	= reader.ReadInt32();
			
			var allocator = new Allocator2D( size );


			//	read nodes if depth-first order :
			var S = new Stack<Block>();

			S.Push( allocator.RootBlock );

			while (S.Any()) 
			{
				var block = S.Pop();

				var fcc = reader.ReadFourCC();

				block.Address.X = reader.ReadInt32();
				block.Address.Y = reader.ReadInt32();
				block.Size		= reader.ReadInt32();
				
				var state		=	(BlockState)reader.ReadInt32();

				if (state==BlockState.Allocated) 
				{
					block.Tag = reader.ReadString();
				}

				if (state==BlockState.Split) 
				{
					block.Split();
					S.Push( block.BottomRight );
					S.Push( block.BottomLeft );
					S.Push( block.TopRight );
					S.Push( block.TopLeft );
				}
			}

			return allocator;
		}
	}
}
