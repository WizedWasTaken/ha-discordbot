using BotTemplate.BotCore.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotTemplate.BotCore.Repositories
{
    public interface IRepository<T>
    {
        void Add(T entity);

        void Update(T entity);

        void Delete(int id);

        void Delete(T entity);

        T GetById(int id);

        IEnumerable<T> GetAll();
    }

    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly DataContext _context;

        public Repository(DataContext context)
        {
            _context = context;
        }

        public virtual void Add(T entity)
        {
            _context.Add(entity);
            _context.SaveChanges();
        }

        public virtual void Delete(T entity)
        {
            _context.Remove(entity);
            _context.SaveChanges();
        }

        public virtual void Delete(int id)
        {
            T entity = GetById(id);
            Delete(entity);
        }

        public virtual IEnumerable<T> GetAll()
        {
            return _context.Set<T>().ToList();
        }

        public virtual T GetById(int id)
        {
            return _context.Find<T>(id);
        }

        public virtual void Update(T entity)
        {
            _context.Update(entity);
            _context.SaveChanges();
        }
    }
}