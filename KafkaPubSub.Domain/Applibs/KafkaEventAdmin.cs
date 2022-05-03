
namespace KafkaPubSub.Domain.Applibs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Confluent.Kafka;
    using Confluent.Kafka.Admin;

    public class KafkaEventAdmin : IDisposable
    {
        private IAdminClient admin;

        private ILogPasser logger;

        public KafkaEventAdmin(string brokerList, ILogPasser logger)
        {
            var config = new AdminClientConfig()
            {
                BootstrapServers = brokerList
            };

            this.admin = new AdminClientBuilder(config).Build();
            this.logger = logger;
        }

        public async Task CreateTopicAsync(IEnumerable<(string topic, int partition, short replica)> topicInfos)
        {
            try
            {
                await this.admin.CreateTopicsAsync(topicInfos.Select(info => new TopicSpecification()
                {
                    Name = info.topic,
                    ReplicationFactor = info.replica,
                    NumPartitions = info.partition,
                    
                }));
            }
            catch (CreateTopicsException ctEx)
            {
                if(ctEx.Error.Code == ErrorCode.Local_Partial)
                {
                    return;
                }

                this.logger.Error(ctEx, $"KafkaEventAdmin CreateTopicAsync Exception");
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, $"KafkaEventAdmin CreateTopicAsync Exception");
            }
        }

        public void Dispose()
        {
            this.admin.Dispose();
        }
    }
}
