using BotTemplate.BotCore.DataAccess;
using BotTemplate.BotCore.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace BotTemplate.BotCore.Repositories
{
    public interface IStrikeRepository : IRepository<Strike>
    {
        ICollection<Strike> GetStrikeForUser(User user);

        Strike GetStrike(int id);

        ICollection<Strike> GetAllStrikes();
    }

    public class StrikeRepository : Repository<Strike>, IStrikeRepository
    {
        private readonly DataContext context;

        public StrikeRepository(DataContext _context) : base(_context)
        {
            context = _context;
        }

        public ICollection<Strike> GetAllStrikes()
        {
            var res = context.Strikes.Include(x => x.GivenTo).Include(x => x.GivenBy).ToList();

            if (res != null)
            {
                return res;
            }

            return null;
        }

        public Strike GetStrike(int id)
        {
            var res = context.Strikes.Where(x => x.StrikeId == id).Include(x => x.GivenTo).Include(x => x.GivenBy).FirstOrDefault();

            if (res != null)
            {
                return res;
            }

            return null;
        }

        public ICollection<Strike> GetStrikeForUser(User user)
        {
            var res = context.Strikes.Where(x => x.GivenTo.DiscordId == user.DiscordId).Include(x => x.GivenBy).ToList();

            if (res != null)
            {
                return res;
            }

            return null;
        }
    }
}