using ProjectManagementSystem.DBModels;

namespace ProjectManagementSystem.Models
{
    public class StudentCreateViewModel
    {
        public Student Student { get; set; } = new();

        public List<Teacher> SupervisorList { get; set; } = new();
    }
}
