using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core.Extensions;
using Fusion.Core.Mathematics;
using IronStar.ECS.Serialization;

namespace IronStar.ECS
{
	/// <summary>
	/// #TODO #ECS -- see https://andrewlock.net/benchmarking-4-reflection-methods-for-calling-a-constructor-in-dotnet/ for NEW alternatives.
	/// </summary>
	static class ECSTypeManager
	{
		class ComponentTypeInfo
		{
			public ComponentTypeInfo( Type t, ComponentSerializer s )
			{
				ComponentType	=	t;
				Serializer		=	s;
			}
			public readonly Type ComponentType;
			public readonly ComponentSerializer Serializer;
		}

		static readonly Type[] componentTypes;

		static ECSTypeManager()
		{
			Log.Message("Scanning for ECS components...");

			componentTypes	=	Misc.GatherInterfaceImplementations( typeof(IComponent) );

			for (int i=0; i<componentTypes.Length; i++)
			{
				var type = componentTypes[i];
				Log.Debug("[{0:D2}/{1:D2}] {2:X16} : {3}", i, componentTypes.Length, 1L << i, type.Name);
			}

			if (componentTypes.Length > GameState.MaxComponentTypes)
			{
				throw new InvalidOperationException("ECSTypeManager -- too much component types. Max " + GameState.MaxComponentTypes.ToString());
			}
		}


		public static Type[] GetComponentTypes()
		{
			return componentTypes;
		}


		public static long GetComponentBit( Type componentType )
		{
			for (int index=0; index<componentTypes.Length; index++)
			{
				if (componentTypes[index]==componentType) 
				{
					return (1L << index);
				}
			}
			throw new ArgumentException("Bad component type : {0}", componentType.ToString());
		}


		public static Type GetComponentType( long bit )
		{
			int index = MathUtil.LogBase2( (ulong)bit );
			return componentTypes[index];
		}
	}
}
