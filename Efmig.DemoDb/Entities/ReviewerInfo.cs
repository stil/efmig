using Microsoft.EntityFrameworkCore;

namespace Efmig.DemoDb.Entities;

[Owned]
public class ReviewerInfo
{
    public string Username { get; set; }
    public string Email { get; set; }
    public DateTimeOffset ReviewDate { get; set; }
}