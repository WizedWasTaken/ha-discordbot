using BotTemplate.BotCore.DataAccess;
using BotTemplate.BotCore.Entities;
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
    }

    public class BoughtWeaponRepository : Repository<BoughtWeapon>, IBoughtWeaponRepository
    {
        private readonly DataContext context;

        public BoughtWeaponRepository(DataContext _context) : base(_context)
        {
            context = _context;
        }

        public Weapon GetWeaponByName(WeaponName weaponName)
        {
            var res = context.Weapons.Where(x => x.WeaponName == weaponName).FirstOrDefault();
            if (res != null)
            {
                return res;
            }

            return null;
        }
    }
}