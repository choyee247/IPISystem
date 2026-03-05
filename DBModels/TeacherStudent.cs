using System;
using System.Collections.Generic;

namespace ProjectManagementSystem.DBModels;

public partial class TeacherStudent
{
    public int TeacherStudentPkId { get; set; }

    public int TeacherId { get; set; }

    public int StudentPkId { get; set; }

    public int AcademicYearPkId { get; set; }

    public DateTime AssignedDate { get; set; }

    public bool IsActive { get; set; }

    public virtual AcademicYear AcademicYearPk { get; set; } = null!;

    public virtual Student StudentPk { get; set; } = null!;

    public virtual Teacher Teacher { get; set; } = null!;
}
