using Microsoft.EntityFrameworkCore;

namespace _00_Domain;

public class ArticleDbContext : DbContext
{
    public ArticleDbContext(DbContextOptions options)
        : base(options)
    {
    }

    public DbSet<Article> Articles => Set<Article>();
}