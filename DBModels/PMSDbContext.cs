using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ProjectManagementSystem.DBModels;

public partial class PMSDbContext : DbContext
{
    public PMSDbContext()
    {
    }

    public PMSDbContext(DbContextOptions<PMSDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AcademicYear> AcademicYears { get; set; }

    public virtual DbSet<Announcement> Announcements { get; set; }

    public virtual DbSet<City> Cities { get; set; }

    public virtual DbSet<Company> Companies { get; set; }

    public virtual DbSet<DownloadRequest> DownloadRequests { get; set; }

    public virtual DbSet<DownloadTransaction> DownloadTransactions { get; set; }

    public virtual DbSet<Email> Emails { get; set; }

    public virtual DbSet<Framework> Frameworks { get; set; }

    public virtual DbSet<Language> Languages { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Nrctownship> Nrctownships { get; set; }

    public virtual DbSet<Nrctype> Nrctypes { get; set; }

    public virtual DbSet<Otp> Otps { get; set; }

    public virtual DbSet<Project> Projects { get; set; }

    public virtual DbSet<ProjectFile> ProjectFiles { get; set; }

    public virtual DbSet<ProjectMember> ProjectMembers { get; set; }

    public virtual DbSet<ProjectType> ProjectTypes { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<StudentCompany> StudentCompanies { get; set; }

    public virtual DbSet<StudentDepartment> StudentDepartments { get; set; }

    public virtual DbSet<Teacher> Teachers { get; set; }

    public virtual DbSet<TeacherCompany> TeacherCompanies { get; set; }

    public virtual DbSet<TeacherStudent> TeacherStudents { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=203.81.89.218; Database=InternPMS; User Id=internadmin; Password=intern@dmin123;Trust Server Certificate=true");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AcademicYear>(entity =>
        {
            entity.HasKey(e => e.AcademicYearPkId);

            entity.Property(e => e.AcademicYearPkId).HasColumnName("AcademicYear_pkId");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<Announcement>(entity =>
        {
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.FilePath).HasMaxLength(255);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Message).HasMaxLength(1000);
            entity.Property(e => e.StartDate).HasColumnType("datetime");
            entity.Property(e => e.Title).HasMaxLength(200);
        });

        modelBuilder.Entity<City>(entity =>
        {
            entity.HasKey(e => e.CityPkId);

            entity.Property(e => e.CityPkId).HasColumnName("City_pkId");
            entity.Property(e => e.CityName).HasMaxLength(100);
            entity.Property(e => e.ImageFileName).HasMaxLength(200);
        });

        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasKey(e => e.CompanyPkId);

            entity.Property(e => e.CompanyPkId).HasColumnName("Company_pkId");
            entity.Property(e => e.Address).HasMaxLength(200);
            entity.Property(e => e.CityPkId).HasColumnName("City_pkId");
            entity.Property(e => e.CompanyName).HasMaxLength(100);
            entity.Property(e => e.Contact).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.ImageFileName).HasMaxLength(200);
            entity.Property(e => e.Incharge).HasMaxLength(100);

            entity.HasOne(d => d.CityPk).WithMany(p => p.Companies).HasForeignKey(d => d.CityPkId);

            entity.HasOne(d => d.Teacher).WithMany(p => p.Companies)
                .HasForeignKey(d => d.TeacherId)
                .HasConstraintName("FK_Companies_Teachers");
        });

        modelBuilder.Entity<DownloadRequest>(entity =>
        {
            entity.HasKey(e => e.DownloadRequestPkId).HasName("PK__Download__317AD5A92E146706");

            entity.Property(e => e.ApprovedDate).HasColumnType("datetime");
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.RequestDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.RollNumber).HasMaxLength(50);
            entity.Property(e => e.StudentName).HasMaxLength(100);
            entity.Property(e => e.Year).HasMaxLength(50);

            entity.HasOne(d => d.ApprovedByTeacher).WithMany(p => p.DownloadRequests)
                .HasForeignKey(d => d.ApprovedByTeacherId)
                .HasConstraintName("FK_DownloadRequests_Teachers");

            entity.HasOne(d => d.ProjectFilePk).WithMany(p => p.DownloadRequests)
                .HasForeignKey(d => d.ProjectFilePkId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DownloadRequests_ProjectFiles");

            entity.HasOne(d => d.StudentPk).WithMany(p => p.DownloadRequests)
                .HasForeignKey(d => d.StudentPkId)
                .HasConstraintName("FK_DownloadRequests_Students");
        });

