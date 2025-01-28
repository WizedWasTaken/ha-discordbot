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
    }

    public class StrikeRepository : Repository<Strike>, IStrikeRepository
    {
        private readonly DataContext context;

        public StrikeRepository(DataContext _context) : base(_context)
        {
            context = _context;
        }

        public ICollection<Strike> GetStrikeForUser(User user)
        {
            var res = context.Strikes.Where(x => x.GivenTo.DiscordId == user.DiscordId).ToList();

            if (res != null)
            {
                return res;
            }

            return null;
        }
    }
}