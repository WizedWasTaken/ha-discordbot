using BotTemplate.BotCore.DataAccess;
using BotTemplate.BotCore.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BotTemplate.BotCore.Repositories
{
    public interface IEventRepository : IRepository<Event>
    {
        Event GetLatest();

        Event Get(int discordId);

        ICollection<Event> GetParticipating(ulong userId);

        ICollection<Event> GetDeclined(ulong userId);

        Event GetByTitle(string title);

        ICollection<Event> GetActive();

        Event AddWithReturn(Event entity);

        Event GetLatestBandeBuyEvent();

        Task<Event> GetLatestBandeBuyEventAsync();
    }

    public class EventRepository : Repository<Event>, IEventRepository
    {
        private readonly DataContext context;

        public EventRepository(DataContext _context) : base(_context)
        {
            context = _context;
        }

        public ICollection<Event> GetDeclined(ulong userId)
        {
            var res = context.Events.Where(x => x.Absent.Any(y => y.DiscordId == userId)).Include(x => x.MadeBy).ToList();

            if (res != null)
            {
                return res;
            }

            return null;
        }

        public Event GetLatest()
        {
            var res = context.Events.Include(x => x.MadeBy).FirstOrDefault();

            if (res != null)
            {
                return res;
            }

            return null;
        }

        public virtual Event Get(int discordId)
        {
            return context.Events
                .Include(e => e.MadeBy)
                .Include(e => e.WeaponsBought)
                    .ThenInclude(bw => bw.User)
                .Include(e => e.Participants)
                .Include(e => e.Absent)
                .Include(e => e.WeaponsBought)
                    .ThenInclude(bw => bw.Weapon)
                .OrderByDescending(e => e.EventDate)
                .FirstOrDefault(e => e.EventId == discordId);
        }

        public ICollection<Event> GetParticipating(ulong userId)
        {
            var res = context.Events.Where(x => x.Participants.Any(y => y.DiscordId == userId)).Include(x => x.MadeBy).ToList();

            if (res != null)
            {
                return res;
            }

            return null;
        }

        public ICollection<Event> GetActive()
        {
            var res = context.Events.Where(x => x.EventDate > System.DateTime.Now).Include(x => x.MadeBy).ToList();

            if (res != null)
            {
                return res;
            }

            return null;
        }

        public override ICollection<Event> GetAll()
        {
            return context.Events.Include(x => x.MadeBy).ToList();
        }

        public Event GetByTitle(string title)
        {
            var res = context.Events.Where(x => x.EventTitle == title).Include(x => x.MadeBy).FirstOrDefault();

            if (res != null)
            {
                return res;
            }

            return null;
        }

        public Event AddWithReturn(Event entity)
        {
            context.Events.Add(entity);
            context.SaveChanges();
            return context.Events.Include(e => e.MadeBy).FirstOrDefault(e => e.EventTitle == entity.EventTitle && e.EventDate == entity.EventDate);
        }

        public override Event GetById(int id)
        {
            return context.Events
                .Include(e => e.MadeBy)
                .Include(e => e.Participants)
                .Include(e => e.Absent)
                .FirstOrDefault(e => e.EventId == id);
        }

        public Event GetLatestBandeBuyEvent()
        {
            return context.Events
                .Include(e => e.MadeBy)
                .Include(e => e.WeaponsBought)
                    .ThenInclude(bw => bw.User)
                .Include(e => e.WeaponsBought)
                    .ThenInclude(bw => bw.Weapon)
                .Where(e => e.EventType == EventType.BandeBuy && e.EventDate >= DateTime.Now)
                .OrderByDescending(e => e.EventDate)
                .FirstOrDefault();
        }

        public async Task<Event> GetLatestBandeBuyEventAsync()
        {
            var res = await context.Events
                .Include(e => e.MadeBy)
                .Include(e => e.WeaponsBought)
                    .ThenInclude(wp => wp.User)
                .Include(e => e.WeaponsBought)
                    .ThenInclude(wp => wp.Weapon).Where(e => e.EventType == EventType.BandeBuy && e.EventDate >= DateTime.Now)

                .OrderByDescending(e => e.EventDate)
                .FirstOrDefaultAsync();

            return res;
        }

        public override void Delete(Event entity)
        {
            // Ensure related BoughtWeapons are loaded
            context.Entry(entity).Collection(e => e.WeaponsBought).Load();

            // Manually remove related BoughtWeapons
            var boughtWeapons = entity.WeaponsBought;
            context.BoughtWeapons.RemoveRange(boughtWeapons);
            context.SaveChanges();

            // Now remove the Event
            context.Events.Remove(entity);
            context.SaveChanges();
        }
    }
}