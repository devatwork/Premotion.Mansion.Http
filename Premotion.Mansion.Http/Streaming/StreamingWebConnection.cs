using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Premotion.Mansion.Http.Patterns;

namespace Premotion.Mansion.Http.Streaming
{
	/// <summary>
	/// Handles the connection details for connection for a Twitter streaming API.
	/// </summary>
	/// <typeparam name="T">The type of object streamed from this connection.</typeparam>
	public abstract class StreamingWebConnection<T> : DisposableBase, IObservable<T>
	{
		#region Nested type: Unsubscriber
		/// <summary>
		/// Unsubscribes a <see cref="IObserver{T}"/> when disposed.
		/// </summary>
		private class Unsubscriber : DisposableBase
		{
			#region Constructors
			/// <summary>
			/// Constructs the unsubsriber.
			/// </summary>
			/// <param name="observers">Reference to the list holding the <see cref="IObserver{T}"/>s.</param>
			/// <param name="observer">The <see cref="IObserver{T}"/> which to unsubscribe.</param>
			public Unsubscriber(List<IObserver<T>> observers, IObserver<T> observer)
			{
				// validate arguments
				if (observers == null)
					throw new ArgumentNullException("observers");
				if (observer == null)
					throw new ArgumentNullException("observer");

				// set the values
				this.observers = observers;
				this.observer = observer;
			}
			#endregion
			#region IDisposable Members
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

				// remove the observer from the list
				if (observer != null)
					observers.Remove(observer);
			}
			#endregion
			#region Private Fields
			private readonly IObserver<T> observer;
			private readonly List<IObserver<T>> observers;
			#endregion
		}
		#endregion
		#region Constructors
		/// <summary>
		/// Constructs a new <see cref="StreamingWebConnection{T}"/> using the given <paramref name="client"/>.
		/// </summary>
		/// <param name="client">The <see cref="HttpClient"/> to user.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="client"/> is null.</exception>
		protected StreamingWebConnection(HttpClient client)
		{
			// validate arguments
			if (client == null)
				throw new ArgumentNullException("client");

			// set the value
			this.client = client;

			// set the timespan to infinit since we will be streaming
			client.Timeout = Timeout.InfiniteTimeSpan;
		}
		#endregion
		#region Connection Methods
		/// <summary>
		/// Connects to the stream.
		/// </summary>
		protected async Task Connect(HttpRequestMessage request)
		{
			// validate arguments
			if (request == null)
				throw new ArgumentNullException("request");
			CheckDisposed();

			// try to send the request
			try
			{
				using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
				{
					// ensure the request was successful
					response.EnsureSuccessStatusCode();

					// get the response stream
					using (var responseStream = await response.Content.ReadAsStreamAsync())
					using (var textReader = new StreamReader(responseStream))
					{
						// successful connection made
						Connected();

						// read chuncked responses seperated by a newline
						while (true)
						{
							// create the tweet
							var token = Read(textReader);

							// emit next tweet to the observers
							foreach (var observer in observers)
								observer.OnNext(token);
						}
					}
				}
			}
			catch (Exception exception)
			{
				// the object might be disposed if so we eat the error
				if (IsDisposed)
					return;

				// check if this is a recoverable error
				if (TryHandle(exception, () => {
					// check if the object is not disposed yet
					if (IsDisposed)
					{
						return Task.Run(() => {
							// noop
						});
					}

					// try to connect
					return Connect(request);
				}))
					return;

				// notify all the observers an unhandled error has occurred
				foreach (var observer in observers)
					observer.OnError(exception);
			}
		}
		/// <summary>
		/// Fired when a response was received without an error.
		/// </summary>
		protected virtual void Connected()
		{
		}
		#endregion
		#region Stream Methods
		/// <summary>
		/// Reads a <typeparamref name="T"/> from the given <paramref name="reader"/>.
		/// </summary>
		/// <param name="reader">The <see cref="TextReader"/> from which to read the token.</param>
		/// <returns>Returns the parsed <typeparamref name="T"/>.</returns>
		protected abstract T Read(TextReader reader);
		/// <summary>
		/// Tries the handle the given <paramref name="exception"/>.
		/// </summary>
		/// <param name="exception">The <see cref="Exception"/> which to try to handle.</param>
		/// <param name="reconnect">A callback invoked when a reconnection attempt can be made.</param>
		/// <returns>Returns true if the error is handled, otherwise false.</returns>
		protected abstract bool TryHandle(Exception exception, Func<Task> reconnect);
		#endregion
		#region IObservable<T> Members
		/// <summary>
		/// Notifies the provider that an observer is to receive notifications.
		/// </summary>
		/// <returns>
		/// A reference to an interface that allows observers to stop receiving notifications before the provider has finished sending them.
		/// </returns>
		/// <param name="observer">The object that is to receive notifications.</param>
		public IDisposable Subscribe(IObserver<T> observer)
		{
			// validate arguments
			if (observer == null)
				throw new ArgumentNullException("observer");

			// add the subscriber
			observers.Add(observer);

			// create a Unsubscriber
			return new Unsubscriber(observers, observer);
		}
		#endregion
		#region DisposableBase Members
		/// <summary>
		/// Dispose resources. Override this method in derived classes. Unmanaged resources should always be released
		/// when this method is called. Managed resources may only be disposed of if disposeManagedResources is true.
		/// </summary>
		/// <param name="disposeManagedResources">A value which indicates whether managed resources may be disposed of.</param>
		protected override void DisposeResources(bool disposeManagedResources)
		{
			if (!disposeManagedResources)
				return;

			// close the connection, if open
			client.CancelPendingRequests();
			client.Dispose();

			// unsubscribe all observers
			foreach (var observer in observers)
				observer.OnCompleted();
			observers.Clear();
		}
		#endregion
		#region Private Fields
		private readonly HttpClient client;
		private readonly List<IObserver<T>> observers = new List<IObserver<T>>();
		#endregion
	}
}