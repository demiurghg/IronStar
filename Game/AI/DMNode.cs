using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.AI
{
	public enum DMNode
	{
		Dead,
		Stand,
		StandGaping,
		Roaming,
		CombatRoot,
		CombatChase,
		CombatAttack,
		CombatMove,
		CombatRunToCover,
		CombatStayCover,
	}
}
