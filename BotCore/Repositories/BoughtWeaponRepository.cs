using BotTemplate.BotCore.DataAccess;
using BotTemplate.BotCore.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotTemplate.BotCore.Repositories
{
    public interface IBoughtWeaponRepository : IRepository<BoughtWeapon>
    {
        Weapon GetWeaponByName(WeaponName weaponName);

        Task<Weapon> GetByWeaponNameAsync(WeaponName weaponName);

        int GetWeaponLimitLeft(WeaponName weaponName);

        Task<int> GetWeaponLimitLeftAsync(WeaponName weaponName);

        int GetWeaponAmountOrder(WeaponName weaponName, User user);

        BoughtWeapon GetWeaponOrderedByUser(Weapon weapon, User user);

        Task<int> GetWeaponAmountOrderAsync(WeaponName weaponName, User user);

        Task<ICollection<BoughtWeapon>> GetWeaponsBoughtAsync();
    }

    public class BoughtWeaponRepository : Repository<BoughtWeapon>, IBoughtWeaponRepository
    {
        private readonly DataContext context;
        private readonly IEventRepository _eventRepository;

        public BoughtWeaponRepository(DataContext _context, IEventRepository eventRepository) : base(_context)
        {
            context = _context;
            _eventRepository = eventRepository;
        }

        public Weapon GetWeaponByName(WeaponName weaponName)
        {
            var res = context.Weapons.FirstOrDefault(x => x.WeaponName == weaponName);
            if (res != null)
            {
                return res;
            }

            throw new InvalidOperationException($"Weapon with name {weaponName} not found.");
        }

        public int GetWeaponLimitLeft(WeaponName weaponName)
        {
            var weapon = GetWeaponByName(weaponName);
            var boughtWeapons = context.BoughtWeapons.Where(x => x.Weapon == weapon).ToList();
            var events = _eventRepository.GetLatestBandeBuyEvent().WeaponsBought.ToList();
            var boughtAmount = boughtWeapons.Sum(x => x.Amount);
            var eventAmount = events.Where(x => x.Weapon == weapon).Sum(x => x.Amount);

            return weapon.WeaponLimit - boughtAmount - eventAmount;
        }

        public async Task<int> GetWeaponLimitLeftAsync(WeaponName weaponName)
        {
            var weapon = await GetByWeaponNameAsync(weaponName);
            var latestEvent = await _eventRepository.GetLatestBandeBuyEventAsync();
            var boughtWeapons = latestEvent.WeaponsBought
                .Where(x => x.Weapon.WeaponName == weaponName)
                .ToList();

            var eventWeapons = latestEvent?.WeaponsBought
                .Where(x => x.Weapon.WeaponName == weaponName)
                .ToList() ?? new List<BoughtWeapon>();

            var boughtAmount = boughtWeapons.Sum(x => x.Amount) + eventWeapons.Sum(x => x.Amount);

            return weapon.WeaponLimit - boughtAmount;
        }

        public int GetWeaponAmountOrder(WeaponName weaponName, User user)
        {
            var weapon = GetWeaponByName(weaponName);
            var boughtWeapons = context.BoughtWeapons.Where(x => x.Weapon == weapon && x.User == user).ToList();
            var events = _eventRepository.GetLatestBandeBuyEvent().WeaponsBought.ToList();
            var boughtAmount = boughtWeapons.Sum(x => x.Amount);
            var eventAmount = events.Where(x => x.Weapon == weapon && x.User == user).Sum(x => x.Amount);

            return boughtAmount + eventAmount;
        }

        public BoughtWeapon GetWeaponOrderedByUser(Weapon weapon, User user)
        {
            var latestEvent = _eventRepository.GetLatestBandeBuyEvent();

            if (latestEvent == null)
            {
                throw new InvalidOperationException("No latest event found.");
            }

            var boughtWeapon = latestEvent.WeaponsBought
                .FirstOrDefault(bw => bw.User.UserId == user.UserId && bw.Weapon.WeaponName == weapon.WeaponName);

            if (boughtWeapon == null)
            {
                return null;
            }

            return boughtWeapon;
        }

        public async Task<Weapon> GetByWeaponNameAsync(WeaponName weaponName)
        {
            var res = await context.Weapons.FirstOrDefaultAsync(x => x.WeaponName == weaponName);
            if (res != null)
            {
                return res;
            }

            throw new InvalidOperationException($"Weapon with name {weaponName} not found.");
        }

        public async Task<int> GetWeaponAmountOrderAsync(WeaponName weaponName, User user)
        {
            var weapon = await GetByWeaponNameAsync(weaponName);
            var boughtWeapons = await context.BoughtWeapons.Where(x => x.Weapon == weapon && x.User == user).ToListAsync();
            var events = _eventRepository.GetLatestBandeBuyEvent().WeaponsBought.ToList();
            var boughtAmount = boughtWeapons.Sum(x => x.Amount);
            var eventAmount = events.Where(x => x.Weapon == weapon && x.User == user).Sum(x => x.Amount);

            return boughtAmount + eventAmount;
        }

        public async Task<ICollection<BoughtWeapon>> GetWeaponsBoughtAsync()
        {
            var latestEvent = await _eventRepository.GetLatestBandeBuyEventAsync();

            if (latestEvent == null)
            {
                throw new InvalidOperationException("No latest event found.");
            }

            return latestEvent.WeaponsBought.ToList();
        }
    }
}