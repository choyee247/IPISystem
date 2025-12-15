using System;
using System.Collections.Generic;

namespace ProjectManagementSystem.DBModels;

public partial class Project
{
    public int ProjectPkId { get; set; }

    public string? ProjectName { get; set; }

    public string? Description { get; set; }

    public int ProjectTypePkId { get; set; }

    public int LanguagePkId { get; set; }

    public int FrameworkPkId { get; set; }

    public int CompanyPkId { get; set; }

    public DateTime? ProjectSubmittedDate { get; set; }

    public bool? IsDeleted { get; set; }

    public DateTime? CreatedDate { get; set; }

    public string? CreatedBy { get; set; }

    public string SupervisorName { get; set; } = null!;

    public string? AdminComment { get; set; }

    public DateTime? ApprovedDate { get; set; }

    public DateTime? RejectedDate { get; set; }

    public string? Status { get; set; }

    public int StudentPkId { get; set; }

    public int SubmittedByStudentPkId { get; set; }

    public bool? IsApprovedByTeacher { get; set; }

    public DateTime? ScheduleTime { get; set; }

    public DateTime? MeetingTime { get; set; }

    public int? TeacherId { get; set; }

    public virtual Company CompanyPk { get; set; } = null!;

    public virtual Framework FrameworkPk { get; set; } = null!;

    public virtual Language LanguagePk { get; set; } = null!;

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<ProjectFile> ProjectFiles { get; set; } = new List<ProjectFile>();

    public virtual ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();

    public virtual ProjectType ProjectTypePk { get; set; } = null!;

    public virtual Student StudentPk { get; set; } = null!;

    public virtual Student SubmittedByStudentPk { get; set; } = null!;

    public virtual Teacher? Teacher { get; set; }
}
