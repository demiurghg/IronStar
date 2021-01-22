using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;

namespace IronStar.AI
{
	public class StateMachine<TState,TInput>
	{
		readonly Type stateType;
		readonly Dictionary<TState,MethodInfo> stateBinding;

		TState state;

		public StateMachine(TState initialState)
		{
			state		=	initialState;
			stateType	=	typeof(TState);

			if (!stateType.IsEnum) throw new ArgumentException("TState is not enum");

			stateBinding	=	new Dictionary<TState, MethodInfo>();

			foreach ( var value in Enum.GetValues(stateType) )
			{
				var methodInfo = GetType().GetMethod( value.ToString(), BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance);
				
				if (methodInfo==null) throw new ArgumentException(string.Format("Class does not contain method with enum name '{0}.{1}'", stateType.Name, value));

				stateBinding.Add( (TState)value, methodInfo );
			}
		}

		
		public void Update ( TInput input )
		{
			var method		=	stateBinding[ state ];
			var oldState	=	state;
			var newState	=	(TState)method.Invoke( this, new object[] { input } );

			if (!oldState.Equals(newState))
			{
				state	=	newState;
				Transition( oldState, newState );
			}
		}


		protected virtual void Transition( TState previous, TState next )
		{
		}
	}
}
