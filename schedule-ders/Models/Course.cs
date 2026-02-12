using System.Diagnostics.Contracts;

namespace schedule_ders.Models
{
    public class Course
    {
        public int CourseID { get; set; }   
        public string CourseName { get; set; }
        public string CourseSection { get; set; }
        public string CourseMeetingTimes { get; set; } 
        public string CourseProfessor { get; set; }
        public string CourseLeader { get; set; }
        public string OfficeHours { get; set; }
        public List<Session> Sessions { get; set; }


    }
}
