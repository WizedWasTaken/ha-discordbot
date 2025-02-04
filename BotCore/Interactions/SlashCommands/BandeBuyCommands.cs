using BotTemplate.BotCore.DataAccess;
using BotTemplate.BotCore.Entities;
using BotTemplate.BotCore.Repositories;
using BotTemplate.BotCore.Services;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using System.Globalization;

namespace BotTemplate.BotCore.Interactions.SlashCommands
{
    [Group("bandebuy", "Kommandoer til at administrere bande buy")]
    public class BandeBuyCommands : InteractionsCore
    {
        private readonly ILogger<BandeBuyCommands> _logger;
        private readonly IUserRepository _userRepository;
        private readonly IRepository<Weapon> _weaponRepository;
        private readonly IEventRepository _eventRepository;
        private readonly BandeBuyService _bandeBuyService;
        private readonly IBoughtWeaponRepository _boughtWeaponRepository;

        public BandeBuyCommands(ILogger<BandeBuyCommands> logger, IUserRepository userRepository, IBoughtWeaponRepository boughtWeaponRepository, IRepository<Weapon> weaponRepository, IEventRepository eventRepository, BandeBuyService bandeBuyService)
        {
            _logger = logger;
            _userRepository = userRepository;
            _boughtWeaponRepository = boughtWeaponRepository;
            _weaponRepository = weaponRepository;
            _eventRepository = eventRepository;
            _bandeBuyService = bandeBuyService;
        }

        [SlashCommand("createweapon", "Opret et våben")]
        [RequireRole("SGT At Arms")]
        public async Task CreateWeaponAsync(WeaponName weaponName, int price, int limit)
        {
            var weapon = new Weapon
            {
                WeaponName = weaponName,
                WeaponPrice = price,
                WeaponLimit = limit
            };

            _weaponRepository.Add(weapon);
            await RespondAsync($"Våbenet {weaponName} er blevet oprettet.");
        }

        [SlashCommand("buyweapon", "Køb et våben")]
        [RequireRole("Bandebuy Adgang")]
        public async Task BuyWeaponAsync(WeaponName weaponName, string amount)
        {
            await DeferAsync(ephemeral: true);
            var user = await _userRepository.GetByDiscordIdAsync(Context.User.Id);
            var weapon = await _boughtWeaponRepository.GetByWeaponNameAsync(weaponName);

            if (user == null)
            {
                await FollowupAsync("Du er ikke registreret som bruger.", ephemeral: true);
                return;
            }

            if (weapon == null)
            {
                await FollowupAsync("Våbenet blev ikke fundet.", ephemeral: true);
                return;
            }

            if (!int.TryParse(amount, out int weaponAmount))
            {
                await FollowupAsync("Ugyldigt antal.", ephemeral: true);
                return;
            }

            if (weaponAmount <= 0)
            {
                await FollowupAsync("Antal skal være større end 0.", ephemeral: true);
                return;
            }

            if (weaponAmount > weapon.WeaponLimit)
            {
                await FollowupAsync($"Du kan kun købe {weapon.WeaponLimit} {weaponName} ad gangen.", ephemeral: true);
                return;
            }

            var latestBandeBuyEvent = await _eventRepository.GetLatestBandeBuyEventAsync();
            if (latestBandeBuyEvent == null)
            {
                await FollowupAsync("Der er ikke nogen bande buy event.", ephemeral: true);
                return;
            }

            var weaponLimitLeft = await _boughtWeaponRepository.GetWeaponLimitLeftAsync(weaponName);
            if (weaponLimitLeft == 0)
            {
                await FollowupAsync($"Der kan ikke købes flere {weaponName}.", ephemeral: true);
                return;
            }
            else if (weaponAmount > weaponLimitLeft)
            {
                await FollowupAsync($"Du kan kun købe {weaponLimitLeft} {weaponName} mere.", ephemeral: true);
                return;
            }

            // Build a custom ID that encodes the weapon name and amount
            var customId = $"confirm_order_{weaponName}_{weaponAmount}";
            var customCancel = $"cancel_order_{weaponName}_{weaponAmount}";

            var component = new ComponentBuilder()
                .WithButton("Bekræft", customId, ButtonStyle.Primary)
                .WithButton("Annuller", "cancel_order", ButtonStyle.Danger)
                .Build();

            await FollowupAsync($"Er du sikker på, at du vil købe {weaponAmount} {weaponName} for {(weapon.WeaponPrice * weaponAmount).ToString("N0", new CultureInfo("de-DE"))}?", components: component, ephemeral: true);
        }

