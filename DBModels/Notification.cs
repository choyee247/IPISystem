using System;
using System.Collections.Generic;

namespace ProjectManagementSystem.DBModels;

public partial class Notification
{
    public int NotificationPkId { get; set; }

    public int UserId { get; set; }

    public string? Title { get; set; }

    public string? Message { get; set; }

    public bool? IsRead { get; set; }

    public string? NotificationType { get; set; }

    public bool? IsDeleted { get; set; }

    public DateTime? DeletedDate { get; set; }

    public int? ProjectPkId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int? AnnouncementId { get; set; }

    public int? TeacherId { get; set; }

    public virtual Announcement? Announcement { get; set; }

    public virtual Project? ProjectPk { get; set; }

    public virtual Teacher? Teacher { get; set; }

    public virtual Student User { get; set; } = null!;
}
