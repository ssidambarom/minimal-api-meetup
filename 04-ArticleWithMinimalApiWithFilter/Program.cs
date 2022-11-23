using _00_Domain;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDbContext<ArticleDbContext>(opt => opt.UseInMemoryDatabase("ArticlesDb"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.MapGet("/", () =>
    {
        app.Logger.LogInformation("             Endpoint");
        return "Test of multiple filters";
    })
    .AddEndpointFilter(async (efiContext, next) =>
    {
        app.Logger.LogInformation("Before first filter");
        var result = await next(efiContext);
        app.Logger.LogInformation("After first filter");
        return result;
    })
    .AddEndpointFilter(async (efiContext, next) =>
    {
        app.Logger.LogInformation(" Before 2nd filter");
        var result = await next(efiContext);
        app.Logger.LogInformation(" After 2nd filter");
        return result;
    })
    .AddEndpointFilter(async (efiContext, next) =>
    {
        app.Logger.LogInformation("     Before 3rd filter");
        var result = await next(efiContext);
        app.Logger.LogInformation("     After 3rd filter");
        return result;
    });


app.MapGet("/articles", async (ArticleDbContext db) =>
    await db.Articles.ToListAsync());

app.MapGet("/articles/is-online", async (ArticleDbContext db) =>
    await db.Articles.Where(t => t.IsOnline).ToListAsync());

app.MapGet("/articles/{id}", async (int id, ArticleDbContext db) =>
    await db.Articles.FindAsync(id)
        is Article article
            ? Results.Ok(article)
            : Results.NotFound())
    .AddEndpointFilter(async (invocationContext, next) =>
    {
        var id = invocationContext.GetArgument<int>(0);

        if (id == 0)
        {
            return Results.Problem("0 is not allowed!");
        }
        return await next(invocationContext);
    });

app.MapPost("/articles", async (Article article, ArticleDbContext db) =>
{
    db.Articles.Add(article);
    await db.SaveChangesAsync();

    return Results.Created($"/articles/{article.Id}", article);
}).AddEndpointFilter<ArticleIsValidFilter>();

app.MapPut("/articles/{id}", async (Article updatedArticle, int id, ArticleDbContext db) =>
{
    var articleFound = await db.Articles.FindAsync(id);

    if (articleFound is null) return Results.NotFound();

    articleFound!.Title = updatedArticle.Title;
    articleFound.PublishedDate = updatedArticle.PublishedDate;
    articleFound.Author = updatedArticle.Author;
    articleFound.IsOnline = updatedArticle.IsOnline;

    await db.SaveChangesAsync();

    return Results.NoContent();
}).AddEndpointFilter<ArticleIsValidFilter>();

app.MapDelete("/articles/{id}", async (int id, ArticleDbContext db) =>
{
    if (await db.Articles.FindAsync(id) is Article article)
    {
        db.Articles.Remove(article);
        await db.SaveChangesAsync();
        return Results.Ok(article);
    }

    return Results.NotFound();
});

app.Run();

public class ArticleIsValidFilter : IEndpointFilter
{
    private ILogger _logger;
    public ArticleIsValidFilter(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<ArticleIsValidFilter>();
    }

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext efiContext,
        EndpointFilterDelegate next)
    {
        var article = efiContext.GetArgument<Article>(0);

        var validationError = Utilities.IsValid(article!);

        if (!string.IsNullOrEmpty(validationError))
        {
            _logger.LogWarning(validationError);
            return Results.Problem(validationError);
        }
        return await next(efiContext);
    }
}

public static class Utilities
{
    public static string IsValid(Article src)
    {
        const string Mandatory = "{0} is mandatory.";
        const string DateMustBeBefore = "{0} must be before {1}.";

        if (string.IsNullOrWhiteSpace(src.Title))
            return string.Format(Mandatory, nameof(Article.Title));

        if (string.IsNullOrWhiteSpace(src.Author))
            return string.Format(Mandatory, nameof(Article.Author));

        if (src.PublishedDate > DateTime.Now)
            return string.Format(DateMustBeBefore, nameof(Article.PublishedDate), DateTime.Now);

        return string.Empty;
    }
}