using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using IronStar.ECS;

namespace IronStar.AI
{
	public class AITokenPool : IEnumerable<AIToken>
	{
		AIToken[] tokens;

		public AITokenPool( int count, TimeSpan cooldown )
		{
			tokens	=	new AIToken[count];

			for (int i=0; i<count; i++)
			{
				tokens[i] = new AIToken(cooldown);
			}
		}


		public void Update( GameTime gameTime )
		{
			foreach (var token in tokens)
			{
				token.Update(gameTime);
			}
		}


		public void RestoreTokens(Entity deadEntity)
		{
			foreach (var token in tokens)
			{
				token.Restore(deadEntity);
			}
		}


		public AIToken Acquire(Entity owner)
		{
			foreach (var token in tokens)
			{
				if (token.IsReady)
				{
					token.Acquire(owner);
					return token;
				}
			}

			return null;
		}

		public IEnumerator<AIToken> GetEnumerator()
		{
			return ( (IEnumerable<AIToken>)tokens ).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ( (IEnumerable<AIToken>)tokens ).GetEnumerator();
		}
	}
}
