namespace ProjectManagementSystem.Models
{
    public class StudentWithProjectViewModel
    {
        public int StudentPkId { get; set; }
        public string StudentName { get; set; } = null!;
        public string RollNumber { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string NrcNumber { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string DepartmentName { get; set; } = null!;
        public string AcademicYear { get; set; } = null!;
        public bool HasProject { get; set; }
    }
}
