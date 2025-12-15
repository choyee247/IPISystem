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

    public virtual StudentDepartment? DepartmentPk { get; set; }

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
}
