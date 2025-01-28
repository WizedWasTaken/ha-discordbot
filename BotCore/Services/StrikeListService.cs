using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BotTemplate.BotCore.Repositories;
using Discord.WebSocket;
using Discord;

public class StrikeListService : IHostedService, IDisposable
{
    private readonly ILogger<StrikeListService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly DiscordSocketClient _discordClient;
    private readonly ulong _channelId;
    private ulong _messageId;
    private Timer _timer;

    public StrikeListService(ILogger<StrikeListService> logger, IServiceProvider serviceProvider, DiscordSocketClient discordClient, ulong channelId, ulong messageId)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _discordClient = discordClient;
        _channelId = channelId;
        _messageId = messageId;

        _discordClient.Ready += OnReadyAsync;
    }

    private Task OnReadyAsync()
    {
        _logger.LogInformation("Bot is ready. Starting the strike list update timer.");
        _timer = new Timer(UpdateStrikeList, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        return Task.CompletedTask;
    }

    private async void UpdateStrikeList(object state)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var strikeRepository = scope.ServiceProvider.GetRequiredService<IStrikeRepository>();
            try
            {
                var strikes = strikeRepository.GetAllStrikes()
                    .OrderBy(strike => strike.GivenTo.IngameName)
                    .ToList();

                var totalStrikes = strikes.Count;
                var activeStrikes = strikes.Count(strike => strike.IsStrikeActive());

                var guild = _discordClient.Guilds.FirstOrDefault();
                if (guild == null)
                {
                    _logger.LogError("Bot is not a member of any guilds.");
                    return;
                }

                var channel = guild.GetTextChannel(_channelId);
                if (channel == null)
                {
                    _logger.LogError("Channel with ID {ChannelId} not found. Available channels:", _channelId);
                    foreach (var ch in guild.TextChannels)
                    {
                        _logger.LogInformation("Channel: {ChannelName}, ID: {ChannelId}", ch.Name, ch.Id);
                    }
                    return;
                }

                if (_messageId == 0)
                {
                    IEnumerable<IMessage> messages = await channel.GetMessagesAsync(500).FlattenAsync();
                    await ((ITextChannel)channel).DeleteMessagesAsync(messages);
                }

                var embedBuilder = new EmbedBuilder()
                    .WithTitle("Strike List")
                    .WithColor(Color.Red)
                    .WithTimestamp(DateTimeOffset.Now)
                    .AddField("Total Strikes", totalStrikes, inline: true)
                    .AddField("Aktive Strikes", activeStrikes, inline: true);

                var groupedStrikes = strikes.GroupBy(strike => strike.GivenTo.IngameName);

                foreach (var group in groupedStrikes)
                {
                    var userName = group.Key;
                    var userStrikes = string.Join("\n\n", group.Select(strike => $"**ID:** {strike.StrikeId}\n**Grund:** {strike.Reason}\n**Dato:** {strike.Date}\n**Aktiv:** {(strike.IsStrikeActive() ? "Ja" : "Nej")}"));
                    var activeStrikesCount = group.Count(strike => strike.IsStrikeActive());

                    embedBuilder.AddField($"**__{userName}__**", $"Aktive Strikes: {activeStrikesCount}", inline: false);
                    embedBuilder.AddField("Strikes", userStrikes, inline: false);
                }

                var embed = embedBuilder.Build();

                if (_messageId == 0)
                {
                    var newMessage = await channel.SendMessageAsync(embed: embed);
                    _messageId = newMessage.Id;
                    _logger.LogInformation("New message sent with ID: {MessageId}", _messageId);
                }
                else
                {
                    var message = await channel.GetMessageAsync(_messageId) as IUserMessage;
                    if (message == null)
                    {
                        var newMessage = await channel.SendMessageAsync(embed: embed);
                        _messageId = newMessage.Id;
                        _logger.LogInformation("Message not found, new message sent with ID: {MessageId}", _messageId);
                    }
                    else
                    {
                        // Update the existing message with the new embed
                        await message.ModifyAsync(msg => msg.Embed = embed);
                        _logger.LogInformation("Message updated with ID: {MessageId}", _messageId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating strike list");
            }
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Do not start the timer here, it will be started in the OnReadyAsync method
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}