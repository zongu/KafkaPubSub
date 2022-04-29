
namespace KafkaPubSub.Producer
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Confluent.Kafka;
    using KafkaPubSub.Domain.Applibs;
    using KafkaPubSub.Domain.Event;
    using KafkaPubSub.Producer.Model;

    internal class Program
    {
        const string borkerList = @"127.0.0.1:9092";

        static void Main(string[] args)
        {
            try
            {
                var running = true;
                var topic = string.Empty;

                while (string.IsNullOrEmpty(topic))
                {
                    Console.Write($"Topic:");
                    topic = Console.ReadLine();
                }

                using (var producer = new KafkaEventProducer(borkerList, Acks.All, new LogPasser()))
                {
                    Task.Run(() =>
                    {
                        while (running)
                        {
                            producer.Publish(topic, 1, new TimeStampEvent()
                            {
                                UtcDateTimeStamp = TimeStampHelper.UtcNow
                            });

                            SpinWait.SpinUntil(() => false, 1000);
                        }
                    });

                    Console.Write("Press anykey to stop");
                    Console.Read();
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
