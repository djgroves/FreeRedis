﻿using console_netcore31_newsocket.IO;
using Microsoft.AspNetCore.Connections;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace console_netcore31_newsocket
{
    public class SocketConnectionFactory : IConnectionFactory, IAsyncDisposable
    {
        private readonly SocketTransportOptions _options;
        private readonly MemoryPool<byte> _memoryPool;

        public SocketConnectionFactory(SocketTransportOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }


            _options = options;
            _memoryPool = options.MemoryPoolFactory();

        }



        public async ValueTask<ConnectionContext> ConnectAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
        {
            var ipEndPoint = endpoint as IPEndPoint;

            if (ipEndPoint is null)
            {
                throw new NotSupportedException("The SocketConnectionFactory only supports IPEndPoints for now.");
            }



            var addressFamily = endpoint.AddressFamily;
            if (addressFamily == AddressFamily.Unspecified && endpoint is DnsEndPoint)
            {   // default DNS to ipv4 if not specified explicitly
                addressFamily = AddressFamily.InterNetwork;
            }

            var protocolType = addressFamily == AddressFamily.Unix ? ProtocolType.Unspecified : ProtocolType.Tcp;
            var socket = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, protocolType)
            {
                NoDelay = _options.NoDelay,
            };
            // Apply the option: keep alive
            //if (OptionKeepAlive)
            //socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            //socket.UseOnlyOverlappedIO = true;
            // Apply the option: no delay
            //if (OptionNoDelay)
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
            await socket.ConnectAsync(ipEndPoint);

            var socketConnection = new SocketConnection(
                socket,
                _memoryPool,
                 new IOQueue(),
                _options.MaxReadBufferSize,
                _options.MaxWriteBufferSize,
                _options.WaitForDataBeforeAllocatingBuffer,
                _options.UnsafePreferInlineScheduling);

            socketConnection.Start();
            return socketConnection;
        }

        public ValueTask DisposeAsync()
        {
            _memoryPool.Dispose();
            return default;
        }
    }
}
