
namespace KafkaPubSub.Monitor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using KafkaPubSub.Domain.Applibs;
    using KafkaPubSub.Monitor.Model;

    internal class Program
    {
        const string borkerList = @"192.168.6.24:9092,192.168.6.24:9093,192.168.6.24:9094";

        static void Main(string[] args)
        {
            try
            {
                using (var admin = new KafkaEventAdmin(borkerList, new LogPasser()))
                {
                    bool keepRun = true;

                    var task = Task.Run(() =>
                    {
                        while (keepRun)
                        {
                            var consumerInfoResult = admin.GetConsumerInfo();

                            if (consumerInfoResult.exception != null)
                            {
                                Console.WriteLine($"GetConsumerInfo Exception:{consumerInfoResult.exception.Message}");
                            }

                            Console.Clear();
                            Console.WriteLine(String.Join("\r\n", consumerInfoResult.record.Select(p => $"GroupId:{p.groupId} Partition:{p.topicPartition.Topic}-{p.topicPartition.Partition.Value} Lag:{p.lag}")));

                            SpinWait.SpinUntil(() => false, TimeSpan.FromSeconds(5));
                        }
                    });

                    Console.Read();
                    keepRun = false;
                    task.Wait();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine("Finished");
            Console.Read();
        }
    }
}
