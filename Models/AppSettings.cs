namespace TaskNote.Models
{
    public class AppSettings
    {
        public string DatabasePath { get; set; } = string.Empty;
        public string TimerStartSoundPath { get; set; } = string.Empty;
        public string TimerFinishSoundPath { get; set; } = string.Empty;
        public int TimerDurationMinutes { get; set; } = 25;
        public string Theme { get; set; } = "Light";
    }
}
