// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;

namespace MultiplexingSocket.Protocol.Transport
{
    internal sealed class SocketConnectionListener : IConnectionListener
    {
        private readonly MemoryPool<byte> memoryPool;
        private readonly int numSchedulers;
        private readonly PipeScheduler[] schedulers;
        private readonly ISocketsTrace trace;
        private Socket listenSocket;
        private int schedulerIndex;
        private readonly SocketTransportOptions options;

        public EndPoint EndPoint { get; private set; }

        internal SocketConnectionListener(
            EndPoint endpoint,
            SocketTransportOptions options,
            ISocketsTrace trace)
        {
            EndPoint = endpoint;
            this.trace = trace;
            this.options = options;
            memoryPool = MemoryPool<byte>.Shared;
            var ioQueueCount = options.IOQueueCount;

            if (ioQueueCount > 0)
            {
                numSchedulers = ioQueueCount;
                schedulers = new IOQueue[numSchedulers];

                for (var i = 0; i < numSchedulers; i++)
                {
                    schedulers[i] = new IOQueue();
                }
            }
            else
            {
                var directScheduler = new PipeScheduler[] { PipeScheduler.ThreadPool };
                numSchedulers = directScheduler.Length;
                schedulers = directScheduler;
            }
        }

        internal void Bind()
        {
            if (this.listenSocket != null)
            {
                throw new InvalidOperationException("TransportAlreadyBound");
            }

            Socket listenSocket;

            // Unix domain sockets are unspecified
            var protocolType = EndPoint is UnixDomainSocketEndPoint ? ProtocolType.Unspecified : ProtocolType.Tcp;

            listenSocket = new Socket(EndPoint.AddressFamily, SocketType.Stream, protocolType);

            // Kestrel expects IPv6Any to bind to both IPv6 and IPv4
            if (EndPoint is IPEndPoint ip && ip.Address == IPAddress.IPv6Any)
            {
                listenSocket.DualMode = true;
            }

            try
            {
                listenSocket.Bind(EndPoint);
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.AddressAlreadyInUse)
            {
                throw new AddressInUseException(e.Message, e);
            }

            EndPoint = listenSocket.LocalEndPoint;

            listenSocket.Listen(512);

            this.listenSocket = listenSocket;
        }

        public async ValueTask<ConnectionContext> AcceptAsync(CancellationToken cancellationToken = default)
        {
            while (true)
            {
                try
                {
                    var acceptSocket = await listenSocket.AcceptAsync();

                    // Only apply no delay to Tcp based endpoints
                    if (acceptSocket.LocalEndPoint is IPEndPoint)
                    {
                        acceptSocket.NoDelay = options.NoDelay;
                    }

                    var connection = new SocketConnection(acceptSocket, memoryPool, schedulers[schedulerIndex], trace, options.MaxReadBufferSize, options.MaxWriteBufferSize);

                    connection.Start();

                    schedulerIndex = (schedulerIndex + 1) % numSchedulers;

                    return connection;
                }
                catch (ObjectDisposedException)
                {
                    // A call was made to UnbindAsync/DisposeAsync just return null which signals we're done
                    return null;
                }
                catch (SocketException e) when (e.SocketErrorCode == SocketError.OperationAborted)
                {
                    // A call was made to UnbindAsync/DisposeAsync just return null which signals we're done
                    return null;
                }
                catch (SocketException)
                {
                    // The connection got reset while it was in the backlog, so we try again.
                    trace.ConnectionReset(connectionId: "(null)");
                }
            }
        }

        public ValueTask UnbindAsync(CancellationToken cancellationToken = default)
        {
            listenSocket?.Dispose();
            return default;
        }

        public ValueTask DisposeAsync()
        {
            listenSocket?.Dispose();
            // Dispose the memory pool
            memoryPool.Dispose();
            return default;
        }
    }
}
