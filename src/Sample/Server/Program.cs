using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MultiplexingSocket.Protocol.Internal;
using MultiplexingSocket.Protocol.Transport;
using Shared.AddTwoInteger;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Server
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
            MultiplexingSocketProtocol<AddIntegerRequest, int> protocol = new MultiplexingSocketProtocol<AddIntegerRequest,int>(connection, new AddIntegerRequestReader(), new AddIntegerResponseWritter(), new Int32MessageIdGenerator(), new Int32MessageIdParser());
            _ = Task.Run(async () => { await ProcessProtocol(protocol); });
         }
      }

      static async Task ProcessProtocol(MultiplexingSocketProtocol<AddIntegerRequest,int> protocol)
      {
         while(true)
         {
            var res = await protocol.Read();
            var sum = res.Item2.A + res.Item2.B;
            var id = res.Item1;
            await protocol.Write(sum, id);
         }
      }
   }
}
