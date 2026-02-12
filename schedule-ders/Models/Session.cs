namespace schedule_ders.Models
{
    public class Session
    {
        public int SessionID { get; set; }
        public string Day { get; set; }
        public string Time { get; set; }
        public string Location { get; set; }

        // Foreign Key
        public int CourseID { get; set; }

        // Navigation Property
        public Course Course { get; set; }
    }
}
