using DotNetTruyen.Models;

namespace DotNetTruyen.ViewModels.Management
{
    public class UserIndexViewModel
    {
        public List<UserViewModel> Users { get; set; }
        public string SearchQuery { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public List<string> Roles { get; set; } = default!;
    }
}
