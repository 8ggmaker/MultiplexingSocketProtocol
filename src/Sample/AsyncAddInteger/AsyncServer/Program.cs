using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MultiplexingSocket.Protocol;
using MultiplexingSocket.Protocol.Internal;
using MultiplexingSocket.Protocol.Transport;
using Shared.AddTwoInteger;
using System;
using System.Net;
using System.Threading.Tasks;

namespace AsyncServer
{
   class Program
   {
      private static ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.AddConsole());

      private static SocketTransportFactory transportFactory = new SocketTransportFactory(Options.Create(new SocketTransportOptions
      {
         IOQueueCount = 16
      }), loggerFactory);
      static async Task Main(string[] args)
      {
         await ProcessAddInteger();
      }

      static async Task ProcessAddInteger()
      {
         var listener = await transportFactory.BindAsync(new IPEndPoint(IPAddress.Loopback, 5005));
         while (true)
         {
            var connection = await listener.AcceptAsync();
            IMultiplexingSocketProtocol<AddIntegerRequest, int> protocol = new MultiplexingSocketProtocol<AddIntegerRequest, int>(connection, new AddIntegerRequestReader(), new AddIntegerResponseWritter(), new Int32MessageIdGenerator(), new Int32MessageIdParser());
            _ = Task.Run(async () => { await ProcessProtocol(protocol); });
         }
      }

      static async Task ProcessProtocol(IMultiplexingSocketProtocol<AddIntegerRequest, int> protocol)
      {
         while (true)
         {
            var res = await protocol.Read();
            _ = PerformAddInteger(res.Item1, res.Item2.A, res.Item2.B, protocol);
         }
      }

      static async Task PerformAddInteger(MessageId id,int a,int b, IMultiplexingSocketProtocol<AddIntegerRequest, int> protocol)
      {
         var sum = a + b;
         await protocol.Write(sum, id);
      }
   }
}
