namespace Efmig.DemoDb.Entities;

public class Game
{
    public int Id { get; set; }
    public string Title { get; set; }
    public DateTimeOffset ReleaseDate { get; set; }
    public decimal Price { get; set; }
    public GameRating Rating { get; set; }

    public int DeveloperId { get; set; }
    public Developer Developer { get; set; }

    public ICollection<Genre> Genres { get; set; } = new List<Genre>();
    public ICollection<Platform> Platforms { get; set; } = new List<Platform>();
    // public ICollection<Review> Reviews { get; set; } = new List<Review>();
    // public ICollection<DLC> DLCs { get; set; } = new List<DLC>();
    public ICollection<GameTag> Tags { get; set; } = new List<GameTag>();
}