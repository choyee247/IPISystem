using System;
using System.Collections.Generic;

namespace ProjectManagementSystem.DBModels;

public partial class Company
{
    public int CompanyPkId { get; set; }

    public string? CompanyName { get; set; }

    public string? Incharge { get; set; }

    public string? Address { get; set; }

    public string? Contact { get; set; }

    public string? Description { get; set; }

    public int? CityPkId { get; set; }

    public DateTime? CreatedDate { get; set; }

    public string? ImageFileName { get; set; }

    public int? TeacherId { get; set; }

    public virtual City? CityPk { get; set; }

    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();

    public virtual ICollection<StudentCompany> StudentCompanies { get; set; } = new List<StudentCompany>();

    public virtual Teacher? Teacher { get; set; }

    public virtual ICollection<TeacherCompany> TeacherCompanies { get; set; } = new List<TeacherCompany>();
}
