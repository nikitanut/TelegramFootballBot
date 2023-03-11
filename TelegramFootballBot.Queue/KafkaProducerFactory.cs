using Confluent.Kafka;

namespace TelegramFootballBot.Queue
{
    public class KafkaProducerFactory : IProducerFactory
    {
        private readonly ProducerConfig _config;
        private readonly string _topic;

        public KafkaProducerFactory(ProducerConfig config, string topic)
        {
            _config = config;
            _topic = topic;
        }

        public IProducer Create()
        {
            return new KafkaProducer(_config, _topic);
        }
    }
}
