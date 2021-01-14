using Microsoft.Extensions.Logging;
using MultiplexingSocket.Protocol.Client;
using MultiplexingSocket.Protocol.Internal;
using MultiplexingSocket.Protocol.Transport;
using Shared.AddTwoInteger;
using System;
using System.Net;
using System.Threading.Tasks;

namespace AsyncClient
{
   class Program
   {
      private static ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.AddConsole());
      private static SocketConnectionFactory connectionFactory = new SocketConnectionFactory(loggerFactory);
      static async Task Main(string[] args)
      {
         await ProcessAddInteger();
         Console.ReadKey();
      }

      static async Task ProcessAddInteger()
      {
         await using var connection = await connectionFactory.ConnectAsync(new IPEndPoint(IPAddress.Loopback, 5005));
         MultiplexingSocketProtocol<int, AddIntegerRequest> protocol = new MultiplexingSocketProtocol<int, AddIntegerRequest>(connection, new AddIntegerResponseReader(), new AddIntegerRequestWritter(), new Int32MessageIdGenerator(), new Int32MessageIdParser());
         IMessageIdGenerator idGenerator = new Int32MessageIdGenerator();
         IMultiplexingSocketClientProtocol<int, AddIntegerRequest> clientProtocol = new MultiplexingSocketClientProtocol<int, AddIntegerRequest>(protocol, idGenerator);
         int cnt = 10;
         Task[] tasks = new Task[cnt];
         for (int i = 0; i < cnt; i++)
         {
            tasks[i] = PeformAddInteger(i, i + 1, clientProtocol);
         }
         await Task.WhenAll(tasks);
      }

      static async Task PeformAddInteger(int a,int b,IMultiplexingSocketClientProtocol<int,AddIntegerRequest> clientProtocol)
      {
         var res = await clientProtocol.SendAsync(new AddIntegerRequest
         {
            A = a,
            B = b
         });
         Console.WriteLine($"{a} + {b} = {res}");
      }

   }
}
