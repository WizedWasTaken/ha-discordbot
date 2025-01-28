using BotTemplate.BotCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace BotTemplate.BotCore.DataAccess
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }

        // Define your DbSets here
        public DbSet<User> Users { get; set; }

        public DbSet<Strike> Strikes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Strike>()
                .HasOne(s => s.GivenTo)
                .WithMany()
                .HasForeignKey("GivenToUserId")
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Strike>()
                .HasOne(s => s.GivenBy)
                .WithMany()
                .HasForeignKey("GivenByUserId")
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}