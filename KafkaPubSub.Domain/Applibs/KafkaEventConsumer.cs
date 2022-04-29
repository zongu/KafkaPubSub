
namespace KafkaPubSub.Domain.Applibs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Confluent.Kafka;
    using KafkaPubSub.Domain.Model;
    using Newtonsoft.Json;

    public class KafkaEventConsumer
    {
        private ILogPasser logger;

        private IPubSubDispatcher<KafkaEventStream> dispatcher;

        private bool running;

        private List<IConsumer<string, string>> consumers;

        public KafkaEventConsumer(
            int consumerNum,
            string groupId,
            IEnumerable<string> topics,
            string brokerList,
            IPubSubDispatcher<KafkaEventStream> dispatcher,
            ILogPasser logger)
        {
            this.logger = logger;
            this.dispatcher = dispatcher;

            var config = new ConsumerConfig()
            {
                GroupId = groupId,
                EnableAutoCommit = true,
                AutoCommitIntervalMs = 5000,
                StatisticsIntervalMs = 60000,
                BootstrapServers = brokerList,
                AutoOffsetReset = AutoOffsetReset.Latest
            };

            this.consumers = Enumerable.Range(1, consumerNum).Select(index =>
            {
                var consumer = new ConsumerBuilder<string, string>(config).Build();
                consumer.Subscribe(topics);
                return consumer;
            }).ToList();
        }


        public void Start()
        {
            this.running = true;

            this.consumers.ForEach(consumer =>
            {
                Task.Run(() =>
                {
                    while (this.running)
                    {
                        try
                        {
                            var consumeResult = consumer.Consume(TimeSpan.FromMilliseconds(1000));
                            var @event = JsonConvert.DeserializeObject<KafkaEventStream>(consumeResult.Message.Value);
                            this.dispatcher.DispatchMessage(@event);
                        }
                        catch (Exception ex)
                        {
                            this.logger.Error(ex, $"KafkaEventConsumer Consume Exception");
                        }
                    }
                });
            });
        }


        public void Stop()
        {
            this.running = false;
        }
    }
}
