using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;

namespace IronStar.AI
{
	public class StateMachine<TState,TData,TInput>
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

		
		public void Update ( GameTime gameTime, TData data, TInput input )
		{
			var method		=	stateBinding[ state ];
			var oldState	=	state;
			var newState	=	(TState)method.Invoke( this, new object[] { data, input } );

			if (!oldState.Equals(newState))
			{
				state	=	newState;
				Transition( data, input, oldState, newState );
			}
		}


		protected virtual void Transition( TData data, TInput input, TState previous, TState next )
		{
		}
	}
}
