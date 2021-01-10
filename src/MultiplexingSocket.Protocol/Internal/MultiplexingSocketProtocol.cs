﻿using Microsoft.AspNetCore.Connections;
using MultiplexingSocket.Protocol.Internal;
using MultiplexingSocket.Protocol.Messages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MultiplexingSocket.Protocol.Internal
{
   internal class MultiplexingSocketProtocol<TInbound,TOutbound>:IOQueue,IMultiplexingSocketProtocol<TInbound,TOutbound>
   {
      private readonly ConnectionContext connection;
      private readonly ProtocolReader reader;
      private readonly ProtocolWriter writer;
      private readonly IMessageIdGenerator messageIdGenerator;
      private readonly IMessageReader<TInbound> messageReader;
      private readonly IMessageWriter<TOutbound> messageWriter;
      private readonly IMessageIdParser messageIdParser;
      public MultiplexingSocketProtocol(ConnectionContext connection,IMessageReader<TInbound> messageReader,IMessageWriter<TOutbound> messageWriter,IMessageIdGenerator messageIdGenerator,IMessageIdParser messageIdParser)
      {
         this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
         this.messageReader = messageReader ?? throw new ArgumentNullException(nameof(messageReader));
         this.messageWriter = messageWriter ?? throw new ArgumentNullException(nameof(messageWriter));
         this.messageIdGenerator = messageIdGenerator ?? throw new ArgumentNullException(nameof(messageIdGenerator));
         this.reader = new ProtocolReader(this.connection.Transport.Input);
         this.writer = new ProtocolWriter(this.connection.Transport.Output);
         this.messageIdParser = messageIdParser ?? throw new ArgumentNullException(nameof(messageIdParser));
      }

      public async ValueTask<I4ByteMessageId> Write(TOutbound message)
      {
         I4ByteMessageId id;
         var idTask = this.messageIdGenerator.Next();
         if(idTask.IsCompleted)
         {
            id = idTask.Result;
         }
         else
         {
            id = await idTask;
         }
      }

      public ValueTask<TInbound> Read()
      {
         throw new NotImplementedException();
      }

      private void WriteHeader()
      {

      }

      private I4ByteMessageId ReadHeader()
      {
         return null;
      }
   }
}