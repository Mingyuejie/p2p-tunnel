using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace common
{
    public class Logger
    {
        private static readonly Lazy<Logger> lazy = new Lazy<Logger>(() => new Logger());
        public static Logger Instance => lazy.Value;
        private readonly ConcurrentQueue<LoggerModel> queue = new ConcurrentQueue<LoggerModel>();
        public int Count => queue.Count;

        public Action<LoggerModel> OnLogger;
        private Logger()
        {
            OnLogger = (model) =>
            {
                ConsoleColor currentForeColor = Console.ForegroundColor;
                switch (model.Type)
                {
                    case LoggerTypes.DEBUG:
                        Console.ForegroundColor = ConsoleColor.Blue;
                        break;
                    case LoggerTypes.INFO:
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    case LoggerTypes.WARNING:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    case LoggerTypes.ERROR:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                    default:
                        break;
                }
                Console.WriteLine($"[{model.Type}][{model.Time:yyyy-MM-dd HH:mm:ss}]:{model.Content}");
                Console.ForegroundColor = currentForeColor;
            };

            _ = Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (Count > 0)
                    {
                        LoggerModel model = Dequeue();
                        if (model != null)
                        {
                            OnLogger?.Invoke(model);
                        }
                    }
                    else
                    {
                        System.Threading.Thread.Sleep(1);
                    }
                }
            }, TaskCreationOptions.LongRunning);
        }

        [Conditional("DEBUG")]
        public void Debug(string content, params object[] args)
        {
            queue.Enqueue(new LoggerModel { Type = LoggerTypes.INFO, Content = string.Format(content, args) });
        }

        public void Info(string content, params object[] args)
        {
            queue.Enqueue(new LoggerModel { Type = LoggerTypes.INFO, Content = string.Format(content, args) });
        }
        public void Warning(string content, params object[] args)
        {
            queue.Enqueue(new LoggerModel { Type = LoggerTypes.WARNING, Content = string.Format(content, args) });
        }
        public void Error(string content, params object[] args)
        {
            queue.Enqueue(new LoggerModel { Type = LoggerTypes.ERROR, Content = string.Format(content, args) });
        }

        public void Enqueue(LoggerModel model)
        {
            queue.Enqueue(model);
        }
        public LoggerModel Dequeue()
        {
            _ = queue.TryDequeue(out LoggerModel model);
            return model;
        }
    }

    public class LoggerModel
    {
        public LoggerTypes Type { get; set; } = LoggerTypes.INFO;
        public DateTime Time { get; set; } = DateTime.Now;
        public string Content { get; set; } = string.Empty;
    }

    public enum LoggerTypes
    {
       DEBUG, INFO, WARNING, ERROR
    }
}
