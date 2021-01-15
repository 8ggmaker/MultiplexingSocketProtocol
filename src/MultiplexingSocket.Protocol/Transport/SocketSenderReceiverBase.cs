// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Net.Sockets;

namespace MultiplexingSocket.Protocol.Transport
{
    internal abstract class SocketSenderReceiverBase : IDisposable
    {
        protected readonly Socket socket;
        protected readonly SocketAwaitableEventArgs awaitableEventArgs;

        protected SocketSenderReceiverBase(Socket socket, PipeScheduler scheduler)
        {
            this.socket = socket;
            awaitableEventArgs = new SocketAwaitableEventArgs(scheduler);
        }

        public void Dispose() => awaitableEventArgs.Dispose();
    }
}
