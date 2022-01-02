using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using IronStar.ECS;
using IronStar.Gameplay.Components;

namespace IronStar.AI
{
	public class AITokenPool
	{
		Dictionary<Team,AIToken[]> tokens;

		public AITokenPool( int count, TimeSpan cooldown )
		{
			tokens	=	new Dictionary<Team,AIToken[]>();

			foreach ( var team in Enum.GetValues(typeof(Team)) )
			{
				tokens[(Team)team] = new AIToken[ count ];

				for (int i=0; i<count; i++)
				{
					tokens[(Team)team][i] = new AIToken(cooldown);
				}
			}

		}


		public void Update( GameTime gameTime )
		{
			foreach (var tokenArray in tokens)
			{
				foreach ( var token in tokenArray.Value )
				{
					token.Update(gameTime);
				}
			}
		}


		public AIToken Acquire(Team team, Entity owner)
		{
			foreach (var token in tokens[team])
			{
				if (token.IsReady)
				{
					token.Acquire(owner);
					return token;
				}
			}

			return null;
		}
	}
}
