namespace ProjectManagementSystem.Models
{
    public class StudentProjectRoleViewModel
    {
        public int StudentPkId { get; set; }
        public string StudentName { get; set; }

        public int ProjectPkId { get; set; }
        public string ProjectName { get; set; }

        public string Role { get; set; }
    }
}
