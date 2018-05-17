using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fusion.Core.Extensions {

	public static class GameExtensions
	{

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="game"></param>
		/// <param name="component"></param>
		public static void AddServiceAndComponent(this Game game, IGameComponent component)
		{
			int order = game.Components.Count;

			if (component is GameComponent) {
				(component as GameComponent).UpdateOrder = order;
			}

			game.Components.Add(component);
			game.Services.AddService(component.GetType(), component);
		}


		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="game"></param>
		/// <returns></returns>
		public static T GetService<T>(this Game game) where T: class
		{
			return (T)game.Services.GetService<T>();
		}
	}
}
