
namespace KafkaPubSub.Domain.Applibs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Confluent.Kafka;
    using Confluent.Kafka.Admin;

    public class KafkaEventAdmin : IDisposable
    {
        private IAdminClient admin;

        private ILogPasser logger;

        private string brokerList;

        public KafkaEventAdmin(string brokerList, ILogPasser logger)
        {
            var config = new AdminClientConfig()
            {
                BootstrapServers = brokerList
            };

            this.admin = new AdminClientBuilder(config).Build();
            this.logger = logger;
            this.brokerList = brokerList;
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
                if (ctEx.Error.Code == ErrorCode.Local_Partial)
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

        public (Exception exception, IEnumerable<(TopicPartition topicPartition, long lag)> record) GetConsumerInfo()
        {
            try
            {
                var topicMetadata = this.admin.GetMetadata(TimeSpan.FromSeconds(3))
                .Topics.SelectMany(t => t.Partitions.Select(p => new TopicPartition(t.Topic, p.PartitionId)));
                var consumerGroup = this.admin.ListGroups(TimeSpan.FromSeconds(3)).Where(p => p.Protocol == string.Empty);

                return (null, consumerGroup.SelectMany(consumerInfo =>
                {
                    var config = new ConsumerConfig()
                    {
                        GroupId = consumerInfo.Group,
                        BootstrapServers = this.brokerList,
                        EnableAutoCommit = false,
                        AllowAutoCreateTopics = false,
                        EnableAutoOffsetStore = false,
                        AutoOffsetReset = AutoOffsetReset.Error
                    };

                    using (var metadataConsumer = new ConsumerBuilder<string, string>(config).Build())
                    {
                        var committedOffsets = metadataConsumer.Committed(topicMetadata, TimeSpan.FromSeconds(3));

                        var topicsWithFoundOffsets = committedOffsets.GroupBy(t => t.Topic).Where(t => t.Any(s => !s.Offset.IsSpecial)).SelectMany(t => t);

                        var result = topicsWithFoundOffsets.Select(tpo => {
                            if (tpo.Offset.IsSpecial)
                            {
                                return (tpo.TopicPartition, tpo.Offset.Value);
                            }

                            var watermark = metadataConsumer.QueryWatermarkOffsets(tpo.TopicPartition, TimeSpan.FromSeconds(3));

                            return (tpo.TopicPartition, watermark.High - tpo.Offset);
                        }).ToList();

                        metadataConsumer.Close();
                        metadataConsumer.Dispose();

                        return result;
                    }
                }).ToList());
            }
            catch (Exception ex)
            {
                return (ex, null);
            }
        }

        public void Dispose()
        {
            this.admin.Dispose();
        }

        private string DecodeMetaData(byte[] byteArray)
        {
            Func<int, int> swapEndianness = (int value) =>
            {
                var b1 = (value >> 0) & 0xff;
                var b2 = (value >> 8) & 0xff;
                var b3 = (value >> 16) & 0xff;
                var b4 = (value >> 24) & 0xff;
                return b1 << 24 | b2 << 16 | b3 << 8 | b4 << 0;
            };

            Func<short, short> swapShortEndianness = (short i) =>
            {
                return (short)((i << 8) + (i >> 8));
            };

            UTF8Encoding enc = new UTF8Encoding();
            StringBuilder s = new StringBuilder();
            try
            {
                short version = swapShortEndianness(BitConverter.ToInt16(byteArray, 0));
                int num_topic_assignments = swapEndianness(BitConverter.ToInt32(byteArray, 2));
                int i = 6;
                for (int t = 0; t < num_topic_assignments; t++)
                {
                    short topic_len = swapShortEndianness(BitConverter.ToInt16(byteArray, i));
                    byte[] str = new byte[topic_len];
                    Array.Copy(byteArray, i + 2, str, 0, topic_len);
                    string topic = enc.GetString(str);
                    i += (topic_len + 2);
                    int num_partition = swapEndianness(BitConverter.ToInt32(byteArray, i));
                    if (s.Length > 0) s.Append($"; ");
                    s.Append($"{topic}: ");
                    for (int j = 0; j < num_partition; j++)
                    {
                        i += 4;
                        s.Append($"{swapEndianness(BitConverter.ToInt32(byteArray, i))}{(j < num_partition - 1 ? "," : "")}");
                    }
                }
                return s.ToString();
            }
            catch
            {
                return "";
            }
        }
    }
}
