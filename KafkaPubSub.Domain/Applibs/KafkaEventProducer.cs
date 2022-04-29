
namespace KafkaPubSub.Domain.Applibs
{
    using System;
    using Confluent.Kafka;
    using KafkaPubSub.Domain.Model;
    using Newtonsoft.Json;

    public class KafkaEventProducer : IDisposable
    {
        private Lazy<IProducer<string, string>> producer;

        private ILogPasser logger;

        public KafkaEventProducer(string brokerList, Acks acks, ILogPasser logger)
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

        public void Dispose()
        {
            if (this.producer.IsValueCreated)
            {
                this.producer.Value.Flush();
                this.producer = null;
            }
        }

        public void Publish<T>(string topicName, int suffix, T data)
        {
            var es = new KafkaEventStream(
                typeof(T).Name,
                JsonConvert.SerializeObject(data),
                TimeStampHelper.ToUtcTimeStamp(DateTime.Now));


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
