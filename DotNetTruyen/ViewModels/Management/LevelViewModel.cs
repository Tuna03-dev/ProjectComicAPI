namespace DotNetTruyen.ViewModels.Management
{
    public class LevelViewModel
    {
        public Guid Id { get; set; }
        public int LevelNumber { get; set; }
        public int ExpRequired { get; set; }
        public string Name { get; set; }
        public int UserCount { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
