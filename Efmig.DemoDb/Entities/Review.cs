namespace Efmig.DemoDb.Entities;

public class Review
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public Game Game { get; set; }
    public int Score { get; set; }
    public string Comment { get; set; }

    public ReviewerInfo ReviewerInfo { get; set; }
}