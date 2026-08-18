using log4net;
using log4net.Config;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
namespace Documate.Helper
{
    public static class Log4NetAspExtensions
    {
        public static void ConfigureLog4Net(this IWebHostEnvironment hostingEnv, string configFileRelativePath)
        {
            GlobalContext.Properties["appRoot"] = hostingEnv.ContentRootPath;
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo(Path.Combine(hostingEnv.ContentRootPath, configFileRelativePath)));
        }

        public static void AddLog4Net(this ILoggerFactory loggerFactory)
        {
            loggerFactory.AddProvider(new Log4NetProvider());
        }
    }

    public class Log4NetProvider : ILoggerProvider
    {
        private IDictionary<string, ILogger> _loggers
            = new Dictionary<string, ILogger>();

        public ILogger CreateLogger(string name)
        {
            if (!_loggers.ContainsKey(name))
            {
                lock (_loggers)
                {
                    // Have to check again since another thread may have gotten the lock first
                    if (!_loggers.ContainsKey(name))
                    {
                        _loggers[name] = new Log4NetAdapter(name);
                    }
                }
            }
            return _loggers[name];
        }

        public void Dispose()
        {
            _loggers.Clear();
            _loggers = null;
        }
    }

    public class Log4NetAdapter : ILogger
    {
        private ILog _logger;

        public Log4NetAdapter(string loggerName)
        {
            _logger = LogManager.GetLogger(GetType()); // to verify again - Faheem (25/03/2020)
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null; ;
        }

        public IDisposable BeginScopeImpl(object state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Debug:
                    return _logger.IsDebugEnabled;
                case LogLevel.Information:
                    return _logger.IsInfoEnabled;
                case LogLevel.Warning:
                    return _logger.IsWarnEnabled;
                case LogLevel.Error:
                    return _logger.IsErrorEnabled;
                case LogLevel.Critical:
                    return _logger.IsFatalEnabled;
                default:
                    throw new ArgumentException($"Unknown log level {logLevel}.", nameof(logLevel));
            }
        }

        public void Log(LogLevel logLevel, int eventId, object state, Exception exception, Func<object, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }
            string message = null;
            if (null != formatter)
            {
                message = formatter(state, exception);
            }
            else
            {
                //message = new LogFormatter().Format(state, exception); // to fix later - Faheem (25/03/2020)
            }
            switch (logLevel)
            {
                case LogLevel.Debug:
                    _logger.Debug(message, exception);
                    break;
                case LogLevel.Information:
                    _logger.Info(message, exception);
                    break;
                case LogLevel.Warning:
                    _logger.Warn(message, exception);
                    break;
                case LogLevel.Error:
                    _logger.Error(message, exception);
                    break;
                case LogLevel.Critical:
                    _logger.Fatal(message, exception);
                    break;
                default:
                    _logger.Warn($"Encountered unknown log level {logLevel}, writing out as Info.");
                    _logger.Info(message, exception);
                    break;
            }
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            throw new NotImplementedException();
        }
    }
}
