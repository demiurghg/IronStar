using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Core {

	public class GameServiceContainer : IServiceProvider {

		readonly Dictionary<Type, object> services;

							
		/// <summary>
		/// Initializes a new instance of this class, which represents a collection of game services.
		/// </summary>
		public GameServiceContainer()
		{
			services = new Dictionary<Type, object>();
		}


		/// <summary>
		/// Adds a service to the GameServiceContainer.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="provider"></param>
		public void AddService(Type type, object provider)
		{
			if (type==null) {
				throw new ArgumentNullException("type");
			}
			if (provider==null) {
				throw new ArgumentNullException("provider");
			}

			if (!type.IsAssignableFrom(provider.GetType())) {
				throw new ArgumentException("The provider does not match the specified service type!");
			}

			services.Add(type, provider);
		}


		/// <summary>
		/// Gets the object providing a specified service.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public object GetService(Type type)
		{
			if (type==null) {
				throw new ArgumentNullException("type");
			}
						
			object service;
			if (services.TryGetValue(type, out service)) {
				return service;
			}

			return null;
		}


		/// <summary>
		/// Removes the object providing a specified service.
		/// </summary>
		/// <param name="type"></param>
		public void RemoveService(Type type)
		{
			if (type==null) {
				throw new ArgumentNullException("type");
			}

			services.Remove(type);
		}
		

		/// <summary>
		/// Adds a service to the GameServiceContainer.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="provider"></param>
		public void AddService<T>(T provider)
		{
			AddService(typeof(T), provider);
		}


		/// <summary>
		/// Gets the object providing a specified service.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public T GetService<T>() where T : class
		{
			var service = GetService(typeof(T));

			if (service==null) {
				return null;
			}

			return (T)service;
		}
	}
}
