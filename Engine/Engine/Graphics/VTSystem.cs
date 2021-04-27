﻿using System;
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

namespace Fusion.Engine.Graphics {

	[RequireShader("vtcache")]
	internal class VTSystem : GameComponent {

		readonly RenderSystem rs;

		[Config]
		[AECategory("Performamce")]
		[Description("Max uploaded to physical texture pages per frame")]
		public int MaxPPF { get; set; }

		[Config]
		[AECategory("Debugging")]
		public bool ShowPageCaching { get; set; }

		[Config]
		[AECategory("Debugging")]
		public bool ShowPageLoads { get; set; }

		[Config]
		[AECategory("Debugging")]
		[Description("Enables displaying of physical texture")]
		public bool ShowPhysicalTextures { get; set; }

		[Config]
		[AECategory("Debugging")]
		public bool ShowPageTexture { get; set; }

		[Config]
		[AECategory("Tiles")]
		[Description("Shows tile border for each uploaded tile")]
		public bool ShowTileBorder { get; set; }

		[Config]
		[AECategory("Tiles")]
		[Description("Disables virtual texturing feedback")]
		public bool LockTiles { get; set; }

		[Config]
		[AECategory("Tiles")]
		[Description("Shows tile address for each uploaded tile")]
		public bool ShowTileAddress { get; set; }

		[Config]
		[AECategory("Tiles")]
		[Description("Fills each tile with checkers, for filtering debugging")]
		public bool ShowTileCheckers { get; set; }

		[Config]
		[AECategory("Debugging")]
		[Description("Fills each tile with checkers, for filtering debugging")]
		public bool UpdateStressTest { get; set; }

		[Config]
		[AECategory("Tiles")]
		[Description("Fills each tile mip level with solid colors to debug mip transitions")]
		public bool ShowMipLevels { get; set; }

		[Config]
		[AECategory("Tiles")]
		[Description("Fills each tile with random color.")]
		public bool RandomColor { get; set; }

		[Config]
		[AECategory("Performamce")]
		[Description("Texture LOD bias.")]
		public int LodBias { get; set; } = 0;

		[Config]
		[AECategory("Debugging")]
		public bool ShowThrashing { get; set; } = false;

		[Config]
		[AECategory("Tiles")]
		public bool ShowDiffuse { get; set; } = false;

		[Config]
		[AECategory("Tiles")]
		public bool ShowSpecular { get; set; } = false;

		[Config]
		[AECategory("Tiles")]
		[Description("Show thrashing.")]
		public bool ShowMirror { get; set; } = false;

		[Config]
		[AECategory("Performamce")]
		[Description("Enables and disables anisotropic filtering.")]
		public bool UseAnisotropic { get; set; }

		[Config]
		[AECategory("Debugging")]
		public float DebugGradientScale { get; set; } = 1;

		[Config]
		[AECategory("Performamce")]
		[Description("Size of physical texture")]
		public int PhysicalSize 
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
		int physicalSize = 1024;
		bool physicalSizeDirty = true;


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

			MaxPPF	=	16;
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

			tileLoader		=	new VTTileLoader( this, Game.Content.VTStorage );

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



		/// <summary>
		/// 
		/// </summary>
		void ApplyVTState ()
		{
			if (physicalSizeDirty) {

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

				PageTable			=	new RenderTarget2D( rs.Device, ColorFormat.Rgba32F, tableSize, tableSize, true, true );
				PageData			=	new StructuredBuffer( rs.Device, typeof(PageGpu), maxTiles, StructuredBufferFlags.None );
				pageDataCpu			=	new PageGpu[ maxTiles ];
				Params				=	new ConstantBuffer( rs.Device, 16 );

				tileCache			=	new VTTileCache( physPages, physicalSize );

				PageScaleRCP		=	VTConfig.PageSize / (float)physSize;
				
				physicalSizeDirty	=	false;
			}
		}



