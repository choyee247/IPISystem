using ProjectManagementSystem.DBModels;

namespace ProjectManagementSystem.Models
{
    public class TeacherDetailViewModel
    {
        public int TeacherId { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Role { get; set; } = null!;
        public string? DepartmentName { get; set; }
        public string? AcademicYear { get; set; }
        public DateTime? CreatedDate { get; set; }
        public List<Student> AssignedStudents { get; set; } = new();
        public List<StudentProjectRoleViewModel> StudentProjectRoles { get; set; } = new();
        public List<Company> Companies { get; set; } = new();
        public List<StudentCompanyViewModel> StudentCompanyAssignments { get; set; }

    }
}
