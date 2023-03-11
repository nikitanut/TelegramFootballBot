using Confluent.Kafka;
using System.Threading;
using System.Threading.Tasks;
using TelegramFootballBot.Queue.Models;

namespace TelegramFootballBot.Queue
{
    public sealed class KafkaProducer : IProducer
    {
        private readonly IProducer<Null, Message> _producer;
        private readonly string _topic;

        public KafkaProducer(ProducerConfig config, string topic)
        {
            _producer = new ProducerBuilder<Null, Message>(config)
                .SetValueSerializer(new MessageSerializer()).Build();
            _topic = topic;
        }

        public async Task ProduceAsync(Message request, CancellationToken cancellationToken = default)
        {
            var kafkaMessage = new Message<Null, Message> { Value = request };
            await _producer.ProduceAsync(_topic, kafkaMessage, cancellationToken);
        }

        public void Dispose()
        {
            _producer.Dispose();
        }
    }
}
