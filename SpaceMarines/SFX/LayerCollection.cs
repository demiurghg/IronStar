using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Extensions;
using Fusion.Core.Mathematics;
using Fusion.Engine.Graphics;

namespace SpaceMarines.SFX {
	public class LayerCollection : DisposableBase {

		SpriteLayer background;
		SpriteLayer tiles;
		SpriteLayer decals;
		SpriteLayer entities;
		SpriteLayer sfx;
		SpriteLayer info;

		public SpriteLayer Background	{ get { return background	; } }
		public SpriteLayer Tiles		{ get { return tiles		; } }
		public SpriteLayer Decals		{ get { return decals		; } }
		public SpriteLayer Entities		{ get { return entities		; } }
		public SpriteLayer SFX			{ get { return sfx			; } }
		public SpriteLayer Info			{ get { return info			; } }

		readonly RenderSystem rs;

		public LayerCollection ( RenderSystem rs )
		{
			this.rs			=	rs;

			background	=	new SpriteLayer( rs, 16384 );
			tiles		=	new SpriteLayer( rs, 16384 );
			decals		=	new SpriteLayer( rs, 16384 );
			entities	=	new SpriteLayer( rs, 16384 );
			sfx			=	new SpriteLayer( rs, 16384 );
			info		=	new SpriteLayer( rs, 16384 );

			background	.Order	=	10;
			tiles		.Order	=	11;
			decals		.Order	=	12;
			entities	.Order	=	13;
			sfx			.Order	=	14;
			info		.Order	=	15;

			tiles.FilterMode	=	SpriteFilterMode.LinearClamp;
			tiles.Transform		=	Matrix.Translation(-32,-18,0);
			tiles.UseProjection	=	true;
			tiles.Projection	=	Matrix.OrthoRH(64,36,-1024,1024);

			rs.SpriteLayers.Add( background );
			rs.SpriteLayers.Add( tiles		);
			rs.SpriteLayers.Add( decals		);
			rs.SpriteLayers.Add( entities	);
			rs.SpriteLayers.Add( sfx		);
			rs.SpriteLayers.Add( info		);
		}


		protected override void Dispose( bool disposing )
		{
			if (disposing) {

				rs.SpriteLayers.Remove( background	);
				rs.SpriteLayers.Remove( tiles		);
				rs.SpriteLayers.Remove( decals		);
				rs.SpriteLayers.Remove( entities	);
				rs.SpriteLayers.Remove( sfx			);
				rs.SpriteLayers.Remove( info		);

				SafeDispose( ref background	);
				SafeDispose( ref tiles		);
				SafeDispose( ref decals		);
				SafeDispose( ref entities	);
				SafeDispose( ref sfx		);
				SafeDispose( ref info		);
			}

			base.Dispose( disposing );
		}


		public void Clear()
		{
			background	.Clear();
			tiles		.Clear();
			decals		.Clear();
			entities	.Clear();
			sfx			.Clear();
			info		.Clear();
		}

	}
}
