using System;
using System.Collections.Generic;

namespace ProjectManagementSystem.DBModels;

public partial class AcademicYear
{
    public int AcademicYearPkId { get; set; }

    public string? YearRange { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();

    public virtual ICollection<TeacherStudent> TeacherStudents { get; set; } = new List<TeacherStudent>();

    public virtual ICollection<Teacher> Teachers { get; set; } = new List<Teacher>();
}
