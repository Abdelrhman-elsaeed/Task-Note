namespace TaskNote.Models
{
    public class CarryOverTaskItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public bool IsSelected { get; set; } = true;
    }
}
