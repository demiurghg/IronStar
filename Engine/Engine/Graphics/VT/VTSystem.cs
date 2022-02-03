using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Configuration;
using Fusion.Core.Extensions;
using Fusion.Engine.Common;
using Fusion.Drivers.Graphics;
using Fusion.Engine.Imaging;
using System.Diagnostics;
using Fusion.Build.Mapping;
using Fusion.Engine.Graphics.Ubershaders;
using System.ComponentModel;
using Fusion.Widgets.Advanced;
using Fusion.Core.Utils;
using Fusion.Build;

namespace Fusion.Engine.Graphics 
{
	[ConfigClass]
	[RequireShader("vtcache", true)]
	[ShaderSharedStructure(typeof(PageGpu))]
	internal class VTSystem : GameComponent 
	{
		struct PARAMS
		{
			public uint totalPageCount;
			public uint dummy0;
			public uint dummy1;
			public uint dummy2;
		}

		[ShaderDefine]
		const int BlockSize = 256;

		static FXConstantBuffer<PARAMS>		regParams		=	new CRegister( 0, "Params"	);

		static FXStructuredBuffer<PageGpu>	regPageData		=	new TRegister( 0, "pageData"	);

		static FXRWTexture2D<uint>		regTarget0		=	new URegister( 0, "pageTable"		);
		static FXRWTexture2D<uint>		regTarget1		=	new URegister( 1, "pageTable1"		);
		static FXRWTexture2D<uint>		regTarget2		=	new URegister( 2, "pageTable2"		);
		static FXRWTexture2D<uint>		regTarget3		=	new URegister( 3, "pageTable3"		);
		static FXRWTexture2D<uint>		regTarget4		=	new URegister( 4, "pageTable4"		);
		static FXRWTexture2D<uint>		regTarget5		=	new URegister( 5, "pageTable5"		);
		static FXRWTexture2D<uint>		regTarget6		=	new URegister( 6, "pageTable6"		);

		readonly RenderSystem rs;

		[Config]
		[AECategory("Performamce")]
		[Description("Max uploaded to physical texture pages per frame")]
		static public int MaxPPF { get; set; }

		[Config]
		[AECategory("Debugging")]
		static public bool ShowPageCaching { get; set; }

		[Config]
		[AECategory("Debugging")]
		static public bool ShowPageLoads { get; set; }

		[Config]
		[AECategory("Debugging")]
		static public bool SkipPageTableUpdate { get; set; }

		[Config]
		[AECategory("Tiles")]
		[Description("Shows tile border for each uploaded tile")]
		static public bool ShowTileBorder { get; set; }

		[Config]
		[AECategory("Tiles")]
		[Description("Disables virtual texturing feedback")]
		static public bool LockTiles { get; set; }

		[Config]
		[AECategory("Tiles")]
		[Description("Shows tile address for each uploaded tile")]
		static public bool ShowTileAddress { get; set; }

		[Config]
		[AECategory("Tiles")]
		[Description("Fills each tile with checkers, for filtering debugging")]
		static public bool ShowTileCheckers { get; set; }

		[Config]
		[AECategory("Debugging")]
		[Description("Fills each tile with checkers, for filtering debugging")]
		static public bool UpdateStressTest { get; set; }

		[Config]
		[AECategory("Tiles")]
		[Description("Fills each tile mip level with solid colors to debug mip transitions")]
		static public bool ShowMipLevels { get; set; }

		[Config]
		[AECategory("Tiles")]
		[Description("Fills each tile with random color.")]
		static public bool RandomColor { get; set; }

		[Config]
		[AECategory("Performamce")]
		[Description("Texture LOD bias.")]
		public int LodBias { get; set; } = 0;

		[Config]
		[AECategory("Debugging")]
		static public bool ShowThrashing { get; set; } = false;

		[Config]
		[AECategory("Tiles")]
		static public bool ShowDiffuse { get; set; } = false;

