using BotTemplate.BotCore.Repositories;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BotTemplate.BotCore.Services
{
    public class BandeBuyService : IHostedService, IDisposable
    {
        private readonly ILogger<BandeBuyService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly DiscordSocketClient _discordClient;
        private readonly ulong _channelId;
        private ulong _messageId;
        private IEventRepository _eventRepository;

        public BandeBuyService(ILogger<BandeBuyService> logger, IServiceProvider serviceProvider, DiscordSocketClient discordClient, ulong channelId, ulong messageId, IEventRepository eventRepository)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _discordClient = discordClient;
            _channelId = channelId;
            _messageId = messageId;
            _eventRepository = eventRepository;

            _discordClient.Ready += OnReadyAsync;
        }

        private Task OnReadyAsync()
        {
            ReloadMessage();
            _logger.LogInformation("BandeBuyService is ready.");
            return Task.CompletedTask;
        }

        public async Task ReloadMessage()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var weaponRepository = scope.ServiceProvider.GetRequiredService<IBoughtWeaponRepository>();
                try
                {
                    var weaponsBought = await weaponRepository.GetWeaponsBoughtAsync();

                    var totalWeaponsBought = weaponsBought.Sum(item => item.Amount);
                    var totalWeaponPrice = weaponsBought.Sum(item => item.Weapon.WeaponPrice * item.Amount);

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
                        .WithTitle("BandeBuy liste")
                        .WithColor(Color.Green)
                        .WithThumbnailUrl(_discordClient.CurrentUser.GetAvatarUrl()) // Set the icon at the top
                        .AddField("Total pris", totalWeaponPrice.ToString("N0", new CultureInfo("de-DE")), inline: true);

                    var latestBandeBuyEvent = await _eventRepository.GetLatestBandeBuyEventAsync();
                    embedBuilder.Footer = new EmbedFooterBuilder
                    {
                        Text = $"BandeBuy Bestilling: {latestBandeBuyEvent.EventDate:dd/MM/yyyy}"
                    };

                    var groupedItems = weaponsBought.GroupBy(item => item.User.IngameName);

                    foreach (var group in groupedItems)
                    {
                        var buyerName = group.Key;
                        var buyerItems = string.Join("", group.Select(item => $"**Våben:** {item.Weapon.WeaponName} | **Antal:** {item.Amount}\n"));
                        var totalPrice = group.Sum(item => item.Weapon.WeaponPrice * item.Amount).ToString("N0", new CultureInfo("de-DE"));
                        var paid = group.All(item => item.Paid);
                        var delivered = group.All(item => item.DeliveredToUser);

                        embedBuilder.AddField($"\n\n**__{buyerName}__**", $"**Våben bestilling pris:** {totalPrice}\n**Betalt:** {(paid ? "Ja" : "Nej")}\n**Leveret:** {(delivered ? "Ja" : "Nej")}", inline: false);
                        embedBuilder.AddField("Våben", buyerItems, inline: false);
                    }

                    var listOfAllWeaponsAndAmountOrdered = string.Join("\n", weaponsBought.Select(item =>
                    {
                        var percentage = (double)item.Amount / item.Weapon.WeaponLimit * 100;
                        return $"**{item.Weapon.WeaponName}** - {item.Amount} ({percentage:F2}%)";
                    }));

                    embedBuilder.AddField("\n\n\n\n**__Våben liste__**\n\n", $"\n{listOfAllWeaponsAndAmountOrdered}", inline: false);

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
                    _logger.LogError(ex, "Error updating weapon purchase list");
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
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}