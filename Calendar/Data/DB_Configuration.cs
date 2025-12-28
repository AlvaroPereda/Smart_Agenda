using Calendar.Models;
using Microsoft.EntityFrameworkCore;

namespace Calendar.Data
{
    public class DB_Configuration(DbContextOptions<DB_Configuration> options) : DbContext(options)
    {
        public DbSet<CalendarEvent> CalendarEvent { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
    }
}