		[Config]
		[AECategory("Tiles")]
		static public bool ShowSpecular { get; set; } = false;

		[Config]
		[AECategory("Tiles")]
		static public bool ShowMirror { get; set; } = false;

		[Config]
		[AECategory("Tiles")]
		[AESlider(0,1,1f/6f,0.01f)]
		static public float MirrorRoughness { get; set; } = 0;

		[Config]
		[AECategory("Performamce")]
		[Description("Enables and disables anisotropic filtering.")]
		static public bool UseAnisotropic { get; set; }	= true;

		[Config]
		[AECategory("Debugging")]
		static public float DebugGradientScale { get; set; } = 1;

		[Config]
		[AECategory("Performamce")]
		[Description("Size of physical texture")]
		static public int PhysicalSize 
		{
			get { return physicalSize; }
			set 
			{
				if (physicalSize!=value) 
				{
					physicalSize = value;
					physicalSizeDirty = true;
				}
			}
		}
		static int physicalSize = 1024;
		static bool physicalSizeDirty = true;

		const int FeedBackBufferPoolSize = 8;
		const int GpuPageDataPoolSize = 4;

		public float PageScaleRCP 
		{
			get; private set;
		}

		[AECommand]
		public void VTRestart ()
		{
			Game.Invoker.ExecuteString("vtrestart");
		}

		public Texture2D		PhysicalPages0;
		public Texture2D		PhysicalPages1;
		public Texture2D		PhysicalPages2;
		public Texture2D		MipIndex;
		public RenderTarget2D	PageTable;
		public StructuredBuffer	PageData;
		public ConstantBuffer	Params;

		public Texture2DStaging		StagingTile;
		public Texture2DStaging		StagingTileMip;
		public Texture2DStaging		StagingTileSrgb;
		public Texture2DStaging		StagingTileSrgbMip;

		VTTileLoader	tileLoader;
		VTTileCache		tileCache;
		PageGpu[]		pageDataCpu;

		Ubershader		shader;
		StateFactory	factory;

		FixedObjectPool<VTAddress[]> feedbackBufferPool;
		FixedObjectPool<PageGpu[]> gpuPageDataPool;
		public FixedObjectPool<VTAddress[]> FeedbackBufferPool { get { return feedbackBufferPool; } }
		public FixedObjectPool<PageGpu[]> GpuPageDataPool { get { return gpuPageDataPool; } }

		enum Flags {
			None  = 0,
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="rs"></param>
		/// <param name="baseDirectory"></param>
		public VTSystem ( RenderSystem rs ) : base( rs.Game )
		{
			this.rs	=	rs;

			var buffers = Enumerable
						.Range(0, FeedBackBufferPoolSize )
						.Select( i => CreateFeedbackBuffer() )
						.ToArray();

			feedbackBufferPool	=	new FixedObjectPool<VTAddress[]>( buffers );

			MaxPPF	=	16;

			Game.GetService<Builder>().Building += (s,e) => Suspend();
		}


		VTAddress[] CreateFeedbackBuffer()
		{
			var size   = HdrFrame.FeedbackBufferWidth * HdrFrame.FeedbackBufferHeight; 
			var buffer = new VTAddress[size];

			for (int i=0; i<size; i++) buffer[i] = VTAddress.CreateBadAddress(-1);

			return buffer;
		}

		Stopwatch stopwatch = new Stopwatch();


		/// <summary>
		/// 
		/// </summary>
		public override void Initialize ()
		{
			ApplyVTState();

			var rand = new Random();
			//PageTable.SetData( Enumerable.Range(0,tableSize*tableSize).Select( i => rand.NextColor4() ).ToArray() );

			Game.Reloading += (s,e) => LoadContent();

			LoadContent();
		}


		/// <summary>
		/// 
		/// </summary>
		void LoadContent ()
		{
			shader	=	Game.Content.Load<Ubershader>("vtcache");
			factory	=	shader.CreateFactory( typeof(Flags), Primitive.TriangleList, VertexInputElement.Empty );
		}