		Texture2D CreateMipSelectorTexture ()
		{
			int mips = VTConfig.MaxMipLevel;
			int size = 1 << VTConfig.MaxMipLevel;

			var mipIndex		=	new Texture2D( rs.Device, size, size, ColorFormat.R32F, mips, false );

			for (int i=0; i<=mips; i++) {
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
			if (disposing) {

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
		}



		/// <summary>
		/// 
		/// </summary>
		public void Stop ()
		{
			tileLoader.Purge();
			tileCache.Purge();
		}


		const int	BlockSizeX		=	16;
		const int	BlockSizeY		=	16;


		/// <summary>
		///	Updates page table using GPU
		/// </summary>
		void UpdatePageTable ()
		{
			int tableSize	=	VTConfig.VirtualPageCount;
			PageGpu[] pages;

			using (new PixEvent("UpdatePageTable")) {

				var device = Game.GraphicsDevice;
				device.ResetStates();


				using ( new CVEvent( "GetGpuPageData" ) ) {
					pages = tileCache.GetGpuPageData();
				}

				
				using ( new CVEvent( "SetData" ) ) {
					if ( pages.Any() ) {
						pages.CopyTo( pageDataCpu, 0 );
						//PageData.SetData( pages );
						PageData.UpdateData( pageDataCpu );
					}
				}

				for (int mip=0; mip<VTConfig.MipCount; mip++) {

					using ( new CVEvent( "Dispatch Pass" ) ) {

						Params.SetData( new Int4( pages.Length, mip, 0,0 ) );

						device.PipelineState	=	factory[0];

						device.ComputeConstants[0]	=	Params;
						device.ComputeResources[0]	=	PageData;
						device.SetComputeUnorderedAccess( 0, PageTable.GetSurface(mip).UnorderedAccess );

						int targetSize	=	tableSize >> mip;
						int groupCountX	=	MathUtil.IntDivUp( targetSize, BlockSizeX );
						int groupCountY	=	MathUtil.IntDivUp( targetSize, BlockSizeY );

						device.Dispatch( groupCountX, groupCountY, 1 );
					}
				}
			}
		}

		

		/// <summary>
		/// 
		/// </summary>
		/// <param name="data"></param>
		public void Update ( VTAddress[] data, GameTime gameTime )
		{
			using ( new PixEvent( "VT Update" ) ) {

				var feedback = data.Distinct().Where( p => p.Dummy!=0 ).ToArray();

				ApplyVTState();

				List<VTAddress> feedbackTree = new List<VTAddress>();

				//	
				//	Build tree :
				//
				foreach ( var addr in feedback ) {

					var paddr = addr;

					if (addr.MipLevel<LodBias) {
						continue;
					}

					feedbackTree.Add( paddr );

					while (paddr.MipLevel < VTConfig.MaxMipLevel) {
						paddr = VTAddress.FromChild( paddr );
						feedbackTree.Add( paddr );
					}

				}

				//
				//	Distinct :
				//	
				feedbackTree = feedbackTree
					.Distinct()
					//.Where( p0 => tileCache.Contains(p0) )
					.OrderByDescending( p1 => p1.MipLevel )
					.ToList();//*/


				//
				//	Detect thrashing and prevention
				//	Get highest mip, remove them, repeat until no thrashing occur.
				//
				while (feedbackTree.Count >= tileCache.Capacity * 2 / 3 ) {

					if (ShowThrashing) {
						Log.Warning("VT thrashing: r:{0} a:{1}", feedbackTree.Count, tileCache.Capacity);
					}

					feedbackTree = feedbackTree.Select( a1 => a1.IsLeastDetailed ? a1 : a1.GetLessDetailedMip() )
						.Distinct()
						.OrderByDescending( p1 => p1.MipLevel )
						.ToList()
						;
				}


				if (LockTiles) {
					feedbackTree.Clear();
				}


				if (tileCache!=null) {
				}

				//
				//	Put into cache :
				//
				if (tileCache!=null && tileLoader!=null) {

					int counter = 0;

					foreach ( var addr in feedbackTree ) {
				
						int physAddr;

						if ( tileCache.Add( addr, out physAddr ) ) {

							//Log.Message("...vt tile cache: {0} --> {1}", addr, physAddr );

							tileLoader.RequestTile( addr );

							counter++;
						}

						if (counter>MaxPPF) {
							break;
						}
					}
				}

				//
				//	update table :
				//
				if (tileLoader!=null && tileCache!=null) {

					for (int i=0; i<MaxPPF; i++) {
				
						VTTile tile;

						if (tileLoader.TryGetTile( out tile )) {

							Rectangle rect;

							if (tileCache.TranslateAddress( tile.VirtualAddress, tile, out rect )) {
							
								var sz = VTConfig.PageSizeBordered;

								if (RandomColor) {	
									tile.FillRandomColor();
								}

								if (ShowTileCheckers) {
									tile.DrawChecker();
								}

								if (ShowTileAddress) {	
									tile.DrawText( 16,16, tile.VirtualAddress.ToString() );
									tile.DrawText( 16,32, string.Format("{0} {1}", rect.X/sz, rect.Y/sz ) );
									tile.DrawText( 16,48, Math.Floor(stopwatch.Elapsed.TotalMilliseconds).ToString() );
								}

								if (ShowTileBorder) {
									tile.DrawBorder();
								}

								if (ShowMipLevels) {
									tile.DrawMipLevels(ShowTileBorder);
								}

								if (ShowDiffuse) {
									tile.MakeWhiteDiffuse();
								}

								if (ShowSpecular) {
									tile.MakeGlossyMetal();
								}

								if (ShowMirror) {
									tile.MakeMirror();
								}

								WriteTileToPhysicalTexture( tile, rect.X, rect.Y );
							}

							VTTilePool.Recycle( tile );
						}

					}

					if (UpdateStressTest) {
					
						var tile	=	new VTTile(VTAddress.CreateBadAddress(0));
						tile.FillRandomColor();

						var size	=	VTConfig.PageSizeBordered;;
						int max		=	PhysicalPages0.Width / size;

						for (int i=0; i<MaxPPF; i++) {

							var x = rand.Next( max ) * size;
							var y = rand.Next( max ) * size;

							WriteTileToPhysicalTexture( tile, x, y );
						}
					}


					//	update page table :
					UpdatePageTable();
				}
			}
		}


		Random rand = new Random();



		/// <summary>
		/// 
		/// </summary>
		/// <param name="tile"></param>
		/// <param name="rect"></param>
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
