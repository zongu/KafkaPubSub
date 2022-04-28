
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

        private string brokerList;

        private Acks acks;

        private ILogger logger;

        public KafkaEventProducer(string brokerList, Acks acks, ILogger logger)
        {
            this.brokerList = brokerList;
            this.acks = acks;
            this.logger = logger;
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

        public void Start()
        {
            producer = new Lazy<IProducer<string, string>>(() =>
            {
                var config = new ProducerConfig()
                {
                    BootstrapServers = this.brokerList,
                    MessageSendMaxRetries = 3,
                    Acks = this.acks
                };

                return new ProducerBuilder<string, string>(config).Build();
            });
        }

        public void Stop()
        {
            if (producer.IsValueCreated)
            {
                producer.Value.Flush();
                producer = null;
            }
        }
    }
}
