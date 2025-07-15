using DotNetTruyen.Models;

namespace DotNetTruyen.ViewModels.Management
{
    public class ComicIndexViewModel
    {
        public List<Comic> Comics { get; set; }
        public string SearchQuery { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalComics { get; set; }    
        public long TotalViews { get; set; }    
        public int TotalFollows { get; set; }   
        public long TotalLikes { get; set; }
    }
}
