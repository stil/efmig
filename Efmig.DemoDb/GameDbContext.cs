using Efmig.DemoDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace Efmig.DemoDb;


// docker run -it --rm -p 5432:5432 --name efmig-pgsql -e POSTGRES_PASSWORD=mysecretpassword -e POSTGRES_DB=efmig postgres
public class GameDbContext(DbContextOptions<GameDbContext> options) : DbContext(options)
{
    public DbSet<Game> Games { get; set; }
    public DbSet<Developer> Developers { get; set; }
    public DbSet<Genre> Genres { get; set; }
    public DbSet<Platform> Platforms { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<DLC> DLCs { get; set; }
    public DbSet<GameTag> GameTags { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Game>()
            .HasMany(g => g.Genres)
            .WithMany(g => g.Games);

        modelBuilder.Entity<Game>()
            .HasMany(g => g.Platforms)
            .WithMany(p => p.Games);

        modelBuilder.Entity<Game>()
            .HasMany(g => g.Tags)
            .WithMany(t => t.Games);

        modelBuilder.Entity<Review>()
            .OwnsOne(r => r.ReviewerInfo);

        base.OnModelCreating(modelBuilder);
    }
}