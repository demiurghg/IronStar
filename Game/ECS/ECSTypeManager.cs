﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Extensions;
using Fusion.Core.Mathematics;

namespace IronStar.ECS
{
	static class ECSTypeManager
	{
		static List<Type> componentTypes	= new List<Type>();


		public static void Scan()
		{
			componentTypes	=	Misc.GatherInterfaceImplementations( typeof(IComponent) )
									.ToList();

			if (componentTypes.Count > GameState.MaxComponentTypes)
			{
				throw new InvalidOperationException("ECSTypeManager -- too much component types. Max " + GameState.MaxComponentTypes.ToString());
			}
		}


		public static Type[] GetComponentTypes()
		{
			return componentTypes.ToArray();
		}


		public static long GetComponentBit( Type componentType )
		{
			int index = componentTypes.IndexOf( componentType );
			return index < 0L ? 0L : (1L << index);
		}


		public static Type GetComponentType( long bit )
		{
			int index = MathUtil.LogBase2( (ulong)bit );
			return componentTypes[index];
		}
	}
}
