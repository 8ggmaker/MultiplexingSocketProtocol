using System;
using System.Collections.Generic;
using System.Text;

namespace MultiplexingSocket.Protocol
{
   public interface IObjectPool<T>
   {
      T Rent();

      void Return(T instance);
   }
}
