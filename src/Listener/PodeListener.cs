using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Pode
{
    public class PodeListener : IDisposable
    {
        public IDictionary<string, PodeWebSocket> WebSockets { get; private set; }
        public bool IsListening { get; private set; }
        public bool ErrorLoggingEnabled { get; set; }

        private IList<PodeSocket> Sockets;
        private BlockingCollection<PodeContext> Contexts;
        private BlockingCollection<PodeSignal> Signals;

        public PodeListener()
        {
            Sockets = new List<PodeSocket>();
            WebSockets = new Dictionary<string, PodeWebSocket>();
            Contexts = new BlockingCollection<PodeContext>();
            Signals = new BlockingCollection<PodeSignal>();
        }

        public void Add(PodeSocket socket)
        {
            socket.BindListener(this);
            Sockets.Add(socket);
        }

        public PodeContext GetContext(CancellationToken cancellationToken)
        {
            return Contexts.Take(cancellationToken);
        }

        public Task<PodeContext> GetContextAsync(CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(() => GetContext(cancellationToken), cancellationToken);
        }

        public void AddContext(PodeRequest request, PodeResponse response)
        {
            lock (Contexts)
            {
                Contexts.Add(new PodeContext(request, response, this));
            }
        }

        public PodeSignal GetSignal(CancellationToken cancellationToken)
        {
            return Signals.Take(cancellationToken);
        }

        public Task<PodeSignal> GetSignalAsync(CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(() => GetSignal(cancellationToken), cancellationToken);
        }

        public void AddSignal(string value, string path, string clientId)
        {
            lock (Signals)
            {
                Signals.Add(new PodeSignal(value, path, clientId));
            }
        }

        public void Start()
        {
            foreach (var socket in Sockets)
            {
                socket.Listen();
                socket.Start();
            }

            IsListening = true;
        }

        public void Dispose()
        {
            // stop listening
            IsListening = false;

            // shutdown the sockets
            for (var i = Sockets.Count - 1; i >= 0; i--)
            {
                Sockets[i].Dispose();
            }

            Sockets.Clear();

            // close existing contexts
            foreach (var _context in Contexts.ToArray())
            {
                _context.Dispose();
            }

            // close connected web sockets
            foreach (var _socket in WebSockets.Values)
            {
                _socket.Context.Dispose();
            }
        }
    }
}