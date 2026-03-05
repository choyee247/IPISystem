using Microsoft.AspNetCore.Mvc.Rendering;
using ProjectManagementSystem.DBModels;
using System;
using System.Collections.Generic;

namespace ProjectManagementSystem.ViewModels
{
    public class ProjectIdeasViewModel
    {
        public List<ProjectManagementSystem.DBModels.Project> Projects { get; set; }

        public int CurrentPage { get; set; } = 1;

        public int PageSize { get; set; } = 6;

        public int TotalProjects { get; set; }
        public string? SearchTerm { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)(TotalProjects) / PageSize);

        public List<SelectListItem> ProjectTypes { get; set; } = new();

        public int? SelectedProjectTypeId { get; set; }

        public List<SelectListItem> Languages { get; set; } = new();

        public int? SelectedLanguageId { get; set; }
        public List<SelectListItem> AcademicYears { get; set; } = new();
        public int? SelectedAcademicYearId { get; set; }
        public List<Project> RelatedProjects { get; set; } = new();
    }
}
