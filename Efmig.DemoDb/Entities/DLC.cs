namespace Efmig.DemoDb.Entities;

public class DLC
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public Game Game { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
}