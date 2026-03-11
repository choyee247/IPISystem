using System.Collections.Generic;

namespace ProjectManagementSystem.DBModels
{
    public class TeacherDashboardViewModel
    {
        public string TeacherName { get; set; }

        public int TotalStudents { get; set; }
        public int TotalProjects { get; set; }
        public int TotalCompanies { get; set; }

        public List<RecentProjectVM> RecentProjects { get; set; } = new List<RecentProjectVM>();
        public List<AssignedStudentVM> AssignedStudents { get; set; } = new List<AssignedStudentVM>();
        public List<CompanyVM> Companies { get; set; } = new();
    }

    public class RecentProjectVM
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string StudentName { get; set; }
        public int MembersCount { get; set; }   
        public string CompanyName { get; set; }

        public int Progress { get; set; }

        public string Status { get; set; }
    }
    public class AssignedStudentVM
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }
        public string RollNumber { get; set; }
        public string YearRange { get; set; }
        public string ProjectTitle { get; set; }

        public string CompanyName { get; set; }

        public string LastActive { get; set; }
    }
    public class CompanyVM
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int ProjectsCount { get; set; }

        public int StudentsCount { get; set; }
    }
}