		public void Suspend()
		{
			SafeDispose( ref tileLoader );
		}


		public void Resume()
		{					
			if (tileLoader==null)
			{
				tileLoader = new VTTileLoader(this, tileCache);
			}
		}


		/// <summary>
		/// 
		/// </summary>
		void ApplyVTState ()
		{
			if (physicalSizeDirty) 
			{
				Log.Message("VT state changes: new size {0}", physicalSize);

				SafeDispose( ref PhysicalPages0	);
				SafeDispose( ref PhysicalPages1	);
				SafeDispose( ref PhysicalPages2	);
				SafeDispose( ref PageTable		);
				SafeDispose( ref PageData		);
				SafeDispose( ref Params			);
				SafeDispose( ref MipIndex		);

				MipIndex			=	CreateMipSelectorTexture();

				int tableSize		=	VTConfig.VirtualPageCount;
				int physSize		=	physicalSize;
				int physPages		=	physicalSize / VTConfig.PageSizeBordered;
				int maxTiles		=	physPages * physPages;
				int tileSize		=	VTConfig.PageSizeBordered;

				PhysicalPages0		=	new Texture2D( rs.Device, physSize, physSize, ColorFormat.Rgba8_sRGB, 2, true );
				PhysicalPages1		=	new Texture2D( rs.Device, physSize, physSize, ColorFormat.Rgba8,	  2, false );
				PhysicalPages2		=	new Texture2D( rs.Device, physSize, physSize, ColorFormat.Rgba8,	  2, false );

				StagingTile			=	new Texture2DStaging( rs.Device, tileSize,   tileSize,   ColorFormat.Rgba8		);
				StagingTileSrgb		=	new Texture2DStaging( rs.Device, tileSize,   tileSize,   ColorFormat.Rgba8_sRGB	);
				StagingTileMip		=	new Texture2DStaging( rs.Device, tileSize/2, tileSize/2, ColorFormat.Rgba8		);
				StagingTileSrgbMip	=	new Texture2DStaging( rs.Device, tileSize/2, tileSize/2, ColorFormat.Rgba8_sRGB	);

				PageTable			=	new RenderTarget2D( rs.Device, ColorFormat.R32, tableSize, tableSize, true, true );
				PageData			=	new StructuredBuffer( rs.Device, typeof(PageGpu), maxTiles, StructuredBufferFlags.None );
				pageDataCpu			=	new PageGpu[ maxTiles ];
				Params				=	new ConstantBuffer( rs.Device, 16 );

				SafeDispose( ref tileLoader );

				tileCache			=	new VTTileCache( physPages, physicalSize );
				tileLoader			=	new VTTileLoader( this, tileCache ); 

				PageScaleRCP		=	VTConfig.PageSize / (float)physSize;
				
				physicalSizeDirty	=	false;
			}
		}


