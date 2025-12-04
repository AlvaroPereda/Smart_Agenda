namespace Calendar.Models
{
    public class User
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Password { get; set; }
        public List<Schedule> Schedules { get; set; } = [];
        public List<TaskItem> ContainerTasks { get; set; } = [];
    }
}