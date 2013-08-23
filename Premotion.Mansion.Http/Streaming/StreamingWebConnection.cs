using System;
using System.IO;
using System.Net.Http;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Premotion.Mansion.Http.Patterns;

namespace Premotion.Mansion.Http.Streaming
{
	/// <summary>
	/// Handles the connection details for connection for a Twitter streaming API.
	/// </summary>
	/// <typeparam name="T">The type of object streamed from this connection.</typeparam>
	public abstract class StreamingWebConnection<T> : DisposableBase where T : class
	{
		private readonly HttpClient client;
		private readonly Subject<T> subject = new Subject<T>();
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
		/// <summary>
		/// Gets an <see cref="IObservable{T}"/> of <typeparamref name="T"/>.
		/// </summary>
		public IObservable<T> Stream
		{
			get { return subject.AsObservable(); }
		}
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

							// if no token was returned assume there is no more data left in the stream
							if (token == null)
								break;

							// emit next tweet to the observers
							subject.OnNext(token);
						}
					}
				}

				// notify the observers we are done
				subject.OnCompleted();
			}
			catch (Exception exception)
			{
				// the object might be disposed if so we eat the error
				if (IsDisposed)
					return;

				// check if this is a recoverable error
				if (TryHandle(exception, () => Connect(request)))
					return;

				// notify all the observers an unhandled error has occurred
				subject.OnError(exception);
			}
		}
		/// <summary>
		/// Fired when a response was received without an error.
		/// </summary>
		protected virtual void Connected()
		{
		}
		/// <summary>
		/// Reads a <typeparamref name="T"/> from the given <paramref name="reader"/>.
		/// </summary>
		/// <param name="reader">The <see cref="TextReader"/> from which to read the token.</param>
		/// <returns>Returns the parsed <typeparamref name="T"/> or null if there are no more <typeparamref name="T"/>s in the stream.</returns>
		protected abstract T Read(TextReader reader);
		/// <summary>
		/// Tries the handle the given <paramref name="exception"/>.
		/// </summary>
		/// <param name="exception">The <see cref="Exception"/> which to try to handle.</param>
		/// <param name="reconnect">A callback invoked when a reconnection attempt can be made.</param>
		/// <returns>Returns true if the error is handled, otherwise false.</returns>
		protected abstract bool TryHandle(Exception exception, Func<Task> reconnect);
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

			// unsubscribe all observers
			subject.Dispose();

			// close the connection, if open
			client.CancelPendingRequests();
			client.Dispose();
		}
	}
}