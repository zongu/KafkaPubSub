
namespace KafkaPubSub.Domain.Applibs
{
    using System;
    using Confluent.Kafka;
    using KafkaPubSub.Domain.Model;
    using Newtonsoft.Json;
    using NLog;

    public class KafkaEventProducer
    {
        private Lazy<IProducer<string, string>> producer;

        private ILogger logger;

        public KafkaEventProducer(string brokerList, Acks acks, ILogger logger)
        {
            this.logger = logger;

            producer = new Lazy<IProducer<string, string>>(() =>
            {
                var config = new ProducerConfig()
                {
                    BootstrapServers = brokerList,
                    MessageSendMaxRetries = 3,
                    Acks = acks
                };

                return new ProducerBuilder<string, string>(config).Build();
            });
        }

        public void Publish<T>(string topicName, int suffix, T data)
        {
            var es = new KafkaEventStream(
                typeof(T).Name,
                JsonConvert.SerializeObject(data),
                TimeStampHelper.ToUtcTimeStamp(DateTime.Now));

            if (producer.IsValueCreated)
            {
                producer.Value.ProduceAsync(topicName, new Message<string, string>()
                {
                    Key = $"{es.Type}.{suffix}",
                    Value = JsonConvert.SerializeObject(es)
                }).ContinueWith(deliveryResult =>
                {
                    this.logger.Trace($"KafkaEventProducer Partition: {deliveryResult.Result.Partition}, Offset: {deliveryResult.Result.Offset}");
                });
            }
        }
    }
}
