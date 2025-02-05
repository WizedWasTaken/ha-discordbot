using Discord.Interactions;
using BotTemplate.BotCore.Repositories;
using Microsoft.Extensions.Logging;
using BotTemplate.BotCore.Entities;
using Discord;
using System.Globalization;
using BotTemplate.BotCore.Services;

namespace BotTemplate.BotCore.Interactions.Buttons
{
    public class Buttons : InteractionsCore
    {
        private readonly IEventRepository _eventRepository;
        private readonly IBoughtWeaponRepository _boughtWeaponRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<Buttons> _logger;
        private readonly BandeBuyService _bandeBuyService;

        public Buttons(IEventRepository eventRepository, ILogger<Buttons> logger, IUserRepository userRepository, IBoughtWeaponRepository boughtWeaponRepository, BandeBuyService bandeBuyService)
        {
            _eventRepository = eventRepository;
            _logger = logger;
            _userRepository = userRepository;
            _boughtWeaponRepository = boughtWeaponRepository;
            _bandeBuyService = bandeBuyService;
        }

        [ComponentInteraction("confirm_order_*", runMode: RunMode.Async)]
        public async Task ConfirmOrder()
        {
            await DeferAsync(ephemeral: true);
            await DeleteOriginalResponseAsync();
            var componentInteraction = (SocketMessageComponent)Context.Interaction;
            var customId = componentInteraction.Data.CustomId; // e.g., "confirm_order_Pistol9mm_3"

            var parts = customId.Split('_');
            if (parts.Length < 4)
            {
                _logger.LogError("Invalid customId format: {CustomId}", customId);
                await FollowupAsync("Der opstod en fejl under behandlingen af din bestilling.", ephemeral: true);
                return;
            }

            if (!Enum.TryParse<WeaponName>(parts[2], out var weaponName))
            {
                _logger.LogError("Invalid weapon name: {WeaponName}", parts[2]);
                await FollowupAsync("Ugyldigt våben navn.", ephemeral: true);
                return;
            }

            if (!int.TryParse(parts[3], out int weaponAmount))
            {
                _logger.LogError("Invalid weapon amount: {WeaponAmount}", parts[3]);
                await FollowupAsync("Ugyldigt antal våben.", ephemeral: true);
                return;
            }

            var user = _userRepository.GetByDiscordId(Context.User.Id);
            var weapon = _boughtWeaponRepository.GetWeaponByName(weaponName);
            if (user == null || weapon == null)
            {
                _logger.LogError("User or weapon not found. User: {UserId}, Weapon: {WeaponName}", Context.User.Id, weaponName);
                await FollowupAsync("Der opstod en fejl under behandlingen af din bestilling.", ephemeral: true);
                return;
            }

            var latestBandeBuyEvent = await _eventRepository.GetLatestBandeBuyEventAsync();
            if (latestBandeBuyEvent == null)
            {
                _logger.LogError("No BandeBuy event found.");
                await FollowupAsync("Der er ikke nogen bande buy event.", ephemeral: true);
                return;
            }

            var existingBoughtWeapon = latestBandeBuyEvent.WeaponsBought
                .FirstOrDefault(bw => bw.Weapon.WeaponName == weaponName && bw.User.UserId == user.UserId);

            if (existingBoughtWeapon != null)
            {
                existingBoughtWeapon.Amount += weaponAmount;
                _logger.LogInformation("Updated existing weapon order. User: {UserId}, Weapon: {WeaponName}, Amount: {Amount}", user.UserId, weaponName, existingBoughtWeapon.Amount);
            }
            else
            {
                var boughtWeapon = new BoughtWeapon
                {
                    Weapon = weapon,
                    Amount = weaponAmount,
                    User = user
                };
                latestBandeBuyEvent.WeaponsBought.Add(boughtWeapon);
                _logger.LogInformation("Added new weapon order. User: {UserId}, Weapon: {WeaponName}, Amount: {Amount}", user.UserId, weaponName, weaponAmount);
            }

            _eventRepository.Update(latestBandeBuyEvent);
            _logger.LogInformation("BandeBuy event updated successfully.");

            await FollowupAsync($"Dine {weaponAmount} {weaponName} er tilføjet til bestillingen. Det koster dig {(weapon.WeaponPrice * weaponAmount).ToString("N0", new CultureInfo("de-DE"))}.", ephemeral: true);
        }

        [ComponentInteraction("cancel_order_*", runMode: RunMode.Async)]
        public async Task CancelOrder()
        {
            await DeleteOriginalResponseAsync();
            await RespondAsync("Din bestilling er blevet annulleret.", ephemeral: true);
            _logger.LogInformation("Order cancelled by user: {UserId}", Context.User.Id);
        }

