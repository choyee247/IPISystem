namespace ProjectManagementSystem.DBModels
{
    public class NotificationViewModel
    {
        public int Id { get; set; }
        public string Message { get; set; } = "";
        public string AnnouncementMessage { get; set; } = "";
        public string AnnouncementTitle { get; set; } = "";
        public DateTime? AnnouncementStartDate { get; set; }
        public DateTime? AnnouncementExpiryDate { get; set; }
        public string AnnouncementFilePath { get; set; } = "";
        public bool? AnnouncementBlocksSubmissions { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? ProjectId { get; set; }
        public string ProjectName { get; set; } = "";
        public bool? IsRead { get; set; }
        //public bool? IsRead { get; set; }
        public string Title { get; set; }
        public string NotificationType { get; set; }
        public string DeadlineStatus { get; set; } = "";
        
        public bool? IsDeleted { get; set; }

        // role-specific flags
        public bool IsReadByTeacher { get; set; }
        public bool IsDeletedByTeacher { get; set; }
        public bool IsReadByAdmin { get; set; }
        public bool IsDeletedByAdmin { get; set; }

        // computed property for view convenience
        public bool IsUnread(string role)
        {
            return role switch
            {
                "Teacher" => !IsReadByTeacher,
                "Admin" => !IsReadByAdmin,
                _ => false
            };
        }
    }
}
