using System;
using System.Collections.Generic;

namespace ProjectManagementSystem.DBModels;

public partial class ProjectFile
{
    public int ProjectFilePkId { get; set; }

    public int ProjectPkId { get; set; }

    public string? FilePath { get; set; }

    public string? FileType { get; set; }

    public long? FileSize { get; set; }

    public DateTime? UploadedAt { get; set; }

    public virtual ICollection<DownloadRequest> DownloadRequests { get; set; } = new List<DownloadRequest>();

    public virtual Project ProjectPk { get; set; } = null!;
}
