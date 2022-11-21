using System.Diagnostics;
using _00_Domain;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDbContext<ArticleDbContext>(opt => opt.UseInMemoryDatabase("ArticlesDb"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<Stopwatch>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var articles = app.MapGroup("/articles");


// articles.MapGet("/", async (ArticleDbContext db) =>
//     await db.Articles.ToListAsync());


// articles.MapGet("/is-online", async (ArticleDbContext db) =>
//     await db.Articles.Where(t => t.IsOnline).ToListAsync());

// articles.MapGet("/{id}", async (int id, ArticleDbContext db) =>
//     await db.Articles.FindAsync(id)
//         is Article article
//             ? Results.Ok(article)
//             : Results.NotFound());

// articles.MapPost("/", async (Article article, ArticleDbContext db) =>
// {
//     db.Articles.Add(article);
//     await db.SaveChangesAsync();

//     return Results.Created($"/articles/{article.Id}", article);
// });

// articles.MapPut("/{id}", async (int id, Article updatedArticle, ArticleDbContext db) =>
// {
//     var articleFound = await db.Articles.FindAsync(id);

//     if (articleFound is null) return Results.NotFound();

//     articleFound!.Title = updatedArticle.Title;
//     articleFound.PublishedDate = updatedArticle.PublishedDate;
//     articleFound.Author = updatedArticle.Author;
//     articleFound.IsOnline = updatedArticle.IsOnline;
// Task.Delay(6000);

//     await db.SaveChangesAsync();

//     return Results.NoContent();
// });

// articles.MapDelete("/{id}", async (int id, ArticleDbContext db) =>
// {
//     if (await db.Articles.FindAsync(id) is Article article)
//     {
//         db.Articles.Remove(article);
//         await db.SaveChangesAsync();
//         return Results.Ok(article);
//     }

//     return Results.NotFound();
// });


articles.MapArticleApi()
    .AddEndpointFilter<WaringTimoutFilter>();

app.Run();

public static class RouteGroupBuilderExtensions
{
    public static RouteGroupBuilder MapArticleApi(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (ArticleDbContext db) =>
            await db.Articles.ToListAsync());


        group.MapGet("/is-online", async (ArticleDbContext db) =>
            await db.Articles.Where(t => t.IsOnline).ToListAsync());

        group.MapGet("/{id}", async (int id, ArticleDbContext db) =>
            await db.Articles.FindAsync(id)
                is Article article
                    ? Results.Ok(article)
                    : Results.NotFound());

        group.MapPost("/", async (Article article, ArticleDbContext db) =>
        {
            db.Articles.Add(article);
            await db.SaveChangesAsync();
            await Task.Delay(6000);

            return Results.Created($"/articles/{article.Id}", article);
        });

        group.MapPut("/{id}", async (int id, Article updatedArticle, ArticleDbContext db) =>
        {
            var articleFound = await db.Articles.FindAsync(id);

            if (articleFound is null) return Results.NotFound();

            articleFound!.Title = updatedArticle.Title;
            articleFound.PublishedDate = updatedArticle.PublishedDate;
            articleFound.Author = updatedArticle.Author;
            articleFound.IsOnline = updatedArticle.IsOnline;

            await db.SaveChangesAsync();

            return Results.NoContent();
        });

        group.MapDelete("/{id}", async (int id, ArticleDbContext db) =>
        {
            if (await db.Articles.FindAsync(id) is Article article)
            {
                db.Articles.Remove(article);
                await db.SaveChangesAsync();
                return Results.Ok(article);
            }

            return Results.NotFound();
        });

        return group;
    }
}


public class WaringTimoutFilter : IEndpointFilter
{
    public const int TimeOutInMillisecond = 5000;
    private readonly Stopwatch _stopWatch;
    private ILogger _logger;

    public WaringTimoutFilter(ILoggerFactory loggerFactory, Stopwatch stopWatch)
    {
        _logger = loggerFactory.CreateLogger<WaringTimoutFilter>();
        _stopWatch = stopWatch;
    }

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext efiContext,
        EndpointFilterDelegate next)
    {
        _stopWatch.Start();

        var followup = await next(efiContext);

        _stopWatch.Stop();
        if (_stopWatch.ElapsedMilliseconds > TimeOutInMillisecond)
            _logger.LogWarning("request: {route} is higher than timeout {timeout}", efiContext.HttpContext.Request.Path, TimeOutInMillisecond);

        return followup;
    }
}