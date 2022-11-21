using _00_Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _01_ArticleWithControllers.Controllers;

[ApiController]
[Route("articles")]
public class ArticleController : ControllerBase
{
    private readonly ArticleDbContext _dbContext;
    private readonly ILogger<ArticleController> _logger;

    public ArticleController(
        ArticleDbContext dbContext,
        ILogger<ArticleController> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }


    [HttpGet(Name = "GetAllArticles")]
    public async Task<IActionResult> GetAll()
        => new OkObjectResult(await _dbContext.Articles.ToListAsync());

    [HttpGet("is-online", Name = "GetAllArticlesOnline")]
    public async Task<IActionResult> GetAllOnline()
        => new OkObjectResult(await _dbContext.Articles.Where(a => a.IsOnline).ToListAsync());

    [HttpGet("{id:int}", Name = "GetArticle")]
    public async Task<IActionResult> Get(int id)
        => await _dbContext.Articles.FindAsync(id)
            is Article articleFound ?
                Ok(articleFound) :
                NotFound();

    [HttpPost(Name = "CreateArticle")]
    public async Task<IActionResult> Put(Article newArticle)
    {
        _dbContext.Articles.Add(newArticle);

        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = newArticle.Id }, newArticle);
    }

    [HttpPut("{id:int}", Name = "UpdateArticle")]
    public async Task<IActionResult> Put(int id, Article updatedArticle)
    {
        var articleFound = await _dbContext.Articles.FindAsync(id);

        if (articleFound is null)
            return NotFound();

        articleFound!.Title = updatedArticle.Title;
        articleFound.PublishedDate = updatedArticle.PublishedDate;
        articleFound.Author = updatedArticle.Author;
        articleFound.IsOnline = updatedArticle.IsOnline;

        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}", Name = "DeleteArticle")]
    public async Task<IActionResult> Delete(int id)
    {
        if (await _dbContext.Articles.FindAsync(id) is Article articleFound)
        {
            _dbContext.Articles.Remove(articleFound);
            await _dbContext.SaveChangesAsync();
            return Ok(articleFound);
        }

        return NotFound();
    }
}
