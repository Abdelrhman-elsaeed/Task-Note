using System;

namespace TaskNote.Models
{
    public class TimerHistoryItem
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public int DurationSeconds { get; set; }
    }
}
