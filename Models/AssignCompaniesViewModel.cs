using Microsoft.AspNetCore.Mvc.Rendering;

namespace ProjectManagementSystem.Models
{
    public class AssignCompaniesViewModel
    {

        public int StudentId { get; set; }
        public int TeacherId { get; set; }
        public int CompanyId { get; set; }
        public string TeacherName { get; set; }

        public List<SelectListItem> Companies { get; set; }
        public List<int> SelectedCompanyIds { get; set; }
    }
}
