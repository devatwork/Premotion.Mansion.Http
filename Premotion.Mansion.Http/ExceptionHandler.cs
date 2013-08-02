using System;
using System.Threading.Tasks;
using Premotion.Mansion.Http.Patterns;

namespace Premotion.Mansion.Http
{
	/// <summary>
	/// Base class for all exception handlers.
	/// </summary>
	public abstract class ExceptionHandler : DisposableBase
	{
		/// <summary>
		/// Tries the handle the given <paramref name="exception"/>.
		/// </summary>
		/// <param name="exception">The <see cref="Exception"/> which to try to handle.</param>
		/// <param name="reconnect">A callback invoked when a reconnection attempt can be made.</param>
		/// <returns>Returns true if the error is handled, otherwise false.</returns>
		public bool TryHandle(Exception exception, Func<Task> reconnect)
		{
			// validate arguments
			if (exception == null)
				throw new ArgumentNullException("exception");
			if (reconnect == null)
				throw new ArgumentNullException("reconnect");

			// invoke template method
			return DoTryHandle(exception, reconnect);
		}
		/// <summary>
		/// Tries the handle the given <paramref name="exception"/>.
		/// </summary>
		/// <param name="exception">The <see cref="Exception"/> which to try to handle.</param>
		/// <param name="reconnect">A callback invoked when a reconnection attempt can be made.</param>
		/// <returns>Returns true if the error is handled, otherwise false.</returns>
		protected abstract bool DoTryHandle(Exception exception, Func<Task> reconnect);
		/// <summary>
		/// Clears the exception handler state.
		/// </summary>
		public abstract void Clear();
	}
}