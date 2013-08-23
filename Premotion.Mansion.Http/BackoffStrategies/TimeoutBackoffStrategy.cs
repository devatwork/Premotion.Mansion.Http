using System;
using System.Threading;
using System.Threading.Tasks;

namespace Premotion.Mansion.Http.BackoffStrategies
{
	/// <summary>
	/// Base class for all timeout backoff strategies.
	/// </summary>
	public abstract class TimeoutBackoffStrategy : BackoffStrategy
	{
		private Timer timer;
		/// <summary>
		/// Retries to execute the operation.
		/// </summary>
		/// <param name="operation">The operation which to execute on a retry attempt.</param>
		protected override void DoRetry(Func<Task> operation)
		{
			// validate arguments
			if (operation == null)
				throw new ArgumentNullException("operation");
			CheckDisposed();

			// if there was a timer clean it up
			if (timer != null)
				timer.Dispose();

			// create the callback
			TimerCallback callback = state => {
				// disable the timer
				timer.Dispose();
				timer = null;

				// execute the retry operation
				operation();
			};

			// set the timer
			timer = CreateTimer(callback);
		}
		/// <summary>
		/// Sets a <see cref="Timer"/>.
		/// </summary>
		/// <param name="callback">The <see cref="TimerCallback"/>.</param>
		/// <returns>Returns the created <see cref="Timer"/>.</returns>
		protected abstract Timer CreateTimer(TimerCallback callback);
		/// <summary>
		/// Resets the timeout.
		/// </summary>
		public override void Reset()
		{
			CheckDisposed();

			//  clean up the timer
			if (timer != null)
				timer.Dispose();
		}
		/// <summary>
		/// Dispose resources. Override this method in derived classes. Unmanaged resources should always be released
		/// when this method is called. Managed resources may only be disposed of if disposeManagedResources is true.
		/// </summary>
		/// <param name="disposeManagedResources">A value which indicates whether managed resources may be disposed of.</param>
		protected override void DisposeResources(bool disposeManagedResources)
		{
			// check for unmanaged disposal
			if (!disposeManagedResources)
				return;

			// check if there is a timer to dispose
			if (timer != null)
				timer.Dispose();
		}
	}
}