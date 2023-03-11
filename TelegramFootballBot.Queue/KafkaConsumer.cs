using Confluent.Kafka;
using System;
using TelegramFootballBot.Queue.Models;

namespace TelegramFootballBot.Queue
{
    public sealed class KafkaConsumer : IConsumer
    {
        private readonly IConsumer<Null, Message> _consumer;

        public KafkaConsumer(ConsumerConfig config, string topic)
        {
            _consumer = new ConsumerBuilder<Null, Message>(config)
                .SetValueDeserializer(new MessageSerializer()).Build();
            _consumer.Subscribe(topic);
        }

        public Message Consume()
        {
            try
            {
                var message = _consumer.Consume();
                return message.Message.Value;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public void Dispose()
        {
            _consumer.Dispose();
        }
    }
}
