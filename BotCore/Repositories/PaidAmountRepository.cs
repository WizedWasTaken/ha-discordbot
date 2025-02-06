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
    public interface IPaidAmountRepository : IRepository<PaidAmount>
    {
        PaidAmount GetUserPaidAmount(User user, Event ev);

        List<PaidAmount> GetEventPaidAmounts(Event ev);

        Dictionary<User, decimal> GetEventPaidAmountsDict(Event ev);

        bool CreateNewPayment(User user, Event ev, decimal amount, User paidTo);

        Task<PaidAmount> GetUserPaidAmountAsync(User user, Event ev);

        Task<List<PaidAmount>> GetEventPaidAmountsAsync(Event ev);

        Task<Dictionary<User, decimal>> GetEventPaidAmountsDictAsync(Event ev);

        Task<bool> CreateNewPaymentAsync(User user, Event ev, decimal amount, User paidTo);
    }

    public class PaidAmountRepository : Repository<PaidAmount>, IPaidAmountRepository
    {
        private readonly DataContext context;

        public PaidAmountRepository(DataContext _context) : base(_context)
        {
            context = _context;
        }

        public PaidAmount GetUserPaidAmount(User user, Event ev)
        {
            var res = context.PaidAmounts.Where(x => x.PaidBy.DiscordId == user.DiscordId && x.BandeBuyEvent.EventId == ev.EventId).FirstOrDefault();
            if (res != null)
            {
                return res;
            }

            return null;
        }

        public List<PaidAmount> GetEventPaidAmounts(Event ev)
        {
            var res = context.PaidAmounts.Where(x => x.BandeBuyEvent.EventId == ev.EventId).ToList();
            if (res != null)
            {
                return res;
            }

            return null;
        }

        public Dictionary<User, decimal> GetEventPaidAmountsDict(Event ev)
        {
            var res = context.PaidAmounts.Where(x => x.BandeBuyEvent.EventId == ev.EventId).ToDictionary(x => x.PaidBy, x => x.Amount);
            if (res != null)
            {
                return res;
            }

            return null;
        }

        public bool CreateNewPayment(User user, Event ev, decimal amount, User paidTo)
        {
            var res = context.PaidAmounts.Where(x => x.PaidBy.DiscordId == user.DiscordId && x.BandeBuyEvent.EventId == ev.EventId).FirstOrDefault();
            PaidAmount paidAmount = new PaidAmount
            {
                Amount = amount,
                PaidBy = user,
                BandeBuyEvent = ev,
                PaidTo = new List<User> { paidTo }
            };
            if (res is null)
            {
                context.PaidAmounts.Add(paidAmount);
                context.SaveChanges();
            }
            else
            {
                res.Amount = amount;
                if (res.PaidTo == null)
                {
                    res.PaidTo = new List<User>();
                }
                if (!res.PaidTo.Contains(paidTo))
                {
                    res.PaidTo.Add(paidTo);
                }

                context.SaveChanges();
            }

            return true;
        }

        public async Task<PaidAmount> GetUserPaidAmountAsync(User user, Event ev)
        {
            var res = await context.PaidAmounts.Where(x => x.PaidBy.DiscordId == user.DiscordId && x.BandeBuyEvent.EventId == ev.EventId).FirstOrDefaultAsync();
            if (res != null)
            {
                return res;
            }

            PaidAmount paidAmount = new PaidAmount
            {
                Amount = 0,
                PaidBy = user,
                BandeBuyEvent = ev
            };

            return paidAmount;
        }

        public async Task<List<PaidAmount>> GetEventPaidAmountsAsync(Event ev)
        {
            var res = await context.PaidAmounts.Where(x => x.BandeBuyEvent.EventId == ev.EventId).ToListAsync();
            if (res != null)
            {
                return res;
            }

            return null;
        }

        public async Task<Dictionary<User, decimal>> GetEventPaidAmountsDictAsync(Event ev)
        {
            var res = await context.PaidAmounts.Where(x => x.BandeBuyEvent.EventId == ev.EventId).ToDictionaryAsync(x => x.PaidBy, x => x.Amount);
            if (res != null)
            {
                return res;
            }

            return null;
        }

        public async Task<bool> CreateNewPaymentAsync(User user, Event ev, decimal amount, User paidTo)
        {
            var res = await context.PaidAmounts.Where(x => x.PaidBy.DiscordId == user.DiscordId && x.BandeBuyEvent.EventId == ev.EventId).FirstOrDefaultAsync();
            PaidAmount paidAmount = new PaidAmount
            {
                Amount = amount,
                PaidBy = user,
                BandeBuyEvent = ev,
                PaidTo = new List<User> { paidTo }
            };
            if (res is null)
            {
                context.PaidAmounts.Add(paidAmount);
                await context.SaveChangesAsync();
            }
            else
            {
                res.Amount += amount;
                if (res.PaidTo == null)
                {
                    res.PaidTo = new List<User>();
                }
                if (!res.PaidTo.Contains(paidTo))
                {
                    res.PaidTo.Add(paidTo);
                }

                await context.SaveChangesAsync();
            }

            return true;
        }
    }
}