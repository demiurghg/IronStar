using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using Fusion;
using Fusion.Core.Mathematics;
using Fusion.Engine.Input;
using Fusion.Engine.Graphics;
using Fusion.Engine.Common;
using Forms = System.Windows.Forms;
using KopiLua;
using Fusion.Core.Shell;

namespace Fusion.Engine.Frames {

	public partial class Frame {

		[LuaApi("setText")]
		int LSetText ( LuaState L )
		{
			Text	=	LuaUtils.ExpectString( L, 1, "frame text" );
			return 0;
		}


		[LuaApi("add")]
		int LAdd ( LuaState L )
		{
			Add( LuaUtils.Expect<Frame>( L, 1, "child frame" ) );
			return 0;
		}


		[LuaApi("remove")]
		int LRemove ( LuaState L )
		{
			Remove( LuaUtils.Expect<Frame>( L, 1, "child frame" ) );
			return 0;
		}


		[LuaApi("removeAll")]
		public int LRemoveAll ( LuaState L )
		{			
			children.Clear();
			return 0;
		}


		[LuaApi("move")]
		int LMove ( LuaState L )
		{
			int x = LuaUtils.ExpectInteger(L,1,"x position");
			int y = LuaUtils.ExpectInteger(L,2,"y position");
			X = x;
			Y = y;
			return 0;
		}


		[LuaApi("resize")]
		int LResize ( LuaState L )
		{
			int w = LuaUtils.ExpectInteger(L,1,"width");
			int h = LuaUtils.ExpectInteger(L,2,"height");
			Width = w;
			Height = h;
			return 0;
		}


		[LuaApi("anchor")]
		int LAnchor ( LuaState L )
		{
			var s = LuaUtils.ExpectString(L, 1, "anchor (all|L|R|B|T)").ToLowerInvariant();
			if (s=="all") {
				Anchor = FrameAnchor.All;
			} else {
				var anchor = FrameAnchor.None;
				if (s.Contains('l')) anchor |= FrameAnchor.Left;
				if (s.Contains('r')) anchor |= FrameAnchor.Right;
				if (s.Contains('b')) anchor |= FrameAnchor.Bottom;
				if (s.Contains('t')) anchor |= FrameAnchor.Top;
			}
			return 0;
		}


		[LuaApi("setFont")]
		int LSetFont ( LuaState L )
		{
			try {
				var font = LuaUtils.ExpectString(L, 1, "sprite font path");
				Font = ui.Game.Content.Load<SpriteFont>(font);
			} catch ( Exception e ) {
				LuaUtils.LuaError(L, e.Message);
			}
			return 0;
		}


		[LuaApi("setImage")]
		int LSetImage ( LuaState L )
		{
			try {
				var image = LuaUtils.ExpectString(L, 1, "image path");
				Image = ui.Game.Content.Load<DiscTexture>(image);
			} catch ( Exception e ) {
				LuaUtils.LuaError(L, e.Message);
			}
			return 0;
		}


		[LuaApi("setBorder")]
		int LSetBorder ( LuaState L )
		{
			var n = Lua.LuaGetTop(L);

			if (n==1) {
				Border = LuaUtils.ExpectInteger( L, 1, "border width" );
			} else if (n==4) {
				BorderLeft	 = LuaUtils.ExpectInteger( L, 1, "left border width" );
				BorderRight  = LuaUtils.ExpectInteger( L, 2, "right border width" );
				BorderTop	 = LuaUtils.ExpectInteger( L, 3, "top border width" );
				BorderBottom = LuaUtils.ExpectInteger( L, 4, "bottom border width" );
			} else {
				LuaUtils.LuaError(L, "setBorder() require 1 or 4 arguments");
			}

			return 0;
		}


		[LuaApi("setPadding")]
		int LSetPadding ( LuaState L )
		{
			var n = Lua.LuaGetTop(L);

			if (n==1) {
				Padding = LuaUtils.ExpectInteger( L, 1, "padding width" );
			} else if (n==4) {
				PaddingLeft		= LuaUtils.ExpectInteger( L, 1, "left padding width" );
				PaddingRight	= LuaUtils.ExpectInteger( L, 2, "right padding width" );
				PaddingTop		= LuaUtils.ExpectInteger( L, 3, "top padding width" );
				PaddingBottom	= LuaUtils.ExpectInteger( L, 4, "bottom padding width" );
			} else {
				LuaUtils.LuaError(L, "setPadding() require 1 or 4 arguments");
			}

			return 0;
		}


		[LuaApi("click")]
		LuaValue LClick {
			get; set;
		}
	}
}

