﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Messaging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using Channels = System.Threading.Channels;

namespace Steeltoe.Integration.Channel
{
    public class QueueChannel : AbstractPollableChannel, IQueueChannelOperations
    {
        private readonly Channels.Channel<IMessage> _channel;
        private readonly int _capacity = -1;
        private int _size;

        public QueueChannel(IApplicationContext context, ILogger logger = null)
            : this(context, Channels.Channel.CreateBounded<IMessage>(new Channels.BoundedChannelOptions(int.MaxValue) { FullMode = BoundedChannelFullMode.Wait }), null, logger)
        {
            _capacity = int.MaxValue;
        }

        public QueueChannel(IApplicationContext context, string name, ILogger logger = null)
            : this(context, Channels.Channel.CreateBounded<IMessage>(new Channels.BoundedChannelOptions(int.MaxValue) { FullMode = BoundedChannelFullMode.Wait }), name, logger)
        {
            _capacity = int.MaxValue;
        }

        public QueueChannel(IApplicationContext context, int capacity, ILogger logger = null)
            : this(context, Channels.Channel.CreateBounded<IMessage>(new Channels.BoundedChannelOptions(capacity) { FullMode = BoundedChannelFullMode.Wait }), null, logger)
        {
            _capacity = capacity;
        }

        public QueueChannel(IApplicationContext context, Channels.Channel<IMessage> channel, ILogger logger = null)
            : this(context, channel, null, logger)
        {
        }

        public QueueChannel(IApplicationContext context, Channels.Channel<IMessage> channel, string name, ILogger logger = null)
            : base(context, name, logger)
        {
            if (channel == null)
            {
                throw new ArgumentNullException(nameof(channel));
            }

            _channel = channel;
            Writer = new QueueChannelWriter(this, logger);
            Reader = new QueueChannelReader(this, logger);
        }

        public int QueueSize => _size;

        public int RemainingCapacity
        {
            get { return _capacity - _size; }
        }

        public IList<IMessage> Clear()
        {
            var messages = new List<IMessage>();
            while (_channel.Reader.TryRead(out var message))
            {
                Interlocked.Decrement(ref _size);
                messages.Add(message);
            }

            return messages;
        }

        public IList<IMessage> Purge(IMessageSelector messageSelector)
        {
            throw new NotSupportedException();
        }

        protected override IMessage DoReceiveInternal(CancellationToken cancellationToken)
        {
            if (cancellationToken == default)
            {
                if (_channel.Reader.TryRead(out var message))
                {
                    Interlocked.Decrement(ref _size);
                }

                return message;
            }
            else
            {
                try
                {
                    var message = _channel
                        .Reader
                        .ReadAsync(cancellationToken)
                        .AsTask()
                        .ConfigureAwait(false)
                        .GetAwaiter()
                        .GetResult();

                    if (message != null)
                    {
                        Interlocked.Decrement(ref _size);
                    }

                    return message;
                }
                catch (OperationCanceledException)
                {
                    return null;
                }
            }
        }

        protected override bool DoSendInternal(IMessage message, CancellationToken cancellationToken)
        {
            if (cancellationToken == default)
            {
                if (_channel.Writer.TryWrite(message))
                {
                    Interlocked.Increment(ref _size);
                    return true;
                }

                return false;
            }
            else
            {
                try
                {
                    _channel.Writer.WriteAsync(message, cancellationToken).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
                    Interlocked.Increment(ref _size);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }
    }
}
