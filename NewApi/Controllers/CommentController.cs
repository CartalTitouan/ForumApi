using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ForumApi.Data;
using ForumApi.DTOs;
using ForumApi.Models;
using System.Security.Claims;

namespace ForumApi.Controllers;

[ApiController]
[Route("api/posts/{postId}/comments")]
[Authorize]
public class CommentController : ControllerBase
{
    private readonly AppDbContext _db;

    public CommentController(AppDbContext db) => _db = db;

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(int postId)
    {
        if (!await _db.Posts.AnyAsync(p => p.Id == postId))
            return NotFound("Post introuvable.");

        var comments = await _db.Comments
            .Where(c => c.PostId == postId)
            .Include(c => c.User)
            .Select(c => new CommentResponse(
                c.Id,
                c.Content,
                c.User.Username,
                c.CreatedAt
            ))
            .ToListAsync();

        return Ok(comments);
    }

    [HttpPost]
    public async Task<IActionResult> Create(int postId, CreateCommentRequest req)
    {
        if (!await _db.Posts.AnyAsync(p => p.Id == postId))
            return NotFound("Post introuvable.");

        var comment = new Comment
        {
            Content = req.Content,
            PostId = postId,
            UserId = GetUserId()
        };

        _db.Comments.Add(comment);
        await _db.SaveChangesAsync();
        return Created($"/api/posts/{postId}/comments/{comment.Id}", comment.Id);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int postId, int id)
    {
        var comment = await _db.Comments.FindAsync(id);
        if (comment == null || comment.PostId != postId)
            return NotFound("Commentaire introuvable.");

        var userId = GetUserId();
        if (comment.UserId != userId)
            return Forbid();

        _db.Comments.Remove(comment);
        await _db.SaveChangesAsync();
        return Ok("Commentaire supprimé.");
    }
}
