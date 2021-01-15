// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Net.Sockets;

namespace MultiplexingSocket.Protocol.Transport
{
    internal sealed class SocketReceiver : SocketSenderReceiverBase
    {
        public SocketReceiver(Socket socket, PipeScheduler scheduler) : base(socket, scheduler)
        {
        }

        public SocketAwaitableEventArgs WaitForDataAsync()
        {
            awaitableEventArgs.SetBuffer(Memory<byte>.Empty);

            if (!socket.ReceiveAsync(awaitableEventArgs))
            {
                awaitableEventArgs.Complete();
            }

            return awaitableEventArgs;
        }

        public SocketAwaitableEventArgs ReceiveAsync(Memory<byte> buffer)
        {
            awaitableEventArgs.SetBuffer(buffer);

            if (!socket.ReceiveAsync(awaitableEventArgs))
            {
                awaitableEventArgs.Complete();
            }

            return awaitableEventArgs;
        }
    }
}
