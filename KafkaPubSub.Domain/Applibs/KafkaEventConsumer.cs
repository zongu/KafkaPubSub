
namespace KafkaPubSub.Domain.Applibs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Confluent.Kafka;
    using KafkaPubSub.Domain.Model;
    using Newtonsoft.Json;

    public class KafkaEventConsumer : IDisposable
    {
        private ILogPasser logger;

        private IPubSubDispatcher<KafkaEventStream> dispatcher;

        private bool running;

        private List<IConsumer<string, string>> consumers;

        public KafkaEventConsumer(
            int consumerNum,
            string groupId,
            string brokerList,
            IPubSubDispatcher<KafkaEventStream> dispatcher,
            ILogPasser logger)
        {
            this.logger = logger;
            this.dispatcher = dispatcher;

            var config = new ConsumerConfig()
            {
                GroupId = groupId,
                EnableAutoCommit = false,
                // 跟kafka校對狀態頻率
                StatisticsIntervalMs = 5000,
                BootstrapServers = brokerList,
                AutoOffsetReset = AutoOffsetReset.Latest,
                SessionTimeoutMs = 6000
            };

            this.consumers = Enumerable.Range(1, consumerNum).Select(index =>
            {
                var consumer = new ConsumerBuilder<string, string>(config)
                    .SetErrorHandler((_, error) => this.logger.Error(new Exception(error.Reason), $"KafkaEventConsumer ErrorHandler"))
                    .Build();
                return consumer;
            }).ToList();
        }

        public void Dispose()
        {
            Stop();
        }

        public void Start(IEnumerable<string> topics)
        {
            this.running = true;

            this.consumers.ForEach(consumer =>
            {
                // 需要有topic存在後再綁定，不然第一次consume會噴錯
                consumer.Subscribe(topics);

                Task.Run(() =>
                {
                    while (this.running)
                    {
                        try
                        {
                            var consumeResult = consumer.Consume(TimeSpan.FromMilliseconds(1000));
                            
                            if (consumeResult != null)
                            {
                                this.logger.Trace($"KafkaEventConsumer Consume Key:{consumeResult.Message.Key} Offset:{consumeResult.Offset.Value}, Partition:{consumeResult.Partition.Value}");

                                var @event = JsonConvert.DeserializeObject<KafkaEventStream>(consumeResult.Message.Value);
                                if (this.dispatcher.DispatchMessage(@event))
                                {
                                    consumer.Commit(consumeResult);
                                }
                            }
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

            Parallel.ForEach(this.consumers, consumer => consumer.Dispose());
        }
    }
}
