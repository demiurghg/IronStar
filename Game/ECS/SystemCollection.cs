﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.ECS
{
	class SystemCollection : IEnumerable<SystemWrapper>
	{
		readonly GameState gs;
		readonly object lockObj = new object();
		readonly List<SystemWrapper> systemWrappers;

		public SystemCollection(GameState gs)
		{
			this.gs			=	gs;
			systemWrappers	=	new List<SystemWrapper>( GameState.MaxSystems );
		}
			

		public void Add( ISystem system )
		{
			lock (lockObj)
			{
				systemWrappers.Add( new SystemWrapper( gs, system ) );
			}
		}


		public IEnumerator<SystemWrapper> GetEnumerator()
		{
			return ((IEnumerable<SystemWrapper>)systemWrappers).GetEnumerator();
		}


		IEnumerator IEnumerable.GetEnumerator()
		{
			return systemWrappers.GetEnumerator();
		}
	}
}
