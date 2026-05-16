using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ForumApi.Data;

namespace ForumApi.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly AppDbContext _db;

    public UserController(AppDbContext db) => _db = db;

    private bool IsAdmin() => bool.Parse(User.FindFirst("IsAdmin")?.Value ?? "false");

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        if (!IsAdmin()) return Forbid();

        var users = await _db.Users
            .Select(u => new { u.Id, u.Username, u.Email, u.IsAdmin, u.CreatedAt })
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        if (!IsAdmin()) return Forbid();

        var user = await _db.Users
            .Where(u => u.Id == id)
            .Select(u => new
            {
                u.Id,
                u.Username,
                u.Email,
                u.IsAdmin,
                u.CreatedAt,
                PostCount = u.Posts.Count,
                CommentCount = u.Comments.Count
            })
            .FirstOrDefaultAsync();

        if (user == null) return NotFound("Utilisateur introuvable.");
        return Ok(user);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteById(int id)
    {
        if (!IsAdmin()) return Forbid();

        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound("Utilisateur introuvable.");

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return Ok("Utilisateur supprimé.");
    }
}
