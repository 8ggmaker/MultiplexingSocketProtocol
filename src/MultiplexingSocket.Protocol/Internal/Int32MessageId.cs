using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace MultiplexingSocket.Protocol.Internal
{
   internal class Int32MessageId : I4ByteMessageId
   {
      public int Id { get; private set; }

      public Int32MessageId(int id)
      {
         this.Id = id;
      }

      public bool Equals([AllowNull] I4ByteMessageId other)
      {
         return this.Id.Equals(other);
      }
   }
}
