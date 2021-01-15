// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace MultiplexingSocket.Protocol.Transport
{
    internal class SocketAwaitableEventArgs : SocketAsyncEventArgs, ICriticalNotifyCompletion
    {
        private static readonly Action callbackCompleted = () => { };

        private readonly PipeScheduler ioScheduler;

        private Action callback;

        public SocketAwaitableEventArgs(PipeScheduler ioScheduler)
        {
            this.ioScheduler = ioScheduler;
        }

        public SocketAwaitableEventArgs GetAwaiter() => this;
        public bool IsCompleted => ReferenceEquals(this.callback, callbackCompleted);

        public int GetResult()
        {
            Debug.Assert(ReferenceEquals(this.callback, callbackCompleted));

            this.callback = null;

            if (SocketError != SocketError.Success)
            {
                ThrowSocketException(SocketError);
            }

            return BytesTransferred;

            static void ThrowSocketException(SocketError e)
            {
                throw new SocketException((int)e);
            }
        }

        public void OnCompleted(Action continuation)
        {
            if (ReferenceEquals(this.callback, callbackCompleted) ||
                ReferenceEquals(Interlocked.CompareExchange(ref this.callback, continuation, null), callbackCompleted))
            {
                Task.Run(continuation);
            }
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            OnCompleted(continuation);
        }

        public void Complete()
        {
            OnCompleted(this);
        }

        protected override void OnCompleted(SocketAsyncEventArgs _)
        {
            var continuation = Interlocked.Exchange(ref this.callback, callbackCompleted);

            if (continuation != null)
            {
                this.ioScheduler.Schedule(state => ((Action)state)(), continuation);
            }
        }
    }
}
