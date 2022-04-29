
namespace KafkaPubSub.Domain.Applibs
{
    using System;

    public interface ILogPasser
    {
        void Trace(string str);

        void Debug(string str);

        void Info(string str);

        void Warn(Exception ex, string str);

        void Error(Exception ex, string str);

        void Fatal(Exception ex, string str);
    }
}
