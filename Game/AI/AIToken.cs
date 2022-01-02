using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using IronStar.ECS;

namespace IronStar.AI
{
	public class AIToken
	{
		readonly TimeSpan cooldown;
		TimeSpan timer;
		bool acquired;
		Entity owner;

		public Entity Owner { get { return owner; } }


		public AIToken( TimeSpan cooldown )
		{
			this.cooldown	=	cooldown;
		}

		
		public void Update ( GameTime gameTime )
		{
			if (timer>TimeSpan.Zero) 
			{	
				timer -= gameTime.Elapsed;
			}
		}

		
		public void Acquire(Entity owner)
		{
			if (acquired) 
			{
				throw new InvalidOperationException("AI token is already acquired");
			}
			Log.Debug("Token acquired by #{0}", owner.ID);
			acquired = true;
			this.owner = owner;
		}


		public void Restore(Entity deadOwner)
		{
			if (owner==deadOwner) 
			{
				Log.Debug("Token restored");
				Release();
			}
		}


		public void Release()
		{
			if (!acquired)
			{
				throw new InvalidOperationException("AI token is already released");
			}
			else
			{
				Log.Debug("Token released by #{0}", owner.ID);
				acquired = false;
				owner = null;
				timer = cooldown;
			}
		}

		
		public bool IsReady
		{
			get 
			{
				return (timer <= TimeSpan.Zero) && !acquired;
			}
		}
	}
}
