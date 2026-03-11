using System;
using System.Collections.Generic;

namespace ProjectManagementSystem.DBModels
{
    public class AdminDashboardViewModel
    {
        // Metrics
        public int TotalTeachers { get; set; }
        public int TotalStudents { get; set; }
        public int TotalProjects { get; set; }
        public int TotalAnnouncements { get; set; }

        // Recent Items
        public List<TeacherViewModel> RecentTeachers { get; set; } = new List<TeacherViewModel>();
        public List<StudentViewModel> RecentStudents { get; set; } = new List<StudentViewModel>();
        public List<Announcement> RecentAnnouncements { get; set; } = new List<Announcement>();

        // Academic Year filter
        public List<AcademicYear> AcademicYears { get; set; } = new List<AcademicYear>();
        public int? SelectedYearPkId { get; set; } // Selected AcademicYear foreign key
    }

    // Simplified view model for teachers
    public class TeacherViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;

        public bool IsActive { get; set; }
        public int? AcademicYearPkId { get; set; }
        public string AcademicYearName => AcademicYearPk?.YearRange ?? "N/A";
        public AcademicYear? AcademicYearPk { get; set; }
    }

    public class StudentViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public int? AcademicYearPkId { get; set; }
        public string AcademicYearName => AcademicYearPk?.YearRange ?? "N/A";
        public AcademicYear? AcademicYearPk { get; set; }
        public int ProjectCount { get; set; }
    }
    // Announcement model
    public class Announcements
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime DatePosted { get; set; }
    }

    // AcademicYear model
    public class AcademicYears
    {
        public int Id { get; set; }
        public string YearName { get; set; } = string.Empty;
    }
}