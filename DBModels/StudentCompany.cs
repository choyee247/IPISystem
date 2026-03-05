using System;
using System.Collections.Generic;

namespace ProjectManagementSystem.DBModels;

public partial class StudentCompany
{
    public int Id { get; set; }

    public int StudentId { get; set; }

    public int CompanyId { get; set; }

    public int TeacherId { get; set; }

    public bool IsActive { get; set; }

    public DateTime? CreatedDate { get; set; }

    public virtual Company Company { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;

    public virtual Teacher Teacher { get; set; } = null!;
}
