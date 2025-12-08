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
}
