using System;
using System.Threading.Tasks;
using Premotion.Mansion.Http.Patterns;

namespace Premotion.Mansion.Http
{
	/// <summary>
	/// Base class for backoff strategies.
	/// </summary>
	public abstract class BackoffStrategy : DisposableBase
	{
		/// <summary>
		/// Retries to execute the operation.
		/// </summary>
		/// <param name="operation">The operation which to execute on a retry attempt.</param>
		public void Retry(Func<Task> operation)
		{
			// validate arguments
			if (operation == null)
				throw new ArgumentNullException("operation");

			// invoke template method
			DoRetry(operation);
		}
		/// <summary>
		/// Retries to execute the operation.
		/// </summary>
		/// <param name="operation">The operation which to execute on a retry attempt.</param>
		protected abstract void DoRetry(Func<Task> operation);
		/// <summary>
		/// Resets the timeout.
		/// </summary>
		public abstract void Reset();
	}
}