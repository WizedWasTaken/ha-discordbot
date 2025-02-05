using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BotTemplate.BotCore.Helpers
{
    public class DiscordLogger : Microsoft.Extensions.Logging.ILogger
    {
        private readonly string _webhookUrl;
        private readonly string _name;
        private static readonly HttpClient _httpClient = new HttpClient();

        public DiscordLogger(string name, string webhookUrl)
        {
            _name = name;
            _webhookUrl = webhookUrl;
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => null!;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel) || IsEfCoreLog(eventId))
            {
                return;
            }

            var message = formatter(state, exception);
            if (exception != null)
            {
                message += $"\nException: {exception}";
            }

            SendMessageAsync(logLevel, message, "Unknown User", "", "Log").GetAwaiter().GetResult();
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter, string doingUser, string profilePictureUrl, string embedTitle = "Log", string embedContent = "")
        {
            if (!IsEnabled(logLevel) || IsEfCoreLog(eventId))
            {
                return;
            }

            var message = formatter(state, exception);
            if (exception != null)
            {
                message += $"\nException: {exception}";
            }

            if (!string.IsNullOrEmpty(embedContent))
            {
                message = embedContent;
            }

            SendMessageAsync(logLevel, message, doingUser, profilePictureUrl, embedTitle).GetAwaiter().GetResult();
        }

        private static bool IsEfCoreLog(EventId eventId)
        {
            // EF Core event IDs are in the range 10000-19999
            return eventId.Id >= 10000 && eventId.Id < 20000;
        }

        private async Task SendMessageAsync(LogLevel logLevel, string message, string doingUser, string profilePictureUrl, string embedTitle)
        {
            var embed = new
            {
                embeds = new[]
                {
                    new
                    {
                        title = embedTitle,
                        description = message,
                        color = GetColor(logLevel),
                        timestamp = DateTime.UtcNow.ToString("o"),
                        footer = new
                        {
                            text = _name
                        },
                        author = new
                        {
                            name = doingUser,
                            icon_url = profilePictureUrl
                        }
                    }
                }
            };

            var jsonPayload = new StringContent(System.Text.Json.JsonSerializer.Serialize(embed), Encoding.UTF8, "application/json");
            await _httpClient.PostAsync(_webhookUrl, jsonPayload);
        }

        private static int GetColor(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Critical => 0xFF0000, // Red
                LogLevel.Error => 0xFF0000, // Red
                LogLevel.Warning => 0xFFA500, // Orange
                LogLevel.Information => 0x00FF00, // Green
                LogLevel.Debug => 0x0000FF, // Blue
                LogLevel.Trace => 0x808080, // Gray
                _ => 0xFFFFFF, // White
            };
        }
    }

    public class DiscordLoggerProvider : ILoggerProvider
    {
        private readonly string _webhookUrl;
        private bool _disposed;

        public DiscordLoggerProvider(string webhookUrl)
        {
            _webhookUrl = webhookUrl;
        }

        public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName)
        {
            return new DiscordLogger(categoryName, _webhookUrl);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources here if needed
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}