        [SlashCommand("ispayed", "Marker at en bruger har betalt for våben")]
        public async Task IsPayedAsync(string discordId, bool isPayed)
        {
            var user = new User();
            if (discordId == null)
            {
                discordId = Context.User.Id.ToString();
            }
            else
            {
                if (!ulong.TryParse(discordId, out ulong idToSearch))
                {
                    var errorEmbed = new EmbedBuilder()
                        .WithTitle("Fejl")
                        .WithDescription("Indtast et gyldigt heltal for Discord ID.")
                        .WithColor(Color.Red)
                        .Build();
                    await RespondAsync(embed: errorEmbed, ephemeral: true);
                    return;
                }

                user = await _userRepository.GetByDiscordIdAsync(idToSearch);

                var socketUser = Context.Interaction.User as SocketGuildUser;
                if (!socketUser.GuildPermissions.BanMembers)
                {
                    var errorEmbed = new EmbedBuilder()
                        .WithTitle("Fejl")
                        .WithDescription("Du har ikke tilladelse til at redigere dette.")
                        .WithColor(Color.Red)
                        .Build();
                    await RespondAsync(embed: errorEmbed, ephemeral: true);
                    return;
                }
            }

            if (user == null)
            {
                await RespondAsync("Brugeren blev ikke fundet.", ephemeral: true);
                return;
            }

            var latestBandeBuyEvent = await _eventRepository.GetLatestBandeBuyEventAsync();
            if (latestBandeBuyEvent == null)
            {
                await RespondAsync("Der er ikke nogen bande buy event.", ephemeral: true);
                return;
            }

            var weaponsBought = latestBandeBuyEvent.WeaponsBought.Where(x => x.User == user).ToList();
            if (weaponsBought.Count == 0)
            {
                await RespondAsync("Brugeren har ikke bestilt nogle våben.", ephemeral: true);
                return;
            }

            foreach (var weapon in weaponsBought)
            {
                weapon.Paid = isPayed;
                _boughtWeaponRepository.Update(weapon);
            }

            await RespondAsync($"Brugeren har nu betalt for sine våben.", ephemeral: true);
            _ = _bandeBuyService.ReloadMessage();
        }

        [SlashCommand("isdelivered", "Marker alle våben som leveret")]
        public async Task IsDeliveredAsync(string discordId, bool isDelivered)
        {
            var user = new User();
            if (discordId == null)
            {
                discordId = Context.User.Id.ToString();
            }
            else
            {
                if (!ulong.TryParse(discordId, out ulong idToSearch))
                {
                    var errorEmbed = new EmbedBuilder()
                        .WithTitle("Fejl")
                        .WithDescription("Indtast et gyldigt heltal for Discord ID.")
                        .WithColor(Color.Red)
                        .Build();
                    await RespondAsync(embed: errorEmbed, ephemeral: true);
                    return;
                }

                user = await _userRepository.GetByDiscordIdAsync(idToSearch);

                var socketUser = Context.Interaction.User as SocketGuildUser;
                if (!socketUser.GuildPermissions.BanMembers)
                {
                    var errorEmbed = new EmbedBuilder()
                        .WithTitle("Fejl")
                        .WithDescription("Du har ikke tilladelse til at redigere dette.")
                        .WithColor(Color.Red)
                        .Build();
                    await RespondAsync(embed: errorEmbed, ephemeral: true);
                    return;
                }
            }

            if (user == null)
            {
                await RespondAsync("Brugeren blev ikke fundet.", ephemeral: true);
                return;
            }

            var latestBandeBuyEvent = await _eventRepository.GetLatestBandeBuyEventAsync();
            if (latestBandeBuyEvent == null)
            {
                await RespondAsync("Der er ikke nogen bande buy event.", ephemeral: true);
                return;
            }

            var weaponsBought = latestBandeBuyEvent.WeaponsBought.Where(x => x.User == user).ToList();
            if (weaponsBought.Count == 0)
            {
                await RespondAsync("Brugeren har ikke bestilt nogle våben.", ephemeral: true);
                return;
            }

            foreach (var weapon in weaponsBought)
            {
                weapon.DeliveredToUser = isDelivered;
                _boughtWeaponRepository.Update(weapon);
            }

            await RespondAsync($"Brugeren har nu fået sine våben.", ephemeral: true);
            _ = _bandeBuyService.ReloadMessage();
        }

