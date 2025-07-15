namespace DotNetTruyen.Models
{
    public class Level : BaseEnity<Guid>
    {

        public int LevelNumber { get; set; }
        public int ExpRequired { get; set; }
        public string Name { get; set; } 
        public ICollection<User> Users { get; set; }
    }
}
