using BotTemplate.BotCore.DataAccess;
using BotTemplate.BotCore.Entities;
using BotTemplate.BotCore.Repositories;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;

namespace BotTemplate.BotCore.Interactions.SlashCommands
{
    [Group("bandebuy", "Kommandoer til at administrere bande buy")]
    public class BandeBuyCommands : InteractionsCore
    {
        private readonly ILogger<BandeBuyCommands> _logger;
        private readonly IUserRepository _userRepository;
        private readonly IRepository<Weapon> _weaponRepository;
        private readonly IBoughtWeaponRepository _boughtWeaponRepository;

        public BandeBuyCommands(ILogger<BandeBuyCommands> logger, IUserRepository userRepository, IBoughtWeaponRepository boughtWeaponRepository, IRepository<Weapon> weaponRepository)
        {
            _logger = logger;
            _userRepository = userRepository;

            _boughtWeaponRepository = boughtWeaponRepository;
            _weaponRepository = weaponRepository;
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
        [RequireRole("Medlem")]
        public async Task BuyWeaponAsync(WeaponName weaponName, string amount)
        {
            var user = _userRepository.GetByDiscordId(Context.User.Id);
            var weapon = _boughtWeaponRepository.GetWeaponByName(weaponName);

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

            if (!int.TryParse(amount, out int weaponAmount))
            {
                await RespondAsync("Ugyldigt antal.", ephemeral: true);
                return;
            }

            // Add logic to handle the purchase of the weapon
            // For example, update the user's inventory, deduct the price, etc.

            await RespondAsync($"Du har købt {weaponAmount} {weaponName}.");
        }
    }
}