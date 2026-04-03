using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ForumApi.Data;
using ForumApi.DTOs;
using ForumApi.Models;
using System.Security.Claims;

namespace ForumApi.Controllers;

[ApiController]
[Route("api/posts")]
[Authorize]
public class PostController : ControllerBase
{
    private readonly AppDbContext _db;

    public PostController(AppDbContext db) => _db = db;

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        var posts = await _db.Posts
            .Include(p => p.User)
            .Include(p => p.Reactions)
            .Select(p => new PostResponse(
                p.Id,
                p.Title,
                p.Content,
                p.User.Username,
                p.Reactions.Count(r => r.IsLike),
                p.Reactions.Count(r => !r.IsLike),
                p.CreatedAt
            ))
            .ToListAsync();

        return Ok(posts);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        var post = await _db.Posts
            .Include(p => p.User)
            .Include(p => p.Reactions)
            .Where(p => p.Id == id)
            .Select(p => new PostResponse(
                p.Id,
                p.Title,
                p.Content,
                p.User.Username,
                p.Reactions.Count(r => r.IsLike),
                p.Reactions.Count(r => !r.IsLike),
                p.CreatedAt
            ))
            .FirstOrDefaultAsync();

        if (post == null)
            return NotFound("Post introuvable.");

        return Ok(post);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteById(int id)
    {
        var post = await _db.Posts.FindAsync(id);
        if (post == null)
            return NotFound("Poste introuvable.");

        _db.Posts.Remove(post);
        await _db.SaveChangesAsync();
        return Ok("Poste supprimé.");
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreatePostRequest req)
    {
        var userId = GetUserId();
        var post = new Post
        {
            Title = req.Title,
            Content = req.Content,
            UserId = userId
        };

        _db.Posts.Add(post);
        await _db.SaveChangesAsync();
        return Created($"/api/posts/{post.Id}", post.Id);
    }

    [HttpPost("{id}/like")]
    public async Task<IActionResult> Like(int id) => await React(id, true);

    [HttpPost("{id}/dislike")]
    public async Task<IActionResult> Dislike(int id) => await React(id, false);

    private async Task<IActionResult> React(int postId, bool isLike)
    {
        if (!await _db.Posts.AnyAsync(p => p.Id == postId))
            return NotFound("Post introuvable.");

        var userId = GetUserId();
        var existing = await _db.PostReactions
            .FirstOrDefaultAsync(r => r.PostId == postId && r.UserId == userId);

        if (existing != null)
        {
            if (existing.IsLike == isLike)
            {
                _db.PostReactions.Remove(existing);
            }
            else
            {
                existing.IsLike = isLike;
            }
        }
        else
        {
            _db.PostReactions.Add(new PostReaction { PostId = postId, UserId = userId, IsLike = isLike });
        }

        await _db.SaveChangesAsync();
        return Ok();
    }
}
