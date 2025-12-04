namespace Calendar.Models
{
    public class TaskItem
    {
        public Guid Id { get; set; }
        public required string Title { get; set; }
        public required DateOnly Deadline { get; set; }
        public required int Hours { get; set; }
        public double Priority { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }
}