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
         if(other==null)
         {
            return false;
         }
         if(other is Int32MessageId otherId)
         {
            return Id == otherId.Id;
         }
         return false;
      }

      public string ToString(string format, IFormatProvider formatProvider)
      {
         return Id.ToString();
      }
   }
}
