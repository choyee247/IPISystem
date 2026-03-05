using System;
using System.Collections.Generic;

namespace ProjectManagementSystem.DBModels;

public partial class DownloadTransaction
{
    public int DownloadTransactionPkId { get; set; }

    public int DownloadRequestPkId { get; set; }

    public DateTime DownloadDate { get; set; }

    public string? IpAddress { get; set; }

    public virtual DownloadRequest DownloadRequestPk { get; set; } = null!;
}
