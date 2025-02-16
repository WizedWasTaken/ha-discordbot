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
using System.Timers;

namespace BotTemplate.BotCore.Services
{
    public class BandeBuyService : IHostedService, IDisposable
    {
        private readonly ILogger<BandeBuyService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly DiscordSocketClient _discordClient;
        private readonly ulong _channelId;
        private ulong _messageId;
        private readonly IEventRepository _eventRepository;
        private bool _disposed;
        private System.Timers.Timer _timer;
        private string _lastMessageContent;
        private readonly IPaidAmountRepository _paidAmountRepository;

        public BandeBuyService(ILogger<BandeBuyService> logger, IServiceProvider serviceProvider, DiscordSocketClient discordClient, ulong channelId, ulong messageId, IEventRepository eventRepository, IPaidAmountRepository paidAmountRepository)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _discordClient = discordClient;
            _channelId = channelId;
            _messageId = messageId;
            _eventRepository = eventRepository;
            _paidAmountRepository = paidAmountRepository;

            _discordClient.Ready += OnReadyAsync;
            _timer = new System.Timers.Timer();
            _lastMessageContent = string.Empty;
        }

        private async Task OnReadyAsync()
        {
            await ReloadMessage();
            _timer.Interval = TimeSpan.FromMinutes(1).TotalMilliseconds;
            _timer.Elapsed += async (sender, e) => await ReloadMessage();
            _timer.AutoReset = true;
            _timer.Enabled = true;
            _logger.LogInformation("BandeBuyService is ready.");
        }

        private async Task ReloadMessage()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var weaponRepository = scope.ServiceProvider.GetRequiredService<IBoughtWeaponRepository>();
                try
                {
                    var weaponsBought = await weaponRepository.GetWeaponsBoughtAsync();
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
                    var fields = new List<EmbedFieldBuilder>();

                    foreach (var group in groupedItems)
                    {
                        var buyerName = group.Key;
                        var buyerItems = string.Join("", group
                            .OrderBy(item => item.Weapon.WeaponName) // Sort by starting character
                            .Select(item => $"**Våben:** {item.Weapon.WeaponName} | **Antal:** {item.Amount}\n"));
                        var totalPrice = group.Sum(item => item.Weapon.WeaponPrice * item.Amount)
                            .ToString("N0", new CultureInfo("de-DE"));
                        var payment = await _paidAmountRepository.GetUserPaidAmountAsync(group.First().User, latestBandeBuyEvent);
                        var userTotalPrice = group.Sum(item => item.Weapon.WeaponPrice * item.Amount);
                        var missingToPay = userTotalPrice - payment.Amount;
                        var delivered = group.All(item => item.DeliveredToUser);

                        fields.Add(new EmbedFieldBuilder
                        {
                            Name = $"\n\n**__{buyerName}__**",
                            Value = $"**Våben bestilling pris:** {totalPrice}\n**Mangler at betale:** {missingToPay.ToString("N0", new CultureInfo("de-DE"))}\n**Leveret:** {(delivered ? "Ja" : "Nej")}",
                            IsInline = false
                        });
                        fields.Add(new EmbedFieldBuilder
                        {
                            Name = "Våben",
                            Value = buyerItems,
                            IsInline = false
                        });
                    }

                    var groupedWeapons = weaponsBought
                        .GroupBy(item => item.Weapon.WeaponName)
                        .Select(group => new
                        {
                            WeaponName = group.Key,
                            TotalAmount = group.Sum(item => item.Amount),
                            WeaponLimit = group.First().Weapon.WeaponLimit
                        });

                    var listOfAllWeaponsAndAmountOrdered = string.Join("\n", groupedWeapons
                        .OrderBy(group => group.WeaponName) // Sort by starting character
                        .Select(group =>
                        {
                            var percentage = (double)group.TotalAmount / group.WeaponLimit * 100;
                            return $"**{group.WeaponName}** | **Antal:** {group.TotalAmount} stk. ({percentage:F2}%)";
                        }));

                    fields.Add(new EmbedFieldBuilder
                    {
                        Name = "\n\n\n\n**__Våben liste__**\n\n",
                        Value = $"\n{listOfAllWeaponsAndAmountOrdered}",
                        IsInline = false
                    });

                    // Split fields into multiple embeds if necessary
                    var embeds = new List<Embed>();
                    var currentEmbedBuilder = new EmbedBuilder()
                        .WithTitle(embedBuilder.Title)
                        .WithColor(embedBuilder.Color ?? Color.Default)
                        .WithThumbnailUrl(embedBuilder.ThumbnailUrl)
                        .WithFooter(embedBuilder.Footer)
                        .AddField(embedBuilder.Fields[0]);

                    foreach (var field in fields)
                    {
                        if (currentEmbedBuilder.Fields.Count >= 25)
                        {
                            embeds.Add(currentEmbedBuilder.Build());
                            currentEmbedBuilder = new EmbedBuilder()
                                .WithTitle(embedBuilder.Title)
                                .WithColor(embedBuilder.Color ?? Color.Default)
                                .WithThumbnailUrl(embedBuilder.ThumbnailUrl)
                                .WithFooter(embedBuilder.Footer);
                        }
                        currentEmbedBuilder.AddField(field);
                    }
                    embeds.Add(currentEmbedBuilder.Build());

                    if (_messageId != 0)
                    {
                        var oldMessage = await channel.GetMessageAsync(_messageId) as IUserMessage;
                        if (oldMessage != null)
                        {
                            await oldMessage.DeleteAsync();
                        }
                    }

                    // Delete all messages in channel
                    var messages = await channel.GetMessagesAsync().FlattenAsync();
                    foreach (var message in messages)
                    {
                        await message.DeleteAsync();
                    }

                    foreach (var embed in embeds)
                    {
                        var newMessage = await channel.SendMessageAsync(embed: embed);
                        _messageId = newMessage.Id;
                        _logger.LogInformation("New message sent with ID: {MessageId}", _messageId);
                    }

                    _logger.LogInformation("Weapon purchase list updated successfully.");
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
            _logger.LogInformation("BandeBuyService is starting.");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("BandeBuyService is stopping.");
            return Task.CompletedTask;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _timer?.Dispose();
                    _logger.LogInformation("BandeBuyService timer disposed.");
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
            _logger.LogInformation("BandeBuyService disposed.");
        }
    }
}