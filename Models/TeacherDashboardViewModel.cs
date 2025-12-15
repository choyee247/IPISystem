using System;
using System.Collections.Generic;

namespace ProjectManagementSystem.DBModels
{
    public class TeacherDashboardViewModel
    {
        public Announcement CurrentAnnouncement { get; set; } = new Announcement();
        public List<Project> PendingProjects { get; set; } = new List<Project>();
        public List<Announcement> Announcements { get; set; } = new List<Announcement>();
        public List<StudentSubmission> RecentSubmitters { get; set; } = new List<StudentSubmission>();
        public int TotalStudents { get; set; }
        public int TotalProjects { get; set; }
        public int PendingProjectsCount { get; set; }
        public List<SubmissionStat> SubmissionStats { get; set; } = new List<SubmissionStat>();

    }

    public class StudentSubmission
    {
        public string StudentName { get; set; }
        public string ProjectName { get; set; }
        public DateTime SubmissionDate { get; set; }
    }

    public class SubmissionStat
    {
        public string Date { get; set; }
        public string DisplayDate { get; set; } // Add this line
        public int Count { get; set; }
    }
}