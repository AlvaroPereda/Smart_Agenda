namespace Calendar.Models
{
    public abstract class CalendarEvent
    {
        public Guid Id { get; set; }
        public required string Title { get; set; }
        public required string Category { get; set; }
    }

    public class WorkTask : CalendarEvent
    {
        public required DateOnly Deadline { get; set; }
        public required int Hours { get; set; }
        public double Priority { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }

    public class BreakTask : CalendarEvent
    {
        public TimeOnly Start { get; set; }
        public TimeOnly End { get; set; }
    }
}