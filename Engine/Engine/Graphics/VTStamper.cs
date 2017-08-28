using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Core.Configuration;
using Fusion.Engine.Common;
using Fusion.Drivers.Graphics;
using Fusion.Engine.Imaging;
using System.Threading;
using Fusion.Build.Mapping;

namespace Fusion.Engine.Graphics {

	/// <summary>
	/// 
	/// </summary>
	internal class VTStamper {


		const float StampTimeInterval		=	0.10f;
		const float StampJitterAmplitude	=	0.03f;


		class Stamp {
			public readonly VTTile		Tile;
			public readonly Rectangle	SrcRectangle;
			public readonly Rectangle	SrcRectangleMip;
			public readonly Rectangle	DstRectangle;
			public readonly Rectangle	DstRectangleMip;

			int		counter = 0;
			float	timer	= 0;


			/// <summary>
			/// 
			/// </summary>
			/// <param name="tile"></param>
			/// <param name="rect"></param>
			public Stamp ( VTTile tile, Rectangle rect ) 
			{
				this.Tile				=	tile;
				this.DstRectangle		=	rect;
				this.DstRectangleMip	=	new Rectangle( rect.X/2, rect.Y/2, rect.Width/2, rect.Height/2 );
				this.SrcRectangle		=	new Rectangle( 0,0, DstRectangle.Width,    DstRectangle.Height );
				this.SrcRectangleMip	=	new Rectangle( 0,0, DstRectangleMip.Width, DstRectangleMip.Height );
			}


			/// <summary>
			/// Update internal counters and sets GPU data if necessary
			/// </summary>
			/// <param name="physicalPage"></param>
			/// <param name="dt"></param>
			public void AdvanceStamping ( VTSystem vtSystem, float dt, float jitter )
			{
				timer -= dt;

				if (timer<=0) {
					#if false

					vtSystem.PhysicalPages0.SetData( 0, Rectangle, Tile.GetGpuData(0, 0) );
					vtSystem.PhysicalPages1.SetData( 0, Rectangle, Tile.GetGpuData(1, 0) );
					vtSystem.PhysicalPages2.SetData( 0, Rectangle, Tile.GetGpuData(2, 0) );

					vtSystem.PhysicalPages0.SetData( 1, RectangleMip, Tile.GetGpuData(0, 1) );
					vtSystem.PhysicalPages1.SetData( 1, RectangleMip, Tile.GetGpuData(1, 1) );
					vtSystem.PhysicalPages2.SetData( 1, RectangleMip, Tile.GetGpuData(2, 1) );

					#else

					//vtSystem.StagingTile0.SetData( 0, SrcRectangle, Tile.GetGpuData(0, 0) );
					//vtSystem.StagingTile1.SetData( 0, SrcRectangle, Tile.GetGpuData(1, 0) );
					//vtSystem.StagingTile2.SetData( 0, SrcRectangle, Tile.GetGpuData(2, 0) );

					//vtSystem.StagingTile0.SetData( 1, SrcRectangleMip, Tile.GetGpuData(0, 1) );
					//vtSystem.StagingTile1.SetData( 1, SrcRectangleMip, Tile.GetGpuData(1, 1) );
					//vtSystem.StagingTile2.SetData( 1, SrcRectangleMip, Tile.GetGpuData(2, 1) );

					int x = DstRectangle.X;
					int y = DstRectangle.Y;

					//vtSystem.StagingTile0.CopyToTexture( vtSystem.PhysicalPages0, 0, x, y );
					//vtSystem.StagingTile1.CopyToTexture( vtSystem.PhysicalPages1, 0, x, y );
					//vtSystem.StagingTile2.CopyToTexture( vtSystem.PhysicalPages2, 0, x, y );

					//vtSystem.StagingTile0.CopyToTexture( vtSystem.PhysicalPages0, 1, x, y );
					//vtSystem.StagingTile1.CopyToTexture( vtSystem.PhysicalPages1, 1, x, y );
					//vtSystem.StagingTile2.CopyToTexture( vtSystem.PhysicalPages2, 1, x, y );

					#endif

					counter++;
					timer = StampTimeInterval + jitter;
				}
			}


			/// <summary>
			/// Indicates that given tile is fully imprinted to physical texture.
			/// </summary>
			public bool IsFullyStamped {
				get { return counter>=1; }
			}

		}


		Random rand = new Random();
		Dictionary<Rectangle,Stamp> stamps = new Dictionary<Rectangle,Stamp>();

		
		/// <summary>
		/// Add tile to stamp queue
		/// </summary>
		/// <param name="tile"></param>
		/// <param name="rect"></param>
		public void Add ( VTTile tile, Rectangle rect )
		{
			stamps[ rect ] = new Stamp( tile, rect );
		}


		/// <summary>
		/// Sequntually imprints enqued tiles to physical texture.
		/// </summary>
		/// <param name="physicalPage"></param>
		public void Update ( VTSystem vtSystem, float dt )
		{
			foreach ( var stamp in stamps ) {

				float jitter = rand.NextFloat( -StampJitterAmplitude, StampJitterAmplitude );

				stamp.Value.AdvanceStamping( vtSystem, dt, jitter );

			}	

			stamps = stamps.Where( pair => !pair.Value.IsFullyStamped ).ToDictionary( pair => pair.Key, pair => pair.Value );
		}
		
	}
}
