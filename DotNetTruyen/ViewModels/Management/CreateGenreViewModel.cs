using System.ComponentModel.DataAnnotations;

namespace DotNetTruyen.ViewModels.Management
{
    public class CreateGenreViewModel
    {
        [Required(ErrorMessage = "Tên thể loại không được để trống")]
        public string GenreName { get; set; }
    }
}
