using ProjectManagementSystem.Models.Public;
using X.PagedList;

namespace ProjectManagementSystem.ViewModels
{
    public class CityListViewModel
    {
        public IPagedList<CityInternshipViewModel> Cities { get; set; }

        public int CurrentPage { get; set; }

        public int TotalPages => Cities?.PageCount ?? 1;
    }
}