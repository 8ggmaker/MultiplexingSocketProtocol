using System;
using System.Collections.Generic;
using System.Text;

namespace MultiplexingSocket.Protocol
{
   public interface IObjectPool<T> where T:class
   {
      T Rent();

      void Return(T instance);
   }
}
