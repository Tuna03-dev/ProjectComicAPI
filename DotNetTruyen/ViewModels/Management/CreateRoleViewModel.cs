namespace DotNetTruyen.ViewModels.Management
{
    public class CreateRoleViewModel
    {
        public string? Name { get; set; }
        public List<string> SelectedPermission { get; set; } = default!;
        public List<string> Permissions { get; set; } = default!;
    }
}
