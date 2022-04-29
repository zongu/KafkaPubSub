
namespace KafkaPubSub.Consumer
{
    using System;
    using System.Linq;
    using KafkaPubSub.Consumer.Model;
    using KafkaPubSub.Domain.Applibs;
    using KafkaPubSub.Domain.Model;

    internal class Program
    {
        const string borkerList = @"127.0.0.1:9092";

        static void Main(string[] args)
        {
            try
            {
                var topic = string.Empty;
                var subscribTopics = string.Empty;

                while (string.IsNullOrEmpty(topic))
                {
                    Console.Write($"Topic:");
                    topic = Console.ReadLine();
                }

                while (string.IsNullOrEmpty(subscribTopics))
                {
                    Console.Write($"SubscribTopics:");
                    subscribTopics = Console.ReadLine();
                }

                using (var admin = new KafkaEventAdmin(borkerList, new LogPasser()))
                using (var consumer = new KafkaEventConsumer(1, topic, borkerList, new PubSubDispatcher<KafkaEventStream>(), new LogPasser()))
                {
                    admin.CreateTopicAsync(subscribTopics.Split(',').Select(s => (s, 4, (short)1))).Wait();

                    consumer.Start(subscribTopics.Split(','));

                    Console.Write("Press anykey to stop");
                    Console.Read();

                    consumer.Stop();
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
