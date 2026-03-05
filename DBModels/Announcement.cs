using System;
using System.Collections.Generic;

namespace ProjectManagementSystem.DBModels;

public partial class Announcement
{
    public int AnnouncementId { get; set; }

    public string? Title { get; set; }

    public string? Message { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public bool? BlocksSubmissions { get; set; }

    public string? FilePath { get; set; }

    public bool? IsActive { get; set; }

    public bool IsDeleted { get; set; }

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
