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


app.MapGet("/articles", async (ArticleDbContext db) =>
    await db.Articles.ToListAsync())
    .Produces<Article[]>(StatusCodes.Status200OK)
    .WithName("GetAllArticles")
    .WithTags("Getters");

app.MapGet("/articles/is-online", async (ArticleDbContext db) =>
    await db.Articles.Where(t => t.IsOnline).ToListAsync())
    .Produces<Article[]>(StatusCodes.Status200OK)
    .WithName("GetAllOnlineArticles")
    .WithTags("Getters");

app.MapGet("/articles/{id}", async (int id, ArticleDbContext db) =>
    await db.Articles.FindAsync(id)
        is Article article
            ? Results.Ok(article)
            : Results.NotFound())
    .Produces<Article>(StatusCodes.Status200OK)
    .WithName("GetArticleDetails")
    .WithTags("Getters");

app.MapPost("/articles", async (Article article, ArticleDbContext db) =>
{
    db.Articles.Add(article);
    await db.SaveChangesAsync();

    return Results.Created($"/articles/{article.Id}", article);
})
    .Produces<Article>(StatusCodes.Status201Created)
    .WithName("CreateArticle")
    .WithTags("Getters");

app.MapPut("/articles/{id}", async (int id, Article updatedArticle, ArticleDbContext db) =>
{
    var articleFound = await db.Articles.FindAsync(id);

    if (articleFound is null) return Results.NotFound();

    articleFound!.Title = updatedArticle.Title;
    articleFound.PublishedDate = updatedArticle.PublishedDate;
    articleFound.Author = updatedArticle.Author;
    articleFound.IsOnline = updatedArticle.IsOnline;

    await db.SaveChangesAsync();

    return Results.NoContent();
})
    .Produces(StatusCodes.Status204NoContent)
    .WithName("UpdateArticle")
    .WithTags("Setters");

app.MapDelete("/articles/{id}", async (int id, ArticleDbContext db) =>
{
    if (await db.Articles.FindAsync(id) is Article article)
    {
        db.Articles.Remove(article);
        await db.SaveChangesAsync();
        return Results.Ok(article);
    }

    return Results.NotFound();
})
    .Produces(StatusCodes.Status204NoContent)
    .WithName("DeleteArticle")
    .WithTags("Setters");


app.Run();
