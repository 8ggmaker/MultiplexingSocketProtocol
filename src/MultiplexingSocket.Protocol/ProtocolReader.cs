using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;

namespace MultiplexingSocket.Protocol
{
   public class ProtocolReader : IAsyncDisposable
   {
      private PipeReader reader;
      private int consumed;
      public ValueTask DisposeAsync()
      {
         throw new NotImplementedException();
      }
   }
}
