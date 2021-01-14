using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MultiplexingSocket.Protocol.Transport
{
   internal class SocketConnectionFactory : IConnectionFactory, IAsyncDisposable
   {
      private readonly MemoryPool<byte> memoryPool;
      private readonly SocketsTrace trace;
      
      public SocketConnectionFactory(ILoggerFactory loggerFactory)
      {
         if (loggerFactory == null)
         {
            throw new ArgumentNullException(nameof(loggerFactory));
         }

         this.memoryPool = MemoryPool<byte>.Shared;
         var logger = loggerFactory.CreateLogger("SocketConnectionFactory");
         this.trace = new SocketsTrace(logger);
      }

      public async ValueTask<ConnectionContext> ConnectAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
      {
         var ipEndPoint = endpoint as IPEndPoint;

         if (ipEndPoint is null)
         {
            throw new NotSupportedException("The SocketConnectionFactory only supports IPEndPoints for now.");
         }

         var socket = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
         {
            NoDelay = true
         };

         await socket.ConnectAsync(ipEndPoint);

         var socketConnection = new SocketConnection(
             socket,
             memoryPool,
             PipeScheduler.ThreadPool,
             trace);

         socketConnection.Start();
         return socketConnection;
      }

      public ValueTask DisposeAsync()
      {
         this.memoryPool.Dispose();
         return default;
      }
   }
}
