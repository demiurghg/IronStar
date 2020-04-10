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
			var delta  = newtop - (oldtop+reserved);

			if (newtop!=oldtop+reserved) {

				Lua.LuaSetTop(L,oldtop);

				string reason = "unknown";

				if (newtop > oldtop+reserved) {
					reason = delta.ToString() + " extra value(s) is/are pushed on stack";
				}

				if (newtop < oldtop+reserved) {
					reason = (-delta).ToString() + " values was/were popped from stack";
				}

				Log.Warning("Lua stack corruption : {0}. Restored.", reason); 
				//Log.Warning("Lua stack corruption : top[{0}] + reserved[{1}] != current{2}. Restored.", oldtop, reserved, newtop );
			}
		}

	}
}
