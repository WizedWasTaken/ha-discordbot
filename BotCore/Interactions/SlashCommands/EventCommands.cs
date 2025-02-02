using BotTemplate.BotCore.DataAccess;
using BotTemplate.BotCore.Entities;
using BotTemplate.BotCore.Repositories;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using System.Globalization;
using static BotTemplate.BotCore.Interactions.Modals.Modals;

namespace BotTemplate.BotCore.Interactions.SlashCommands
{
    [Group("event", "Kommandoer til at administrere events")]
    public class EventCommands : InteractionsCore
    {
        private readonly ILogger<EventCommands> _logger;
        private readonly IUserRepository _userRepository;
        private readonly IEventRepository _eventRepository;

        public EventCommands(ILogger<EventCommands> logger, IUserRepository userRepository, IEventRepository eventRepository)
        {
            _logger = logger;
            _userRepository = userRepository;
            _eventRepository = eventRepository;
        }

        [SlashCommand("opret", "Opret nyt event")]
        public async Task OpretEventAsync(EventType eventType, string titel, string beskrivelse, string lokation, bool tilladReaktioner, string dato)
        {
            // Defer the interaction to prevent the interaction from timing out.
            await DeferAsync(ephemeral: true);
            _logger.LogInformation("Creating new event...");

            if (string.IsNullOrEmpty(titel) || string.IsNullOrEmpty(beskrivelse) || string.IsNullOrEmpty(lokation) || string.IsNullOrEmpty(dato))
            {
                await FollowupAsync("Alle felter skal udfyldes.", ephemeral: true);
                return;
            }

            // Parse the date string to DateTime
            if (!DateTime.TryParseExact(dato, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
            {
                await FollowupAsync("Invalidt dato format. Brug venligst: dd/MM/yyyy.", ephemeral: true);
                return;
            }

            if (parsedDate < DateTime.Now)
            {
                await FollowupAsync("Datoen kan ikke være i fortiden.", ephemeral: true);
                return;
            }

            if (eventType == EventType.BandeBuy && tilladReaktioner)
            {
                await FollowupAsync("BandeBuy events kan ikke have reaktioner.", ephemeral: true);
                return;
            }

            var user = _userRepository.GetByDiscordId(Context.User.Id);

            // Create the event
            Event newEvent = new Event
            {
                EventType = eventType,
                EventTitle = titel,
                EventDescription = beskrivelse,
                EventLocation = lokation,
                EventDate = parsedDate,
                MadeBy = user
            };

            if (tilladReaktioner)
            {
                // Add the user as a participant
                newEvent.Participants.Add(user);
            }

            // Save the event to the database
            _eventRepository.Add(newEvent);

            EmbedBuilder embed = new EmbedBuilder();

            if (eventType == EventType.BandeBuy)
            {
                // Create an embed to show the event details
                embed
                    .WithTitle(newEvent.EventTitle)
                    .WithDescription(newEvent.EventDescription)
                    .AddField("Type", newEvent.EventType.ToString())
                    .AddField("Dato", newEvent.EventDate.ToString("dd/MM/yyyy"))
                    .WithColor(Color.Red);

                embed.Footer = new EmbedFooterBuilder()
                    .WithText($"Oprettet af HA-Ledelsen");
            }
            else
            {
                // Create an embed to show the event details
                embed
                    .WithTitle(newEvent.EventTitle)
                    .WithDescription(newEvent.EventDescription)
                    .AddField("Type", newEvent.EventType.ToString())
                    .AddField("Dato", newEvent.EventDate.ToString("dd/MM/yyyy"))
                    .AddField("Lokation", newEvent.EventLocation)
                    .WithColor(Color.Red);

                embed.Footer = new EmbedFooterBuilder()
                    .WithText($"Oprettet af {user.IngameName}");
            }

            // Create buttons if reactions are allowed
            ComponentBuilder componentBuilder = new ComponentBuilder();
            if (tilladReaktioner)
            {
                componentBuilder.WithButton("Kommer", $"event_coming:{newEvent.EventId}", ButtonStyle.Success, new Emoji("✅"))
                                .WithButton("Kommer ikke", $"event_not_coming:{newEvent.EventId}", ButtonStyle.Danger, new Emoji("❌"));
            }

            // Send the embed in the specified channel with @everyone mention
            var channel = Context.Client.GetChannel(1334674825033027594) as IMessageChannel;
            if (channel != null)
            {
                var message = await channel.SendMessageAsync("@everyone", embed: embed.Build(), components: componentBuilder.Build());
                newEvent.MessageID = message.Id.ToString();
                _eventRepository.Update(newEvent);
            }

            // Follow up with a confirmation message
            await FollowupAsync("Event oprettet og sendt i kanalen.", ephemeral: true);
        }

        [SlashCommand("se", "Ser et event")]
        public async Task SeEventAsync(ViewEventEnum viewEvent)
        {
            // Defer the interaction to prevent the interaction from timing out.
            await DeferAsync(ephemeral: true);
            _logger.LogInformation("Viewing event...");

            if (viewEvent == null)
            {
                await FollowupAsync("Der skete en fejl.", ephemeral: true);
                return;
            }

            List<Event> events = new();

            switch (viewEvent)
            {
                case ViewEventEnum.Alle:
                    events = (List<Event>)_eventRepository.GetAll();
                    break;

                case ViewEventEnum.Seneste:
                    events.Add(_eventRepository.GetLatest());
                    break;

                case ViewEventEnum.Deltagende:
                    events = (List<Event>)_eventRepository.GetParticipating(Context.User.Id);
                    break;

                case ViewEventEnum.Afslået:
                    events = (List<Event>)_eventRepository.GetDeclined(Context.User.Id);
                    break;

                case ViewEventEnum.Aktive:
                    events = (List<Event>)_eventRepository.GetActive();
                    break;

                default:
                    break;
            }

            if (events.Count == 0)
            {
                await FollowupAsync("Ingen events fundet.", ephemeral: true);
                return;
            }

            // Group events by type
            var groupedEvents = events.GroupBy(e => e.EventType)
                                      .OrderBy(g => g.Key);

            EmbedBuilder embed = new EmbedBuilder()
                .WithTitle("Events")
                .WithColor(Color.Red);

            foreach (var group in groupedEvents)
            {
                embed.AddField($"**{group.Key}**", "\u200B"); // Add a header for each event type

                foreach (var e in group)
                {
                    embed.AddField(e.EventTitle, e.EventDescription)
                         .AddField("Type", e.EventType.ToString(), true)
                         .AddField("Dato", e.EventDate.ToString("dd/MM/yyyy"), true)
                         .AddField("Lokation", e.EventLocation, true)
                         .AddField("Oprettet af", e.MadeBy.IngameName, true)
                         .AddField("\u200B", "\u200B"); // Add a blank field for spacing
                }
            }

            await FollowupAsync(embed: embed.Build(), ephemeral: true);
        }

        [SlashCommand("slet", "Sletter et event")]
        public async Task SletEventAsync(string eventTitle)
        {
            // Defer the interaction to prevent the interaction from timing out.
            await DeferAsync(ephemeral: true);
            _logger.LogInformation("Deleting event...");

            if (string.IsNullOrEmpty(eventTitle))
            {
                await FollowupAsync("Event titel skal udfyldes.", ephemeral: true);
                return;
            }

            Event eventToDelete = _eventRepository.GetByTitle(eventTitle);

            if (eventToDelete == null)
            {
                await FollowupAsync("Eventet blev ikke fundet.", ephemeral: true);
                return;
            }

            // Delete the channel message
            var channel = Context.Client.GetChannel(1334674825033027594) as IMessageChannel;
            if (channel != null)
            {
                var message = await channel.GetMessageAsync(ulong.Parse(eventToDelete.MessageID));
                if (message != null)
                {
                    await message.DeleteAsync();
                }
            }

            // Delete the event from the database
            _eventRepository.Delete(eventToDelete);

            // Follow up with a confirmation message
            await FollowupAsync("Event slettet.", ephemeral: true);
        }

        [ComponentInteraction("event_coming:*", runMode: RunMode.Async)]
        public async Task HandleEventComingAsync(string eventId)
        {
            // Simple response for now
            await RespondAsync("Du har angivet, at du kommer til eventet.", ephemeral: true);
        }

        [ComponentInteraction("event_not_coming:*", runMode: RunMode.Async)]
        public async Task HandleEventNotComingAsync(string eventId)
        {
            // Simple response for now
            await RespondAsync("Du har angivet, at du ikke kommer til eventet.", ephemeral: true);
        }

        public enum ViewEventEnum
        {
            Alle,
            Seneste,
            Deltagende,
            Afslået,
            Aktive,
        }
    }
}