		Texture2D CreateMipSelectorTexture ()
		{
			int mips = VTConfig.MaxMipLevel;
			int size = 1 << VTConfig.MaxMipLevel;

			var mipIndex		=	new Texture2D( rs.Device, size, size, ColorFormat.R32F, mips, false );

			for (int i=0; i<=mips; i++)
			{
				mipIndex.SetData( i, Enumerable.Range(0, (64>>i)*(64>>i)).Select( n=> (float)i ).ToArray() );
			}
			return mipIndex;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose ( bool disposing )
		{
			if (disposing) 
			{
				SafeDispose( ref tileLoader );
				
				SafeDispose( ref PhysicalPages0	);
				SafeDispose( ref PhysicalPages1	);
				SafeDispose( ref PhysicalPages2	);
				SafeDispose( ref StagingTile		);
				SafeDispose( ref StagingTileSrgb	);
				SafeDispose( ref StagingTileMip		);
				SafeDispose( ref StagingTileSrgbMip	);
				SafeDispose( ref PageTable		);
				SafeDispose( ref PageData		);
				SafeDispose( ref Params			);
				SafeDispose( ref MipIndex		);
			}
			base.Dispose( disposing );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="baseDir"></param>
		public void Start ( VirtualTexture vt )
		{
			stopwatch.Restart();
			Resume();
		}


		/// <summary>
		/// 
		/// </summary>
		public void Stop ()
		{
			tileLoader?.Purge();
			tileCache?.Purge();
		}


		public void Update ( VTAddress[] rawAddressData, GameTime gameTime )
		{
			using ( new PixEvent( "VT Update" ) ) 
			{
				ApplyVTState();

				tileLoader?.ReadFeedbackAndRequestTiles( rawAddressData );

				DownloadTiles(tileLoader, tileCache);

				StressTest();

				UpdatePageTable();
			}
		}


		/// <summary>
		///	Updates page table using GPU
		/// </summary>
		/// 8.12%
		void UpdatePageTable ()
		{
			int tableSize	=	VTConfig.VirtualPageCount;
			PageGpu[] pages;

			using (new PixEvent("UpdatePageTable")) 
			{
				var device = Game.GraphicsDevice;
				device.ResetStates();

				using ( new CVEvent( "GetGpuPageData" ) ) 
				{
					//	2.36%
					pages = tileCache.GetGpuPageData();
				}

				using ( new CVEvent( "SetData" ) ) 
				{
					if ( pages.Any() ) 
					{
						pages.CopyTo( pageDataCpu, 0 );
						//PageData.SetData( pages );
						PageData.UpdateData( pageDataCpu );
					}
				}

				using ( new CVEvent( "Dispatch Pass" ) ) 
				{
					var param = new PARAMS();
					param.totalPageCount	=	(uint)pages.Length;

					Params.SetData( ref param );

					device.PipelineState	=	factory[0];

					device.ComputeConstants[ regParams]		=	Params;
					device.ComputeResources[ regPageData]	=	PageData;
					device.SetComputeUnorderedAccess( regTarget0, PageTable.GetSurface(0).UnorderedAccess );
					device.SetComputeUnorderedAccess( regTarget1, PageTable.GetSurface(1).UnorderedAccess );
					device.SetComputeUnorderedAccess( regTarget2, PageTable.GetSurface(2).UnorderedAccess );
					device.SetComputeUnorderedAccess( regTarget3, PageTable.GetSurface(3).UnorderedAccess );
					device.SetComputeUnorderedAccess( regTarget4, PageTable.GetSurface(4).UnorderedAccess );
					device.SetComputeUnorderedAccess( regTarget5, PageTable.GetSurface(5).UnorderedAccess );
					device.SetComputeUnorderedAccess( regTarget6, PageTable.GetSurface(6).UnorderedAccess );

					if (!SkipPageTableUpdate)
					{
						int threadCount	=	tableSize * tableSize /	(  1 *  1 )//	0
										+	tableSize * tableSize / (  2 *  2 )//	1
										+	tableSize * tableSize / (  4 *  4 )//	2
										+	tableSize * tableSize / (  8 *  8 )//	3
										+	tableSize * tableSize / ( 16 * 16 )//	4
										+	tableSize * tableSize / ( 32 * 32 )//	5
										+	tableSize * tableSize / ( 64 * 64 )//	6
										;
						int groupSize	=	MathUtil.IntDivRoundUp( threadCount, 256 );

						device.Dispatch( groupSize, 1, 1 );
					}
				}
			}
		}



		void DownloadTiles(VTTileLoader tileLoader, VTTileCache tileCache)
		{
			if (tileLoader!=null && tileCache!=null) 
			{
				for (int i=0; i<MaxPPF; i++) 
				{
					VTTile tile;
					Rectangle rect;

					if (tileLoader.TryGetTile( out tile )) 
					{
						if (tileCache.TranslateAddress( tile.VirtualAddress, tile, out rect ) )
						{
							var sz = VTConfig.PageSizeBordered;

							if (RandomColor)		tile.FillRandomColor();
							if (ShowTileCheckers)	tile.DrawChecker();
							if (ShowTileBorder)		tile.DrawBorder();
							if (ShowMipLevels) 		tile.DrawMipLevels(ShowTileBorder);
							if (ShowDiffuse) 		tile.MakeWhiteDiffuse();
							if (ShowSpecular)		tile.MakeGlossyMetal();
							if (ShowMirror)			tile.MakeMirror();
							
							if (ShowTileAddress) 
							{
								tile.DrawText( 16,16, tile.VirtualAddress.ToString() );
								tile.DrawText( 16,32, string.Format("{0} {1}", tile.PhysicalAddress.X/sz, tile.PhysicalAddress.Y/sz ) );
								tile.DrawText( 16,48, Math.Floor(stopwatch.Elapsed.TotalMilliseconds).ToString() );
							}

							WriteTileToPhysicalTexture( tile, rect.X, rect.Y );
						}

						VTTilePool.Recycle( tile );
					}
				}
			}
		}
	

		void StressTest()
		{
			if (UpdateStressTest) 
			{
				var tile	=	new VTTile(VTAddress.CreateBadAddress(0));
				tile.FillRandomColor();

				var size	=	VTConfig.PageSizeBordered;;
				int max		=	PhysicalPages0.Width / size;

				for (int i=0; i<MaxPPF; i++) 
				{
					var x = rand.Next( max ) * size;
					var y = rand.Next( max ) * size;

					WriteTileToPhysicalTexture( tile, x, y );
				}
			}
		}


		Random rand = new Random();



		void WriteTileToPhysicalTexture ( VTTile tile, int x, int y )
		{
			using ( new PixEvent("WriteTileToPhysicalTexture")) 
			{
				#if false

				var mipRect	=	new Rectangle( rect.X/2, rect.Y/2, rect.Width/2, rect.Height/2 );

				PhysicalPages0.SetData( 0, rect,	 tile.GetGpuData(0, 0) );
				PhysicalPages1.SetData( 0, rect,	 tile.GetGpuData(1, 0) );
				PhysicalPages2.SetData( 0, rect,	 tile.GetGpuData(2, 0) );

				PhysicalPages0.SetData( 1, mipRect, tile.GetGpuData(0, 1) );
				PhysicalPages1.SetData( 1, mipRect, tile.GetGpuData(1, 1) );
				PhysicalPages2.SetData( 1, mipRect, tile.GetGpuData(2, 1) );

				#else

				StagingTileSrgb		.SetDataRaw		( 0, tile.GetGpuData(0, 0) );
				StagingTileSrgb		.CopyToTexture	( PhysicalPages0, 0, x, y );
				StagingTile			.SetDataRaw		( 0, tile.GetGpuData(1, 0) );
				StagingTile			.CopyToTexture	( PhysicalPages1, 0, x, y );
				StagingTile			.SetDataRaw		( 0, tile.GetGpuData(2, 0) );
				StagingTile			.CopyToTexture	( PhysicalPages2, 0, x, y );

				StagingTileSrgbMip	.SetDataRaw		( 0, tile.GetGpuData(0, 1) );
				StagingTileSrgbMip	.CopyToTexture	( PhysicalPages0, 1, x/2, y/2 );
				StagingTileMip		.SetDataRaw		( 0, tile.GetGpuData(1, 1) );
				StagingTileMip		.CopyToTexture	( PhysicalPages1, 1, x/2, y/2 );
				StagingTileMip		.SetDataRaw		( 0, tile.GetGpuData(2, 1) );
				StagingTileMip		.CopyToTexture	( PhysicalPages2, 1, x/2, y/2 );

				#endif
			}
		}
	}
}
