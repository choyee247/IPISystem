using System;
using System.Collections.Generic;

namespace ProjectManagementSystem.DBModels;

public partial class TeacherCompany
{
    public int TeacherCompanyId { get; set; }

    public int TeacherId { get; set; }

    public int CompanyPkId { get; set; }

    public DateTime AssignedDate { get; set; }

    public bool IsActive { get; set; }

    public virtual Company CompanyPk { get; set; } = null!;

    public virtual Teacher Teacher { get; set; } = null!;
}
