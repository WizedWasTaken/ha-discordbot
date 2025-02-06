using BotTemplate.BotCore.DataAccess;
using BotTemplate.BotCore.Entities;
using BotTemplate.BotCore.Repositories;
using BotTemplate.BotCore.Services;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using System;
using System.Globalization;
using static System.Formats.Asn1.AsnWriter;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
        private readonly IPaidAmountRepository _paidAmountRepository;

        public BandeBuyCommands(ILogger<BandeBuyCommands> logger, IUserRepository userRepository, IBoughtWeaponRepository boughtWeaponRepository, IRepository<Weapon> weaponRepository, IEventRepository eventRepository, BandeBuyService bandeBuyService, IPaidAmountRepository paidAmountRepository)
        {
            _logger = logger;
            _userRepository = userRepository;
            _boughtWeaponRepository = boughtWeaponRepository;
            _weaponRepository = weaponRepository;
            _eventRepository = eventRepository;
            _bandeBuyService = bandeBuyService;
            _paidAmountRepository = paidAmountRepository;
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

            if (latestBandeBuyEvent.EventStatus == EventStatus.Closed)
            {
                await FollowupAsync("Bande buy bestillingerne er lukket.", ephemeral: true);
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
        }

        [SlashCommand("removeweaponadmin", "Slet våben fra dine våben bestillinger.")]
        [RequireRole("SGT At Arms")]
        public async Task DeleteWeaponOrderAdmin(WeaponName weaponName, string amount, string discordId, string reason)
        {
            var originalUser = await _userRepository.GetByDiscordIdAsync(Context.User.Id);
            var user = await _userRepository.GetByDiscordIdAsync(ulong.Parse(discordId));
            var weapon = await _boughtWeaponRepository.GetByWeaponNameAsync(weaponName);

            if (user == null)
            {
                await RespondAsync("Brugeren er ikke registreret.", ephemeral: true);
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
                await RespondAsync($"{user.IngameName} har ikke bestilt nogle {weaponName}", ephemeral: true);
                return;
            }

            // Check if the user is trying to remove more than what is currently ordered
            if (removeAmount > orderRecord.Amount)
            {
                await RespondAsync($"${user.IngameName} har kun bestilt {orderRecord.Amount} {weaponName}.", ephemeral: true);
                return;
            }

            // Subtract the removal amount from the order
            orderRecord.Amount -= removeAmount;

            // If the amount goes to zero, remove the record (if that's your desired behavior)
            if (orderRecord.Amount == 0)
            {
                _boughtWeaponRepository.Delete(orderRecord);
                await RespondAsync($"${user.IngameName}'s bestilling for {weaponName} er helt fjernet.", ephemeral: true);
            }
            else
            {
                _boughtWeaponRepository.Update(orderRecord);
                await RespondAsync($"{user.IngameName}'s {removeAmount} {weaponName} er fjernet fra bestillingen.\nDu har nu {orderRecord.Amount} {weaponName} bestilt.", ephemeral: true);
            }

            if (reason == null)
            {
                reason = "Ingen grund angivet.";
            }

            // Send a DM to the user
            var socketUser = Context.Guild.GetUser(user.DiscordId);
            if (socketUser != null)
            {
                try
                {
                    var embed = new EmbedBuilder()
                        .WithTitle("Våbenbestilling Fjernet")
                        .WithDescription($"Din bestilling for {removeAmount} {weaponName} er blevet fjernet af {originalUser.IngameName}.")
                        .AddField("Grund", reason, inline: false)
                        .WithColor(Color.Red)
                        .Build();

                    await socketUser.SendMessageAsync(embed: embed);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send DM to user {UserId}.", socketUser.Id);
                }
            }
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

            if (latestBandeBuyEvent.EventStatus == EventStatus.Closed)
            {
                await RespondAsync("Bande buy bestillingerne er lukket.", ephemeral: true);
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
                .AddField("registerpayment", "Register en betaling.\nKræver: Ledelsen", inline: false)
                .AddField("getpaidamount", "Se hvor meget en bruger har betalt.\nKræver: Ledelsen", inline: false)
                .AddField("close", "Luk bande buy bestillinger.\nKræver: Ledelsen", inline: false)
                .AddField("getuserweapons", "Find brugerens våben i et bestemt bande buy.\nKræver: Ledelsen", inline: false)
                .AddField("geteventpaidamounts", "Se alle betalinger for en bande buy event.\nKræver: Ledelsen", inline: false)
                .AddField("removeweaponadmin", "Slet våben fra dine våben bestillinger.\nKræver: SGT At Arms", inline: false)
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

        [SlashCommand("registerpayment", "Register en betaling.")]
        [RequireRole("Ledelsen")]
        public async Task RegisterPayment(string discordId, int amount)
        {
            var user = await _userRepository.GetByDiscordIdAsync(ulong.Parse(discordId));
            var paidTo = await _userRepository.GetByDiscordIdAsync(Context.User.Id);
            var latestBandeBuyEvent = await _eventRepository.GetLatestBandeBuyEventAsync();

            if (latestBandeBuyEvent == null)
            {
                var errorEmbed = new EmbedBuilder()
                    .WithTitle("Fejl")
                    .WithDescription("Der er ikke nogen bande buy event.")
                    .WithColor(Color.Red)
                    .Build();
                await RespondAsync(embed: errorEmbed, ephemeral: true);
                return;
            }

            if (user == null)
            {
                var errorEmbed = new EmbedBuilder()
                    .WithTitle("Fejl")
                    .WithDescription("Brugeren blev ikke fundet.")
                    .WithColor(Color.Red)
                    .Build();
                await RespondAsync(embed: errorEmbed, ephemeral: true);
                return;
            }

            await _paidAmountRepository.CreateNewPaymentAsync(user, latestBandeBuyEvent, amount, paidTo);

            var successEmbed = new EmbedBuilder()
                .WithTitle("Betaling Registreret")
                .WithDescription($"Betalingen på {amount} kr. er blevet registreret for {user.IngameName}.")
                .WithColor(Color.Green)
                .Build();
            await RespondAsync(embed: successEmbed, ephemeral: true);
        }

        [SlashCommand("getpaidamount", "Se hvor meget en bruger har betalt.")]
        [RequireRole("Ledelsen")]
        public async Task GetPaidAmount(string discordId)
        {
            var user = await _userRepository.GetByDiscordIdAsync(ulong.Parse(discordId));
            var latestBandeBuyEvent = await _eventRepository.GetLatestBandeBuyEventAsync();

            if (latestBandeBuyEvent == null)
            {
                var errorEmbed = new EmbedBuilder()
                    .WithTitle("Fejl")
                    .WithDescription("Der er ikke nogen bande buy event.")
                    .WithColor(Color.Red)
                    .Build();
                await RespondAsync(embed: errorEmbed, ephemeral: true);
                return;
            }

            if (user == null)
            {
                var errorEmbed = new EmbedBuilder()
                    .WithTitle("Fejl")
                    .WithDescription("Brugeren blev ikke fundet.")
                    .WithColor(Color.Red)
                    .Build();
                await RespondAsync(embed: errorEmbed, ephemeral: true);
                return;
            }

            var paidAmount = await _paidAmountRepository.GetUserPaidAmountAsync(user, latestBandeBuyEvent);

            if (paidAmount == null)
            {
                var infoEmbed = new EmbedBuilder()
                    .WithTitle("Ingen Betaling")
                    .WithDescription($"{user.IngameName} har ikke betalt noget.")
                    .WithColor(Color.Orange)
                    .Build();
                await RespondAsync(embed: infoEmbed, ephemeral: true);
                return;
            }

            var successEmbed = new EmbedBuilder()
                .WithTitle("Betalingsoplysninger")
                .WithDescription($"{user.IngameName} har betalt {paidAmount.Amount} kr.")
                .WithColor(Color.Green)
                .Build();
            await RespondAsync(embed: successEmbed, ephemeral: true);
        }

        [SlashCommand("close", "Luk bande buy bestillinger.")]
        [RequireRole("Ledelsen")]
        public async Task CloseBandeBuy(int eventId, string closedDateString = null)
        {
            await DeferAsync();
            var latestBandeBuyEvent = _eventRepository.Get(eventId);

            if (latestBandeBuyEvent == null)
            {
                var errorEmbed = new EmbedBuilder()
                    .WithTitle("Fejl")
                    .WithDescription("Der er ikke nogen bande buy event.")
                    .WithColor(Color.Red)
                    .Build();
                await RespondAsync(embed: errorEmbed, ephemeral: true);
                return;
            }

            latestBandeBuyEvent.EventStatus = EventStatus.Closed;
            _eventRepository.Update(latestBandeBuyEvent);

            DateTime closedDate;
            if (string.IsNullOrEmpty(closedDateString) || !DateTime.TryParse(closedDateString, out closedDate))
            {
                closedDate = DateTime.UtcNow;
            }

            var deliveryDate = closedDate.AddHours(96);

            var topBuyer = latestBandeBuyEvent.WeaponsBought
                .GroupBy(x => x.User)
                .Select(g => new
                {
                    UserName = g.Key.IngameName,
                    TotalSpent = g.Sum(x => x.Weapon.WeaponPrice * x.Amount)
                })
                .OrderByDescending(x => x.TotalSpent)
                .FirstOrDefault();

            var weaponAmounts = string.Join("\n", latestBandeBuyEvent.WeaponsBought
                .GroupBy(x => x.Weapon.WeaponName)
                .Select(g => $"{g.Key}: {g.Sum(x => x.Amount).ToString("N0", new CultureInfo("de-DE"))}"));

            var totalPriceList = string.Join("\n", latestBandeBuyEvent.WeaponsBought
                .GroupBy(x => x.Weapon.WeaponName)
                .Select(g => $"{g.Key}: {g.Sum(x => x.Weapon.WeaponPrice * x.Amount).ToString("N0", new CultureInfo("de-DE"))} DKK"));

            var closedBy = await _userRepository.GetByDiscordIdAsync(Context.User.Id);

            var globalEmbed = new EmbedBuilder()
                .WithTitle("Bande Buy Lukket")
                .WithDescription("Bande buy bestillingerne er nu lukket.")
                .AddField("Event Titel", latestBandeBuyEvent.EventTitle, inline: true)
                .AddField("Lukket Dato", $"<t:{new DateTimeOffset(closedDate).ToUnixTimeSeconds()}:F>", inline: true)
                .AddField("Leverings Dato", $"<t:{new DateTimeOffset(deliveryDate).ToUnixTimeSeconds()}:R>", inline: true)
                .AddField("Våben", weaponAmounts, inline: true)
                .AddField("Total Pris", totalPriceList, inline: true)
                .AddField("Top Køber", $"{topBuyer.UserName} - {topBuyer.TotalSpent.ToString("N0", new CultureInfo("de-DE"))} DKK", inline: false)
                .AddField("Lukket af", closedBy.IngameName, inline: false)
                .WithColor(Color.Red)
                .Build();

            var channel = Context.Guild.GetTextChannel(1337043225990397982);
            await channel.SendMessageAsync(embed: globalEmbed);

            // Send a DM to all users who have ordered weapons with a list of the weapons they have ordered and price.
            var weaponsBought = latestBandeBuyEvent.WeaponsBought.GroupBy(x => x.User);

            foreach (var group in weaponsBought)
            {
                var user = group.Key;
                var weaponList = string.Join("\n", group.Select(x => $"**Våben:** {x.Weapon.WeaponName} | **Antal:** {x.Amount} | **Pris:** {(x.Weapon.WeaponPrice * x.Amount).ToString("N0", new CultureInfo("de-DE"))} DKK"));
                var totalPrice = group.Sum(x => x.Weapon.WeaponPrice * x.Amount);
                var totalPriceFormatted = totalPrice.ToString("N0", new CultureInfo("de-DE"));

                var dmEmbed = new EmbedBuilder()
                    .WithTitle("Dine Bande Buy Bestillinger")
                    .WithDescription($"Her er en liste over de våben, du har bestilt:\n\n{weaponList}\n\n**Total pris:** {totalPriceFormatted} DKK")
                    .WithColor(Color.Red)
                    .Build();

                try
                {
                    var userAccount = await _userRepository.GetByDiscordIdAsync(user.DiscordId);
                    if (userAccount != null)
                    {
                        var socketUser = Context.Client.GetUser(userAccount.DiscordId);
                        if (socketUser != null)
                        {
                            await socketUser.SendMessageAsync(embed: dmEmbed);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send DM to user {UserId}", user.DiscordId);
                }
            }

            await FollowupAsync("Bande buy bestillingerne er nu lukket.", ephemeral: true);
        }

        [SlashCommand("getuserweapons", "Find brugerens våben i et bestemt bande buy")]
        [RequireRole("Ledelsen")]
        public async Task GetUserWeapons(string discordId, int eventId)
        {
            var user = await _userRepository.GetByDiscordIdAsync(ulong.Parse(discordId));
            var bandeBuyEvent = _eventRepository.Get(eventId);

            if (user == null)
            {
                var errorEmbed = new EmbedBuilder()
                    .WithTitle("Fejl")
                    .WithDescription("Brugeren blev ikke fundet.")
                    .WithColor(Color.Red)
                    .Build();
                await RespondAsync(embed: errorEmbed, ephemeral: true);
                return;
            }

            if (bandeBuyEvent == null)
            {
                var errorEmbed = new EmbedBuilder()
                    .WithTitle("Fejl")
                    .WithDescription("Bande buy eventet blev ikke fundet.")
                    .WithColor(Color.Red)
                    .Build();
                await RespondAsync(embed: errorEmbed, ephemeral: true);
                return;
            }

            var weaponsBought = bandeBuyEvent.WeaponsBought.Where(x => x.User == user).ToList();

            if (weaponsBought.Count == 0)
            {
                var infoEmbed = new EmbedBuilder()
                    .WithTitle("Ingen Våben")
                    .WithDescription($"{user.IngameName} har ikke bestilt nogle våben.")
                    .WithColor(Color.Orange)
                    .Build();
                await RespondAsync(embed: infoEmbed, ephemeral: true);
                return;
            }

            var weaponList = string.Join("\n", weaponsBought.Select(x => $"**Våben:** {x.Weapon.WeaponName} | **Antal:** {x.Amount} | **Pris:** {(x.Weapon.WeaponPrice * x.Amount).ToString("N0", new CultureInfo("de-DE"))} DKK"));
            var discordUser = Context.Guild.GetUser(user.DiscordId);

            var successEmbed = new EmbedBuilder()
                .WithTitle("Brugerens Våben")
                .WithDescription($"Her er en liste over de våben, {user.IngameName} har bestilt:\n\n{weaponList}")
                .AddField("Total Pris", weaponsBought.Sum(x => x.Weapon.WeaponPrice * x.Amount).ToString("N0", new CultureInfo("de-DE")) + " DKK", inline: false)
                .WithColor(Color.Green)
                .WithAuthor(user.IngameName, discordUser.GetAvatarUrl())
                .Build();
            await RespondAsync(embed: successEmbed, ephemeral: true);
        }

        [SlashCommand("geteventpaidamounts", "Se alle betalinger for en bande buy event.")]
        [RequireRole("Ledelsen")]
        public async Task GetEventPaidAmounts(string eventTitle)
        {
            var bandeBuyEvent = _eventRepository.GetByTitle(eventTitle);

            if (bandeBuyEvent == null)
            {
                bandeBuyEvent = await _eventRepository.GetLatestBandeBuyEventAsync();
            }

            if (bandeBuyEvent == null)
            {
                var errorEmbed = new EmbedBuilder()
                    .WithTitle("Fejl")
                    .WithDescription("Der er ikke nogen bande buy event.")
                    .WithColor(Color.Red)
                    .Build();
                await RespondAsync(embed: errorEmbed, ephemeral: true);
                return;
            }

            var paidAmounts = await _paidAmountRepository.GetEventPaidAmountsAsync(bandeBuyEvent);

            if (paidAmounts == null)
            {
                var infoEmbed = new EmbedBuilder()
                    .WithTitle("Ingen Betalinger")
                    .WithDescription("Der er ikke nogen betalinger.")
                    .WithColor(Color.Orange)
                    .Build();
                await RespondAsync(embed: infoEmbed, ephemeral: true);
                return;
            }

            var paidAmountsDict = await _paidAmountRepository.GetEventPaidAmountsDictAsync(bandeBuyEvent);
            var paidAmountsList = string.Join("\n", paidAmountsDict.Select(x => $"{x.Key.IngameName} - {x.Value} kr."));

            var successEmbed = new EmbedBuilder()
                .WithTitle("Betalingsoplysninger")
                .WithDescription($"Her er en liste over alle betalinger:\n{paidAmountsList}")
                .WithColor(Color.Green)
                .Build();
            await RespondAsync(embed: successEmbed, ephemeral: true);
        }
    }
}