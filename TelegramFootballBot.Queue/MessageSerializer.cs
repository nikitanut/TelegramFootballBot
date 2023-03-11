using Confluent.Kafka;
using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using TelegramFootballBot.Queue.Models;

namespace TelegramFootballBot.Queue
{
    public class MessageSerializer : ISerializer<Message>, IDeserializer<Message>
    {
        private const string _typeKey = "Type";

        public byte[] Serialize(Message data, SerializationContext context)
        {
            context.Headers.Add(_typeKey, Encoding.UTF8.GetBytes(data.Type));
            return JsonSerializer.SerializeToUtf8Bytes(data, data.GetType());
        }

        public Message Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
        {
            var typeHeader = context.Headers.FirstOrDefault(h => h.Key == _typeKey);
            if (typeHeader == null)
                throw new ApplicationException("Type header not found");

            var type = Type.GetType($"TelegramFootballBot.Queue.Messages.{Encoding.UTF8.GetString(typeHeader.GetValueBytes())}");
            var message = JsonSerializer.Deserialize(data, type);
            return message as Message;
        }
    }
}
