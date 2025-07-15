namespace DotNetTruyen.ViewModels.Management
{
    public class LevelIndexViewModel
    {
        public List<LevelViewModel> Levels { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string SearchQuery { get; set; }
        public int TotalLevels { get; set; }
        public int TotalUsers { get; set; }
        public int ActiveLevels { get; set; }
    }
}
