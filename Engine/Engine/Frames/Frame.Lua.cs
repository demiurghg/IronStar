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

		#region Handlers
		[LuaApi("on_tick"		)]	LuaValue LOnTick		{ get; set;	}
		[LuaApi("on_click"		)]	LuaValue LOnClick		{ get; set;	}
		[LuaApi("on_dclick"		)]	LuaValue LOnDClick		{ get; set;	}
		[LuaApi("on_move"		)]	LuaValue LOnMove		{ get; set;	}
		[LuaApi("on_resize"		)]	LuaValue LOnResize		{ get; set;	}
		[LuaApi("on_mouse_down" )]	LuaValue LOnMouseDown	{ get; set;	}
		[LuaApi("on_mouse_up"   )]	LuaValue LOnMouseUp		{ get; set;	}
		[LuaApi("on_mouse_move" )]	LuaValue LOnMouseMove	{ get; set;	}
		[LuaApi("on_mouse_in"   )]	LuaValue LOnMouseIn		{ get; set;	}
		[LuaApi("on_mouse_out"  )]	LuaValue LOnMouseOut	{ get; set;	}
		[LuaApi("on_mouse_wheel")]	LuaValue LOnMouseWheel	{ get; set;	}
		[LuaApi("on_hover"		)]	LuaValue LOnHover		{ get; set;	}
		[LuaApi("on_press"		)]	LuaValue LOnPress		{ get; set;	}
		[LuaApi("on_release"	)]	LuaValue LOnRelese		{ get; set;	}


		void CallHandler ( LuaValue handler )
		{
			if (handler!=null && handler.IsFunction) {
				var L = handler.L;

				handler.LuaPushValue(L);

				LuaUtils.PushObject( L, this );
				LuaUtils.LuaSafeCall(L,1,0);
			}
		}


		void CallHandler ( LuaValue handler, int x, int y, Keys key )
		{
			if (handler!=null && handler.IsFunction) {
				var L = handler.L;

				handler.LuaPushValue(L);

				LuaUtils.PushObject( L, this );
				Lua.LuaPushNumber(L, x);
				Lua.LuaPushNumber(L, y);
				Lua.LuaPushString(L, key.ToString().ToLowerInvariant());

				LuaUtils.LuaSafeCall(L,4,0);
			}
		}

		#endregion


		#region Hierarchy, position and size
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


		[LuaApi("clear")]
		public int LRemoveAll ( LuaState L )
		{			
			Clear();
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
		#endregion


		#region Borders, padding and anchors
		[LuaApi("set_borders")]
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


		[LuaApi("set_padding")]
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
		#endregion


		#region Text
		[LuaApi("set_text")]
		int LSetText ( LuaState L )
		{
			Text	=	LuaUtils.ExpectString( L, 1, "frame text" );
			return 0;
		}


		[LuaApi("get_text")]
		int LGetText ( LuaState L )
		{
			Lua.LuaPushString( L, Text );
			return 1;
		}


		[LuaApi("set_font")]
		int LSetTextFont ( LuaState L )
		{
			var font = LuaUtils.ExpectString(L, 1, "sprite font path");
			Font = ui.Game.Content.Load<SpriteFont>(font);

			return 0;
		}
		#endregion




		#region
		[LuaApi("set_image")]
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

		#endregion

	}
}

