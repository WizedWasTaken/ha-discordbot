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
    public interface IUserRepository : IRepository<User>
    {
        User GetByDiscordId(ulong discordId);
    }

    public class UserRepository : Repository<User>, IUserRepository
    {
        private readonly DataContext context;

        public UserRepository(DataContext _context) : base(_context)
        {
            context = _context;
        }

        public User GetByDiscordId(ulong discordId)
        {
            var res = context.Users.Where(x => x.DiscordId == discordId).FirstOrDefault();
            if (res != null)
            {
                return res;
            }

            return null;
        }
    }
}