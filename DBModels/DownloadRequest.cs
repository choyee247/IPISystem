using System;
using System.Collections.Generic;

namespace ProjectManagementSystem.DBModels;

public partial class DownloadRequest
{
    public int DownloadRequestPkId { get; set; }

    public int ProjectFilePkId { get; set; }

    public int? StudentPkId { get; set; }

    public string? Reason { get; set; }

    public DateTime RequestDate { get; set; }

    public bool? IsApproved { get; set; }

    public DateTime? ApprovedDate { get; set; }

    public bool IsBlocked { get; set; }

    public int? ApprovedByTeacherId { get; set; }

    public string? StudentName { get; set; }

    public string? RollNumber { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Year { get; set; }

    public virtual Teacher? ApprovedByTeacher { get; set; }

    public virtual ICollection<DownloadTransaction> DownloadTransactions { get; set; } = new List<DownloadTransaction>();

    public virtual ProjectFile ProjectFilePk { get; set; } = null!;

    public virtual Student? StudentPk { get; set; }
}
