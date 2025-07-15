namespace DotNetTruyen.ViewModels.Management
{
    public class DashboardViewModel
    {
        public int TotalComics { get; set; }
        public int TotalChapters { get; set; }
        public long TotalViews { get; set; }
        public int TotalUsers { get; set; }


        public double ComicsChangePercentage { get; set; }
        public double ChaptersChangePercentage { get; set; }
        public double ViewsChangePercentage { get; set; }
        public double UsersChangePercentage { get; set; }

        public List<RecentChapterViewModel> RecentChapters { get; set; }
        public List<TopGenreViewModel> TopGenres { get; set; }
        public List<string> ViewsByMonthLabels { get; set; }
        public List<int> ViewsByMonthData { get; set; }
        public List<string> PreviousViewsByMonthLabels { get; set; }
        public List<int> PreviousViewsByMonthData { get; set; }
    }
}
