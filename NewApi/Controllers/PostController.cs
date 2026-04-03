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
public class PostController : ControllerBase
{
    private readonly AppDbContext _db;

    public PostController(AppDbContext db) => _db = db;

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
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

    [HttpPost]
    public async Task<IActionResult> Create(CreatePostRequest req, [FromQuery] int userId)
    {
        if (!await _db.Users.AnyAsync(u => u.Id == userId))
            return BadRequest("Utilisateur introuvable.");

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
    public async Task<IActionResult> Like(int id, [FromQuery] int userId) => await React(id, userId, true);

    [HttpPost("{id}/dislike")]
    public async Task<IActionResult> Dislike(int id, [FromQuery] int userId) => await React(id, userId, false);

    private async Task<IActionResult> React(int postId, int userId, bool isLike)
    {
        if (!await _db.Posts.AnyAsync(p => p.Id == postId))
            return NotFound("Post introuvable.");

        if (!await _db.Users.AnyAsync(u => u.Id == userId))
            return BadRequest("Utilisateur introuvable.");

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
