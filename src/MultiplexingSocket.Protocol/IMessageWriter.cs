
using System.Buffers;

namespace MultiplexingSocket.Protocol
{
   public interface IMessageWriter<T>
   {
      void WriteMessage(T message, IBufferWriter<byte> output);
   }
}
