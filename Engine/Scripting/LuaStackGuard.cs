using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KopiLua;

namespace Fusion.Scripting {

	/// <summary>
	/// Protectes Lua stack from corruption.
	/// 
	/// Usage:
	///		using ( new LuaStackGuard( returned value count ) ) {
	///			...
	///		}
	/// 
	/// </summary>
	public class LuaStackGuard : IDisposable {

		readonly LuaState L;
		readonly int oldtop;
		readonly int reserved;

		public LuaStackGuard ( LuaState L, int reserved = 0 )
		{
			this.L			=	L;
			this.oldtop		=	Lua.LuaGetTop(L);
			this.reserved	=	reserved;
		}


		public void Dispose()
		{																  
			int newtop = Lua.LuaGetTop(L);
			if (newtop!=oldtop+reserved) {
				Lua.LuaSetTop(L,oldtop);
				Log.Warning("Lua stack corruption : {0}+{1} -> {2}. Restored.", oldtop, reserved, newtop );
			}
		}

	}
}
