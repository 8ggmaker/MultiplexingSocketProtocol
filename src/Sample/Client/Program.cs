using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MultiplexingSocket.Protocol.Internal;
using MultiplexingSocket.Protocol.Transport;
using Shared.AddTwoInteger;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Client
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
         MultiplexingSocketProtocol<int, AddIntegerRequest> protocol = new MultiplexingSocketProtocol<int,AddIntegerRequest>(connection, new AddIntegerResponseReader(), new AddIntegerRequestWritter(), new Int32MessageIdGenerator(), new Int32MessageIdParser());
         int cnt = 20;
         for(int i =0;i<cnt;i++)
         {
            var id = await protocol.Write(new AddIntegerRequest 
            { 
               A = i,
               B = i+1
            });
            var resp = await protocol.Read();
            if(resp.Item1 != id)
            {
               Console.WriteLine($"error, id not matched, reqId:{id},respId:{resp.Item1}");
            }
            else
            {
               Console.WriteLine($"{i} + {i + 1} = {resp.Item2}");
            }
         }
      }

   }
}
