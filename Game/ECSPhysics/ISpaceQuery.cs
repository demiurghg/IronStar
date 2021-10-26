﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BEPUphysics;

namespace IronStar.ECSPhysics
{
	public interface ISpaceQuery<TResult>
	{
		TResult Execute( Space space );
	}
}
