using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Premotion.Mansion.Http.ExceptionHandlers
{
	/// <summary>
	/// Tries to handle <see cref="SocketException"/>s.
	/// </summary>
	public class SocketExceptionHandler : ExceptionHandler
	{
		private readonly BackoffStrategy backoffStrategy;
		/// <summary>
		/// Creates a exception handler which handles <see cref="SocketException"/>s.
		/// </summary>
		/// <param name="backoffStrategy">The <see cref="backoffStrategy"/> which to use.</param>
		public SocketExceptionHandler(BackoffStrategy backoffStrategy)
		{
			// validate arguments
			if (backoffStrategy == null)
				throw new ArgumentNullException("backoffStrategy");

			// set values
			this.backoffStrategy = backoffStrategy;
		}
		/// <summary>
		/// Tries the handle the given <paramref name="exception"/>.
		/// </summary>
		/// <param name="exception">The <see cref="Exception"/> which to try to handle.</param>
		/// <param name="reconnect">A callback invoked when a reconnection attempt can be made.</param>
		/// <returns>Returns true if the error is handled, otherwise false.</returns>
		protected override bool DoTryHandle(Exception exception, Func<Task> reconnect)
		{
			// validate arguments
			if (exception == null)
				throw new ArgumentNullException("exception");
			if (reconnect == null)
				throw new ArgumentNullException("reconnect");
			CheckDisposed();

			// check if this is indeed a socket exception
			var socketException = exception as SocketException;
			if (socketException == null && exception.InnerException != null)
				socketException = exception.InnerException as SocketException;
			if (socketException == null)
				return false;

			// use the backoff strategy to reconnect
			backoffStrategy.Retry(reconnect);

			// we handled it
			return true;
		}
		/// <summary>
		/// Clears the exception handler state.
		/// </summary>
		public override void Clear()
		{
			CheckDisposed();
			backoffStrategy.Reset();
		}
		/// <summary>
		/// Dispose resources. Override this method in derived classes. Unmanaged resources should always be released
		/// when this method is called. Managed resources may only be disposed of if disposeManagedResources is true.
		/// </summary>
		/// <param name="disposeManagedResources">A value which indicates whether managed resources may be disposed of.</param>
		protected override void DisposeResources(bool disposeManagedResources)
		{
			// check for unmanaged resource disposal
			if (!disposeManagedResources)
				return;

			backoffStrategy.Dispose();
		}
	}
}