        modelBuilder.Entity<DownloadTransaction>(entity =>
        {
            entity.HasKey(e => e.DownloadTransactionPkId).HasName("PK__Download__3E53D7C48FCD763A");

            entity.Property(e => e.DownloadDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IpAddress).HasMaxLength(50);

            entity.HasOne(d => d.DownloadRequestPk).WithMany(p => p.DownloadTransactions)
                .HasForeignKey(d => d.DownloadRequestPkId)
                .HasConstraintName("FK_DownloadTransactions_DownloadRequests");
        });

        modelBuilder.Entity<Email>(entity =>
        {
            entity.HasKey(e => e.EmailPkId);

            entity.Property(e => e.EmailPkId).HasColumnName("Email_PkId");
            entity.Property(e => e.AcademicYearPkId).HasColumnName("AcademicYear_pkId");
            entity.Property(e => e.Class).HasMaxLength(50);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(sysdatetimeoffset())");
            entity.Property(e => e.EmailAddress).HasMaxLength(50);
            entity.Property(e => e.RollNumber).HasMaxLength(50);

            entity.HasOne(d => d.AssignedTeacher).WithMany(p => p.Emails).HasForeignKey(d => d.AssignedTeacherId);
        });

        modelBuilder.Entity<Framework>(entity =>
        {
            entity.HasKey(e => e.FrameworkPkId);

            entity.Property(e => e.FrameworkPkId).HasColumnName("Framework_pkId");
            entity.Property(e => e.FrameworkName).HasMaxLength(50);
            entity.Property(e => e.LanguagePkId).HasColumnName("Language_pkId");

            entity.HasOne(d => d.LanguagePk).WithMany(p => p.Frameworks)
                .HasForeignKey(d => d.LanguagePkId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Language>(entity =>
        {
            entity.HasKey(e => e.LanguagePkId);

            entity.Property(e => e.LanguagePkId).HasColumnName("Language_pkId");
            entity.Property(e => e.ProjectTypePkId).HasColumnName("ProjectType_pkId");

            entity.HasOne(d => d.ProjectTypePk).WithMany(p => p.Languages)
                .HasForeignKey(d => d.ProjectTypePkId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Languages_ProjectTypes");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationPkId);

            entity.Property(e => e.NotificationPkId).HasColumnName("Notification_pkId");
            entity.Property(e => e.CreatedAt).HasDefaultValue(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
            entity.Property(e => e.NotificationType).HasMaxLength(50);
            entity.Property(e => e.ProjectPkId).HasColumnName("Project_pkId");
            entity.Property(e => e.Title).HasMaxLength(100);

            entity.HasOne(d => d.Announcement).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.AnnouncementId)
                .HasConstraintName("FK_Notifications_Announcements");

            entity.HasOne(d => d.ProjectPk).WithMany(p => p.Notifications).HasForeignKey(d => d.ProjectPkId);

            entity.HasOne(d => d.Teacher).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.TeacherId)
                .HasConstraintName("FK_Notifications_Teachers");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<Nrctownship>(entity =>
        {
            entity.HasKey(e => e.NrcPkId);

            entity.ToTable("NRCTownships");

            entity.Property(e => e.NrcPkId).HasColumnName("NRC_pkId");
            entity.Property(e => e.RegionCodeE).HasColumnName("RegionCode_E");
            entity.Property(e => e.RegionCodeM).HasColumnName("RegionCode_M");
            entity.Property(e => e.TownshipCodeE).HasColumnName("TownshipCode_E");
            entity.Property(e => e.TownshipCodeM).HasColumnName("TownshipCode_M");
        });

        modelBuilder.Entity<Nrctype>(entity =>
        {
            entity.HasKey(e => e.NrctypePkId);

            entity.ToTable("NRCTypes");

            entity.Property(e => e.NrctypePkId).HasColumnName("NRCType_pkId");
            entity.Property(e => e.TypeCode).HasMaxLength(5);
            entity.Property(e => e.TypeDescription).HasMaxLength(50);
        });

        modelBuilder.Entity<Otp>(entity =>
        {
            entity.HasKey(e => e.OtpPkId);

            entity.ToTable("OTPs");

            entity.Property(e => e.OtpPkId).HasColumnName("OTP_PkId");
            entity.Property(e => e.ExpiryTime)
                .HasDefaultValueSql("(dateadd(minute,(5),getdate()))")
                .HasColumnType("datetime");
            entity.Property(e => e.Otpcode).HasColumnName("OTPCode");
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.ProjectPkId);

            entity.Property(e => e.ProjectPkId).HasColumnName("Project_pkId");
            entity.Property(e => e.AcademicYearPkId).HasColumnName("AcademicYear_pkId");
            entity.Property(e => e.AdminComment).HasMaxLength(500);
            entity.Property(e => e.CompanyPkId).HasColumnName("Company_pkId");
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.FrameworkPkId).HasColumnName("Framework_pkId");
            entity.Property(e => e.IsApprovedByTeacher).HasDefaultValue(false);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.LanguagePkId).HasColumnName("Language_pkId");
            entity.Property(e => e.MeetingTime).HasColumnType("datetime");
            entity.Property(e => e.ProjectName).HasMaxLength(200);
            entity.Property(e => e.ProjectTypePkId).HasColumnName("ProjectType_pkId");
            entity.Property(e => e.ScheduleTime).HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("");
            entity.Property(e => e.StudentPkId).HasColumnName("Student_pkId");
            entity.Property(e => e.SubmittedByStudentPkId).HasColumnName("SubmittedByStudent_pkId");
            entity.Property(e => e.SupervisorName).HasMaxLength(50);

            entity.HasOne(d => d.AcademicYearPk).WithMany(p => p.Projects)
                .HasForeignKey(d => d.AcademicYearPkId)
                .HasConstraintName("FK_Projects_AcademicYears");

            entity.HasOne(d => d.CompanyPk).WithMany(p => p.Projects)
                .HasForeignKey(d => d.CompanyPkId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.FrameworkPk).WithMany(p => p.Projects)
                .HasForeignKey(d => d.FrameworkPkId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.LanguagePk).WithMany(p => p.Projects)
                .HasForeignKey(d => d.LanguagePkId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.ProjectTypePk).WithMany(p => p.Projects)
                .HasForeignKey(d => d.ProjectTypePkId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.StudentPk).WithMany(p => p.ProjectStudentPks).HasForeignKey(d => d.StudentPkId);

            entity.HasOne(d => d.SubmittedByStudentPk).WithMany(p => p.ProjectSubmittedByStudentPks)
                .HasForeignKey(d => d.SubmittedByStudentPkId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Teacher).WithMany(p => p.Projects)
                .HasForeignKey(d => d.TeacherId)
                .HasConstraintName("FK_Projects_Teachers");
        });

        modelBuilder.Entity<ProjectFile>(entity =>
        {
            entity.HasKey(e => e.ProjectFilePkId);

            entity.Property(e => e.ProjectFilePkId).HasColumnName("ProjectFile_pkId");
            entity.Property(e => e.FilePath).HasMaxLength(500);
            entity.Property(e => e.FileType).HasMaxLength(150);
            entity.Property(e => e.ProjectPkId).HasColumnName("Project_pkId");

            entity.HasOne(d => d.ProjectPk).WithMany(p => p.ProjectFiles).HasForeignKey(d => d.ProjectPkId);
        });

        modelBuilder.Entity<ProjectMember>(entity =>
        {
            entity.HasKey(e => e.ProjectMemberPkId);

            entity.Property(e => e.ProjectMemberPkId).HasColumnName("ProjectMember_pkId");
            entity.Property(e => e.ProjectPkId).HasColumnName("Project_pkId");
            entity.Property(e => e.RemovedDate).HasColumnType("datetime");
            entity.Property(e => e.RemovedReason).HasMaxLength(500);
            entity.Property(e => e.Role).HasMaxLength(150);
            entity.Property(e => e.RoleDescription).HasMaxLength(100);
            entity.Property(e => e.StudentPkId).HasColumnName("Student_pkId");

            entity.HasOne(d => d.ProjectPk).WithMany(p => p.ProjectMembers)
                .HasForeignKey(d => d.ProjectPkId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.StudentPk).WithMany(p => p.ProjectMembers).HasForeignKey(d => d.StudentPkId);
        });

        modelBuilder.Entity<ProjectType>(entity =>
        {
            entity.HasKey(e => e.ProjectTypePkId);

            entity.Property(e => e.ProjectTypePkId).HasColumnName("ProjectType_pkId");
            entity.Property(e => e.TypeName).HasMaxLength(50);
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.StudentPkId);

            entity.Property(e => e.StudentPkId).HasColumnName("Student_pkId");
            entity.Property(e => e.AcademicYearPkId).HasColumnName("AcademicYear_pkId");
            entity.Property(e => e.CreatedBy).HasMaxLength(50);
            entity.Property(e => e.DepartmentPkId).HasColumnName("Department_pkID");
            entity.Property(e => e.EmailPkId).HasColumnName("Email_PkId");
            entity.Property(e => e.IsEmailSubscribed).HasDefaultValue(true);
            entity.Property(e => e.NrcPkId).HasColumnName("NRC_pkId");
            entity.Property(e => e.Nrcnumber).HasColumnName("NRCNumber");
            entity.Property(e => e.NrctypePkId).HasColumnName("NRCType_pkId");
            entity.Property(e => e.PhoneNumber).HasMaxLength(50);
            entity.Property(e => e.StudentName).HasMaxLength(50);

            entity.HasOne(d => d.AcademicYearPk).WithMany(p => p.Students)
                .HasForeignKey(d => d.AcademicYearPkId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.DepartmentPk).WithMany(p => p.Students).HasForeignKey(d => d.DepartmentPkId);

            entity.HasOne(d => d.EmailPk).WithMany(p => p.Students)
                .HasForeignKey(d => d.EmailPkId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.NrcPk).WithMany(p => p.Students).HasForeignKey(d => d.NrcPkId);

            entity.HasOne(d => d.NrctypePk).WithMany(p => p.Students)
                .HasForeignKey(d => d.NrctypePkId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.SupervisorTeacher).WithMany(p => p.Students)
                .HasForeignKey(d => d.SupervisorTeacherId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Students_SupervisorTeacher");
        });

        modelBuilder.Entity<StudentCompany>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StudentC__3214EC07A51F0513");

            entity.ToTable("StudentCompany");

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.Company).WithMany(p => p.StudentCompanies)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StudentCompany_Companies");

            entity.HasOne(d => d.Student).WithMany(p => p.StudentCompanies)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StudentCompany_Students");

            entity.HasOne(d => d.Teacher).WithMany(p => p.StudentCompanies)
                .HasForeignKey(d => d.TeacherId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StudentCompany_Teachers");
        });

