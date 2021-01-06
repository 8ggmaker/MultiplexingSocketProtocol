using Microsoft.AspNetCore.Connections;
using MultiplexingSocket.Protocol.Messages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MultiplexingSocket.Protocol
{
   public class MultiplexingSocketProtocol
   {
      private readonly ConnectionContext connection;
      private readonly ProtocolReader reader;
      private readonly ProtocolWriter writer;
      private readonly IMessageIdGenerator messageIdGenerator;
      private readonly IMessageReader messageReader;
      private readonly IMessageWriter messageWriter;

      private readonly ConcurrentDictionary<I4ByteMessageId,>
      public MultiplexingSocketProtocol(ConnectionContext connection)
      {
         this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
         this.reader = new ProtocolReader(this.connection.Transport.Input);
         this.writer = new ProtocolWriter(this.connection.Transport.Output);
      }


      private struct ProtocolState
      {
         public Task<byte[]> PendingTask { get; set; }
      }
   }
}
