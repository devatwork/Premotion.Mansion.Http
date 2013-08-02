using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Premotion.Mansion.Http.ExceptionHandlers
{
	/// <summary>
	/// Tries to handle <see cref="WebException"/>s.
	/// </summary>
	public class HttpExceptionHandler : ExceptionHandler
	{
		#region Constructors
		/// <summary>
		/// Creates a exception handler which handles <see cref="SocketException"/>s.
		/// </summary>
		/// <param name="backoffStrategy">The <see cref="backoffStrategy"/> which to use.</param>
		/// <param name="recoverableStatusCodes">The <see cref="HttpStatusCode"/> from which can be recovered.</param>
		public HttpExceptionHandler(BackoffStrategy backoffStrategy, IEnumerable<int> recoverableStatusCodes)
		{
			// validate arguments
			if (backoffStrategy == null)
				throw new ArgumentNullException("backoffStrategy");
			if (recoverableStatusCodes == null)
				throw new ArgumentNullException("recoverableStatusCodes");

			// set values
			this.recoverableStatusCodes = recoverableStatusCodes.ToArray();
			this.backoffStrategy = backoffStrategy;
		}
		#endregion
		#region ExceptionHandler Members
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

			// check if this is indeed a web exception
			var webException = exception as WebException;
			if (webException == null)
				return false;

			// try to get the status code
			if (webException.Response == null)
				return false;
			var response = webException.Response as HttpWebResponse;
			if (response == null)
				return false;
			var statusCode = response.StatusCode;

			// check if the status code is not a recoverable status code
			if (!recoverableStatusCodes.Contains((int) statusCode))
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
		#endregion
		#region Private Fields
		private readonly int[] recoverableStatusCodes;
		private readonly BackoffStrategy backoffStrategy;
		#endregion
	}
}