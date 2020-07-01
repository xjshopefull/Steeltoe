﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.Rabbit.Data;

namespace Steeltoe.Messaging.Rabbit.Batch
{
    public class MessageBatch
    {
        public string Exchange { get; }

        private string RoutingKey { get; }

        private Message Message { get; }

        public MessageBatch(string exchange, string routingKey, Message message)
        {
            Exchange = exchange;
            RoutingKey = routingKey;
            Message = message;
        }
    }
}