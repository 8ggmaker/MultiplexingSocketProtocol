using System.Diagnostics.CodeAnalysis;

namespace MultiplexingSocket.Protocol.Internal
{
   internal class Int32MessageId : MessageId
   {
      public int Id { get; private set; }

      public Int32MessageId(int id)
      {
         this.Id = id;
      }

      public override int GetHashCode()
      {
         return this.Id.GetHashCode();
      }

      public override bool Equals(object obj)
      {
         if (obj == null)
         {
            return false;
         }

         if (ReferenceEquals(this, obj))
         {
            return true;
         }

         if(obj is Int32MessageId otherId)
         {
            return this.Id.Equals(otherId.Id);
         }

         return false;
      }
   }
}
