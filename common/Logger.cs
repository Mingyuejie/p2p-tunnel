﻿using System;
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

        public SimpleSubPushHandler<LoggerModel> OnLogger { get; } = new SimpleSubPushHandler<LoggerModel>();
        private Logger()
        {
            OnLogger.Sub((model) =>
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
            });

            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    if (Count > 0)
                    {
                        if (Dequeue(out LoggerModel model))
                        {
                            OnLogger.Push(model);
                        }
                    }
                    await Task.Delay(1);
                }
            }, TaskCreationOptions.LongRunning);
        }

        [Conditional("DEBUG")]
        public void Debug(string content, params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                content = string.Format(content, args);
            }
            Enqueue(new LoggerModel { Type = LoggerTypes.DEBUG, Content = content });
        }
        [Conditional("DEBUG")]
        public void Debug(Exception ex)
        {
            Enqueue(new LoggerModel { Type = LoggerTypes.DEBUG, Content = ex + "" });
        }

        public void Info(string content, params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                content = string.Format(content, args);
            }
            Enqueue(new LoggerModel { Type = LoggerTypes.INFO, Content = content });
        }

        public void Warning(string content, params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                content = string.Format(content, args);
            }
            Enqueue(new LoggerModel { Type = LoggerTypes.WARNING, Content = content });
        }
        public void Warning(Exception ex)
        {
            Enqueue(new LoggerModel { Type = LoggerTypes.WARNING, Content = ex + "" });
        }

        public void Error(string content, params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                content = string.Format(content, args);
            }
            Enqueue(new LoggerModel { Type = LoggerTypes.ERROR, Content = content });
        }
        public void Error(Exception ex)
        {
            Enqueue(new LoggerModel { Type = LoggerTypes.ERROR, Content = ex + "" });
        }

        [Conditional("DEBUG")]
        public void DebugError(string content, params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                content = string.Format(content, args);
            }
            Enqueue(new LoggerModel { Type = LoggerTypes.ERROR, Content = content });
        }

        [Conditional("DEBUG")]
        public void DebugError(Exception ex)
        {
            Enqueue(new LoggerModel { Type = LoggerTypes.ERROR, Content = ex + "" });
        }

        public void Enqueue(LoggerModel model)
        {
            queue.Enqueue(model);
        }
        public bool Dequeue(out LoggerModel model)
        {
            return queue.TryDequeue(out model);
        }
    }

    public class LoggerModel
    {
        public LoggerTypes Type { get; set; } = LoggerTypes.INFO;
        public DateTime Time { get; set; } = DateTime.Now;
        public string Content { get; set; } = string.Empty;
    }

    public enum LoggerTypes : byte
    {
        DEBUG = 0, INFO = 1, WARNING = 2, ERROR = 3
    }
}
