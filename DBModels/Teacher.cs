using System;
using System.Collections.Generic;

namespace ProjectManagementSystem.DBModels;

public partial class Teacher
{
    public int Id { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public DateTime? CreatedDate { get; set; }

    public string Role { get; set; } = null!;

    public int? DepartmentPkId { get; set; }

    public int? AcademicYearPkId { get; set; }

    public virtual AcademicYear? AcademicYearPk { get; set; }

    public virtual ICollection<Company> Companies { get; set; } = new List<Company>();

    public virtual StudentDepartment? DepartmentPk { get; set; }

    public virtual ICollection<DownloadRequest> DownloadRequests { get; set; } = new List<DownloadRequest>();

    public virtual ICollection<Email> Emails { get; set; } = new List<Email>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();

    public virtual ICollection<StudentCompany> StudentCompanies { get; set; } = new List<StudentCompany>();

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();

    public virtual ICollection<TeacherCompany> TeacherCompanies { get; set; } = new List<TeacherCompany>();

    public virtual ICollection<TeacherStudent> TeacherStudents { get; set; } = new List<TeacherStudent>();
}
