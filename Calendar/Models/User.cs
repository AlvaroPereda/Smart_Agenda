namespace Calendar.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Password { get; set; }
        public Schedule Schedule { get; set; } = new Schedule();
        public List<CalendarEvent> ContainerTasks { get; set; } = [];
    }
}