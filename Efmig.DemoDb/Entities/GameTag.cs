namespace Efmig.DemoDb.Entities;

public class GameTag
{
    public int Id { get; set; }
    public string Tag { get; set; }
    public ICollection<Game> Games { get; set; } = new List<Game>();
}