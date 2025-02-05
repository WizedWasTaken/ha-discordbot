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
        public DbSet<Event> Events { get; set; }
        public DbSet<BoughtWeapon> BoughtWeapons { get; set; }
        public DbSet<Weapon> Weapons { get; set; }
        public DbSet<Note> Notes { get; set; }
        public DbSet<PaidAmount> PaidAmounts { get; set; }

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

            modelBuilder.Entity<Event>()
                .Property(e => e.EventType)
                .HasConversion<string>(); // Storing enum as string

            modelBuilder.Entity<PaidAmount>()
                .HasOne(p => p.PaidBy)
                .WithMany()
                .HasForeignKey("PaidByUserId")
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<PaidAmount>()
                .HasOne(p => p.BandeBuyEvent)
                .WithMany()
                .HasForeignKey("BandeBuyEventEventId")
                .OnDelete(DeleteBehavior.NoAction);

            base.OnModelCreating(modelBuilder);
        }
    }
}