        modelBuilder.Entity<StudentDepartment>(entity =>
        {
            entity.HasKey(e => e.DepartmentPkId);

            entity.Property(e => e.DepartmentPkId).HasColumnName("Department_pkID");
            entity.Property(e => e.DepartmentName).HasMaxLength(100);
        });

        modelBuilder.Entity<Teacher>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Teachers__3214EC079B30E63A");

            entity.HasIndex(e => e.Email, "UQ__Teachers__A9D10534ECD3FED1").IsUnique();

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.PasswordHash).HasMaxLength(200);
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .HasDefaultValue("Teacher");

            entity.HasOne(d => d.AcademicYearPk).WithMany(p => p.Teachers)
                .HasForeignKey(d => d.AcademicYearPkId)
                .HasConstraintName("FK_Teachers_AcademicYears");

            entity.HasOne(d => d.DepartmentPk).WithMany(p => p.Teachers)
                .HasForeignKey(d => d.DepartmentPkId)
                .HasConstraintName("FK_Teachers_StudentDepartments");
        });

        modelBuilder.Entity<TeacherCompany>(entity =>
        {
            entity.HasKey(e => e.TeacherCompanyId).HasName("PK__TeacherC__91BD19678559EEBC");

            entity.ToTable("TeacherCompany");

            entity.HasIndex(e => new { e.TeacherId, e.CompanyPkId }, "UQ_TeacherCompany").IsUnique();

            entity.Property(e => e.AssignedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.CompanyPk).WithMany(p => p.TeacherCompanies)
                .HasForeignKey(d => d.CompanyPkId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TeacherCompany_Company");

            entity.HasOne(d => d.Teacher).WithMany(p => p.TeacherCompanies)
                .HasForeignKey(d => d.TeacherId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TeacherCompany_Teacher");
        });

        modelBuilder.Entity<TeacherStudent>(entity =>
        {
            entity.HasKey(e => e.TeacherStudentPkId).HasName("PK__TeacherS__2F38BA5A25DD3F6D");

            entity.ToTable("TeacherStudent");

            entity.Property(e => e.AssignedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.AcademicYearPk).WithMany(p => p.TeacherStudents)
                .HasForeignKey(d => d.AcademicYearPkId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TeacherStudent_AcademicYear");

            entity.HasOne(d => d.StudentPk).WithMany(p => p.TeacherStudents)
                .HasForeignKey(d => d.StudentPkId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TeacherStudent_Student");

            entity.HasOne(d => d.Teacher).WithMany(p => p.TeacherStudents)
                .HasForeignKey(d => d.TeacherId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TeacherStudent_Teacher");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