        [ComponentInteraction("event_coming:*", runMode: RunMode.Async)]
        public async Task HandleEventComingAsync(string eventId)
        {
            var user = Context.User;
            var eventEntity = _eventRepository.GetById(int.Parse(eventId));

            if (eventEntity == null)
            {
                _logger.LogError("Event not found. EventId: {EventId}", eventId);
                await RespondAsync("Eventet blev ikke fundet.", ephemeral: true);
                return;
            }

            var participant = _userRepository.GetByDiscordId(user.Id);
            if (eventEntity.Participants?.Contains(participant) == true)
            {
                eventEntity.Participants.Remove(participant);
                _eventRepository.Update(eventEntity);
                await UpdateEventMessage(eventEntity);
                await RespondAsync("Du fjernede din registrering som deltagende.", ephemeral: true);
                _logger.LogInformation("User removed from participants. User: {UserId}, EventId: {EventId}", user.Id, eventId);
                return;
            }

            if (eventEntity.Absent?.Contains(participant) == true)
            {
                eventEntity.Absent.Remove(participant);
            }

            eventEntity.Participants.Add(participant);
            _eventRepository.Update(eventEntity);
            await UpdateEventMessage(eventEntity);
            await RespondAsync("Du er nu registreret som deltagende.", ephemeral: true);
            _logger.LogInformation("User added to participants. User: {UserId}, EventId: {EventId}", user.Id, eventId);
        }

        [ComponentInteraction("event_not_coming:*", runMode: RunMode.Async)]
        public async Task HandleEventNotComingAsync(string eventId)
        {
            var user = Context.User;
            var eventEntity = _eventRepository.GetById(int.Parse(eventId));

            if (eventEntity == null)
            {
                _logger.LogError("Event not found. EventId: {EventId}", eventId);
                await RespondAsync("Eventet blev ikke fundet.", ephemeral: true);
                return;
            }

            var participant = _userRepository.GetByDiscordId(user.Id);

            if (participant == null)
            {
                _logger.LogError("User not found. UserId: {UserId}", user.Id);
                await RespondAsync("Du er ikke registreret som bruger.", ephemeral: true);
                return;
            }

            if (eventEntity.Absent?.Contains(participant) == true)
            {
                eventEntity.Absent.Remove(participant);
                _eventRepository.Update(eventEntity);
                await UpdateEventMessage(eventEntity);
                await RespondAsync("Du fjernede din registrering som fraværende.", ephemeral: true);
                _logger.LogInformation("User removed from absentees. User: {UserId}, EventId: {EventId}", user.Id, eventId);
                return;
            }

            if (eventEntity.Participants.Contains(participant))
            {
                eventEntity.Participants.Remove(participant);
            }

            eventEntity.Absent.Add(participant);
            _eventRepository.Update(eventEntity);
            await UpdateEventMessage(eventEntity);
            await RespondAsync("Du blev registreret som fraværende.", ephemeral: true);
            _logger.LogInformation("User added to absentees. User: {UserId}, EventId: {EventId}", user.Id, eventId);
        }

        private async Task UpdateEventMessage(Event eventEntity)
        {
            var channel = Context.Client.GetChannel(1335783333006938132) as IMessageChannel;
            if (channel != null)
            {
                var message = await channel.GetMessageAsync(ulong.Parse(eventEntity.MessageID)) as IUserMessage;
                if (message != null)
                {
                    var embed = CreateEventEmbed(eventEntity);
                    await message.ModifyAsync(msg => msg.Embed = embed.Build());
                    _logger.LogInformation("Event message updated. EventId: {EventId}", eventEntity.EventId);
                }
                else
                {
                    _logger.LogError("Message not found. MessageId: {MessageId}", eventEntity.MessageID);
                }
            }
            else
            {
                _logger.LogError("Channel not found. ChannelId: {ChannelId}", 1335783333006938132);
            }
        }

        private EmbedBuilder CreateEventEmbed(Event eventEntity)
        {
            var embed = new EmbedBuilder()
                .WithTitle(eventEntity.EventTitle)
                .WithDescription(eventEntity.EventDescription)
                .AddField("Type", eventEntity.EventType.ToString())
                .AddField("Dato", eventEntity.EventDate.ToString("dd/MM/yyyy"))
                .WithColor(Color.Red);

            if (eventEntity.EventType != EventType.BandeBuy)
            {
                embed.AddField("Lokation", eventEntity.EventLocation);
                embed.Footer = new EmbedFooterBuilder()
                    .WithText($"Oprettet af {eventEntity.MadeBy.IngameName}");
            }
            else
            {
                embed.Footer = new EmbedFooterBuilder()
                    .WithText($"Oprettet af HA-Ledelsen");
            }

            var participants = string.Join(", ", eventEntity.Participants.Select(p => p.IngameName));
            var absentees = string.Join(", ", eventEntity.Absent.Select(a => a.IngameName));

            embed.AddField($"Deltagere ({eventEntity.Participants.Count()})", string.IsNullOrEmpty(participants) ? "Ingen deltagere" : participants);
            embed.AddField($"Fraværende ({eventEntity.Absent.Count()})", string.IsNullOrEmpty(absentees) ? "Ingen fraværende" : absentees);

            return embed;
        }
    }
}