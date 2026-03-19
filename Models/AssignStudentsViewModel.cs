using Microsoft.AspNetCore.Mvc.Rendering;

namespace ProjectManagementSystem.Models
{
    public class AssignStudentsViewModel
    {
        public int TeacherId { get; set; }               // Teacher to assign students
        public string TeacherName { get; set; } = null!; // For display in view
        public List<SelectListItem> Students { get; set; } = new(); // All students in department & year
        public List<int> SelectedStudentIds { get; set; } = new();
        public int SelectedAcademicYearId { get; set; }
        public List<SelectListItem> AcademicYears { get; set; } = new();
    }
}
