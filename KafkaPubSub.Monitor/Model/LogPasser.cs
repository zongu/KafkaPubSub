
namespace KafkaPubSub.Monitor.Model
{
    using System;
    using KafkaPubSub.Domain.Applibs;

    public class LogPasser : ILogPasser
    {
        public void Debug(string str)
        {
            Console.WriteLine($"Debug {str}");
        }

        public void Error(Exception ex, string str)
        {
            Console.WriteLine($"Error {str}, {ex.ToString()}");
        }

        public void Fatal(Exception ex, string str)
        {
            Console.WriteLine($"Fatal {str}, {ex.ToString()}");
        }

        public void Info(string str)
        {
            Console.WriteLine($"Info {str}");
        }

        public void Trace(string str)
        {
            Console.WriteLine($"Trace {str}");
        }

        public void Warn(Exception ex, string str)
        {
            Console.WriteLine($"Warn {str}, {ex.ToString()}");
        }
    }
}
