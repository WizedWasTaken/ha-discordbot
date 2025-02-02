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
            // Cast the interaction to SocketMessageComponent to get access to the CustomId.
            var componentInteraction = (SocketMessageComponent)Context.Interaction;
            var customId = componentInteraction.Data.CustomId; // e.g., "confirm_order_Pistol9mm_3"

            var parts = customId.Split('_');
            if (parts.Length < 4)
            {
                await FollowupAsync("Der opstod en fejl under behandlingen af din bestilling.", ephemeral: true);
                return;
            }

            // Here, parts[2] is the weapon name and parts[3] is the weapon amount.
            if (!Enum.TryParse<WeaponName>(parts[2], out var weaponName))
            {
                await FollowupAsync("Ugyldigt våben navn.", ephemeral: true);
                return;
            }

            if (!int.TryParse(parts[3], out int weaponAmount))
            {
                await FollowupAsync("Ugyldigt antal våben.", ephemeral: true);
                return;
            }

            // Proceed with the rest of your logic...
            var user = _userRepository.GetByDiscordId(Context.User.Id);
            var weapon = _boughtWeaponRepository.GetWeaponByName(weaponName);
            if (user == null || weapon == null)
            {
                await FollowupAsync("Der opstod en fejl under behandlingen af din bestilling.", ephemeral: true);
                return;
            }

            var latestBandeBuyEvent = await _eventRepository.GetLatestBandeBuyEventAsync();
            if (latestBandeBuyEvent == null)
            {
                await FollowupAsync("Der er ikke nogen bande buy event.", ephemeral: true);
                return;
            }

            // Check if the weapon already exists in the WeaponsBought collection
            var existingBoughtWeapon = latestBandeBuyEvent.WeaponsBought
                .FirstOrDefault(bw => bw.Weapon.WeaponName == weaponName && bw.User.UserId == user.UserId);

            if (existingBoughtWeapon != null)
            {
                // Update the existing BoughtWeapon entity
                existingBoughtWeapon.Amount += weaponAmount;
            }
            else
            {
                // Create a new BoughtWeapon entity
                var boughtWeapon = new BoughtWeapon
                {
                    Weapon = weapon,
                    Amount = weaponAmount,
                    User = user
                };

                latestBandeBuyEvent.WeaponsBought.Add(boughtWeapon);
            }

            _eventRepository.Update(latestBandeBuyEvent);

            await FollowupAsync($"Dine {weaponAmount} {weaponName} er tilføjet til bestillingen. Det koster dig {(weapon.WeaponPrice * weaponAmount).ToString("N0", new CultureInfo("de-DE"))}.", ephemeral: true);
            _bandeBuyService.ReloadMessage();
        }

        [ComponentInteraction("cancel_order_*", runMode: RunMode.Async)]
        public async Task CancelOrder()
        {
            await DeleteOriginalResponseAsync();
            await RespondAsync("Din bestilling er blevet annulleret.", ephemeral: true);
            _bandeBuyService.ReloadMessage();
        }

        [ComponentInteraction("event_coming:*", runMode: RunMode.Async)]
        public async Task HandleEventComingAsync(string eventId)
        {
            var user = Context.User;
            var eventEntity = _eventRepository.GetById(int.Parse(eventId));

            if (eventEntity == null)
            {
                await RespondAsync("Eventet blev ikke fundet.", ephemeral: true);
                return;
            }

            // Add the user to the participants list
            var participant = _userRepository.GetByDiscordId(user.Id);
            if (eventEntity.Participants?.Contains(participant) == true)
            {
                await RespondAsync("Du fjernede din registrering som deltagende.", ephemeral: true);
                eventEntity.Participants.Remove(participant);
                _eventRepository.Update(eventEntity);
                await UpdateEventMessage(eventEntity);
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
        }

        [ComponentInteraction("event_not_coming:*", runMode: RunMode.Async)]
        public async Task HandleEventNotComingAsync(string eventId)
        {
            var user = Context.User;
            var eventEntity = _eventRepository.GetById(int.Parse(eventId));

            if (eventEntity == null)
            {
                await RespondAsync("Eventet blev ikke fundet.", ephemeral: true);
                return;
            }

            // Get the participant from the user repository
            var participant = _userRepository.GetByDiscordId(user.Id);

            if (participant == null)
            {
                await RespondAsync("Du er ikke registreret som bruger.", ephemeral: true);
                return;
            }

            // Check if the user is already marked as absent
            if (eventEntity.Absent?.Contains(participant) == true)
            {
                await RespondAsync("Du fjernede din registrering som fraværende.", ephemeral: true);
                eventEntity.Absent.Remove(participant);
                _eventRepository.Update(eventEntity);
                await UpdateEventMessage(eventEntity);
                return;
            }

            if (eventEntity.Participants.Contains(participant))
            {
                eventEntity.Participants.Remove(participant);
            }

            // Add the user to the absent list
            eventEntity.Absent.Add(participant);
            _eventRepository.Update(eventEntity);

            await UpdateEventMessage(eventEntity);
            await RespondAsync("Du blev registreret som fraværende.", ephemeral: true);
        }

        private async Task UpdateEventMessage(Event eventEntity)
        {
            var channel = Context.Client.GetChannel(1334674825033027594) as IMessageChannel;
            if (channel != null)
            {
                var message = await channel.GetMessageAsync(ulong.Parse(eventEntity.MessageID)) as IUserMessage;
                if (message != null)
                {
                    var embed = CreateEventEmbed(eventEntity);
                    await message.ModifyAsync(msg => msg.Embed = embed.Build());
                }
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