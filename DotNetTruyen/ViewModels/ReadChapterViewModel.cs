using DotNetTruyen.Models;

namespace DotNetTruyen.ViewModels
{
    public class ReadChapterViewModel
    {
        public Chapter Chapter { get; set; }
        public Chapter PreviousChapter { get; set; }
        public Chapter NextChapter { get; set; }
        public List<Chapter> AllChapters { get; set; }
    }
}
