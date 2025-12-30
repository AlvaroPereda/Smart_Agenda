using Calendar.Models;
using Microsoft.EntityFrameworkCore;

namespace Calendar.Data
{
    public class DB_Configuration(DbContextOptions<DB_Configuration> options) : DbContext(options)
    {
        public DbSet<CalendarEvent> CalendarEvent { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Schedule> Schedules { get; set; }

        public DbSet<WorkTask> WorkTasks { get; set; }
        public DbSet<BreakTask> BreakTasks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CalendarEvent>().UseTpcMappingStrategy(); // <--- ESTO ES LA CLAVE

            modelBuilder.Entity<WorkTask>().ToTable("WorkTasks");
            modelBuilder.Entity<BreakTask>().ToTable("BreakTasks");

            base.OnModelCreating(modelBuilder);
        }
    }
}
