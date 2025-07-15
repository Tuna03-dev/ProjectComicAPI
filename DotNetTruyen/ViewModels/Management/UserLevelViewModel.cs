namespace DotNetTruyen.ViewModels.Management
{
    public class UserLevelViewModel
    {
        public int LevelNumber { get; set; }
        public string LevelName { get; set; }
        public int CurrentExp { get; set; }
        public int ExpRequiredForNextLevel { get; set; }
        public int ExpToNextLevel { get; set; }
        public double ProgressPercentage { get; set; }
        public bool IsMaxLevel { get; set; }
    }
}
