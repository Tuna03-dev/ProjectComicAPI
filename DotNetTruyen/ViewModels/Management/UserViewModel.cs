namespace DotNetTruyen.ViewModels.Management
{
    public class UserViewModel
    {
        public string? Id { get; set; }
        public string? NameToDisplay { get; set; }
        public string? ImageUrl { get; set; }
        public string? Email { get; set; }
        public string? Role { get; set; } 
        public DateTimeOffset? Status { get; set; }
    }

}