        [SlashCommand("removeweapon", "Slet våben fra dine våben bestillinger.")]
        [RequireRole("Bandebuy Adgang")]
        public async Task DeleteWeaponOrder(WeaponName weaponName, string amount)
        {
            var user = await _userRepository.GetByDiscordIdAsync(Context.User.Id);
            var weapon = await _boughtWeaponRepository.GetByWeaponNameAsync(weaponName);

            if (user == null)
            {
                await RespondAsync("Du er ikke registreret som bruger.", ephemeral: true);
                return;
            }

            if (weapon == null)
            {
                await RespondAsync("Våbenet blev ikke fundet.", ephemeral: true);
                return;
            }

            if (!int.TryParse(amount, out int removeAmount))
            {
                await RespondAsync("Ugyldigt antal.", ephemeral: true);
                return;
            }

            if (removeAmount <= 0)
            {
                await RespondAsync("Antal skal være større end 0.", ephemeral: true);
                return;
            }

            var latestBandeBuyEvent = await _eventRepository.GetLatestBandeBuyEventAsync();
            if (latestBandeBuyEvent == null)
            {
                await RespondAsync("Der er ikke nogen bande buy event.", ephemeral: true);
                return;
            }

            // Get the order record for this user and weapon
            var orderRecord = _boughtWeaponRepository.GetWeaponOrderedByUser(weapon, user);
            if (orderRecord == null)
            {
                await RespondAsync($"Du har ikke bestilt nogle {weaponName}", ephemeral: true);
                return;
            }

            // Check if the user is trying to remove more than what is currently ordered
            if (removeAmount > orderRecord.Amount)
            {
                await RespondAsync($"Du har kun bestilt {orderRecord.Amount} {weaponName}.", ephemeral: true);
                return;
            }

            // Subtract the removal amount from the order
            orderRecord.Amount -= removeAmount;

            // If the amount goes to zero, remove the record (if that's your desired behavior)
            if (orderRecord.Amount == 0)
            {
                _boughtWeaponRepository.Delete(orderRecord);
                await RespondAsync($"Din bestilling for {weaponName} er helt fjernet.", ephemeral: true);
            }
            else
            {
                _boughtWeaponRepository.Update(orderRecord);
                await RespondAsync($"Dine {removeAmount} {weaponName} er fjernet fra bestillingen.\nDu har nu {orderRecord.Amount} {weaponName} bestilt.", ephemeral: true);
            }

            _ = _bandeBuyService.ReloadMessage();
        }

        [SlashCommand("help", "Få hjælp til bande buy.")]
        public async Task BandeBuyHelp()
        {
            var socketUser = Context.Interaction.User as SocketGuildUser;

            if (socketUser == null)
            {
                _logger.LogError("Failed to get SocketGuildUser from Context.Interaction.User.");
                await RespondAsync("Der skete en fejl. Prøv igen senere.", ephemeral: true);
                return;
            }

            var helpEmbed = new EmbedBuilder()
                .WithTitle("Bande Buy Hjælp")
                .WithDescription("Her er en liste over kommandoer til bande buy.")
                .AddField("Kommandoer", "Alle kommandoer skal starte med `/bandebuy`.", inline: false)
                .AddField("createweapon", "Opret et våben.\nKræver: SGT At Arms", inline: false)
                .AddField("buyweapon", "Køb et våben.\nKræver: Bandebuy Adgang", inline: false)
                .AddField("ispayed", "Marker at en bruger har betalt for våben.\nKræver: Admin", inline: false)
                .AddField("isdelivered", "Marker alle våben som leveret.\nKræver: Admin", inline: false)
                .AddField("removeweapon", "Slet våben fra dine våben bestillinger.\nKræver: Bandebuy Adgang", inline: false)
                .AddField("help", "Få hjælp til bande buy.", inline: false)
                .AddField("seeweapons", "Se alle våben samt. priser i DM.", inline: false)
                .WithColor(Color.Green)
                .Build();

            try
            {
                await socketUser.SendMessageAsync(embed: helpEmbed);
                _logger.LogInformation("Help message sent to user {UserId} in DM.", socketUser.Id);
                await RespondAsync("Hjælpebeskeden er sendt til din DM.", ephemeral: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send help message to user {UserId} in DM.", socketUser.Id);
                await RespondAsync("Kunne ikke sende DM. Tjek dine DM-indstillinger og prøv igen.", ephemeral: true);
            }
        }

        [SlashCommand("seeweapons", "Se alle våben samt. priser i DM")]
        public async Task SeeWeapons()
        {
            var weapons = _weaponRepository.GetAll();
            var weaponList = string.Join("\n", weapons.Select(x => $"**{x.WeaponName}** - Pris: {x.WeaponPrice.ToString("N0", new CultureInfo("de-DE"))} - Limit: {x.WeaponLimit}"));

            var socketUser = Context.Interaction.User as SocketGuildUser;

            if (socketUser == null)
            {
                _logger.LogError("Failed to get SocketGuildUser from Context.Interaction.User.");
                await RespondAsync("Der skete en fejl. Prøv igen senere.", ephemeral: true);
                return;
            }

            try
            {
                await socketUser.SendMessageAsync($"Her er en liste over alle våben:\n{weaponList}");
                _logger.LogInformation("Weapon list sent to user {UserId} in DM.", socketUser.Id);
                await RespondAsync("Våbenlisten er sendt til din DM.", ephemeral: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send weapon list to user {UserId} in DM.", socketUser.Id);
                await RespondAsync("Kunne ikke sende DM. Tjek dine DM-indstillinger og prøv igen.", ephemeral: true);
            }
        }
    }
}