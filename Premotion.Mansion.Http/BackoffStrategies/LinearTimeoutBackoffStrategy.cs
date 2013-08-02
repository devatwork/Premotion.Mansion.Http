using System;
using System.Threading;

namespace Premotion.Mansion.Http.BackoffStrategies
{
	/// <summary>
	/// Implements a linear backoff stategy.
	/// </summary>
	public class LinearTimeoutBackoffStrategy : TimeoutBackoffStrategy
	{
		#region Constructors
		/// <summary>
		/// Constructs a linear backoff strategy.
		/// </summary>
		/// <param name="retryDelay">The number of milliseconds to wait before a retry attempt will be made.</param>
		/// <param name="maxRetryDelay">The maximum number of milliseconds to wait before a retry is attempted. If the the timeout is exceeded an exception will be raised.</param>
		public LinearTimeoutBackoffStrategy(int retryDelay, int maxRetryDelay)
		{
			// set values
			this.retryDelay = retryDelay;
			this.maxRetryDelay = maxRetryDelay;
		}
		#endregion
		#region BackoffStrategy Members
		/// <summary>
		/// Sets a <see cref="Timer"/>.
		/// </summary>
		/// <param name="callback">The <see cref="TimerCallback"/>.</param>
		/// <returns>Returns the created <see cref="Timer"/>.</returns>
		protected override Timer CreateTimer(TimerCallback callback)
		{
			// increment the current timeout
			currentTimeout += retryDelay;
			if (currentTimeout >= maxRetryDelay)
				throw new InvalidOperationException("A timeout has occurred, failed the attempt to retry");

			// set the timer
			return new Timer(callback, null, currentTimeout, currentTimeout);
		}
		/// <summary>
		/// Resets the timeout.
		/// </summary>
		public override void Reset()
		{
			//  clean up the timer
			base.Reset();

			// reset the current timeout
			currentTimeout = 0;
		}
		#endregion
		#region Private Fields
		private int currentTimeout;
		private readonly int retryDelay;
		private readonly int maxRetryDelay;
		#endregion
	}
}