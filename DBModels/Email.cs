using System;
using System.Collections.Generic;

namespace ProjectManagementSystem.DBModels;

public partial class Email
{
    public int EmailPkId { get; set; }

    public string? EmailAddress { get; set; }

    public string? RollNumber { get; set; }

    public string? Class { get; set; }

    public bool? IsDeleted { get; set; }

    public int AcademicYearPkId { get; set; }

    public DateTimeOffset? CreatedDate { get; set; }

    public int? AssignedTeacherId { get; set; }

    public virtual Teacher? AssignedTeacher { get; set; }

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();
}
