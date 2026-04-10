using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ForumApi.Data;

namespace ForumApi.Controllers;

[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    private readonly AppDbContext _db;
    // blabla 
    public UserController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _db.Users
            .Select(u => new
            {
                u.Id,
                u.Username,
                u.Email,
                u.CreatedAt
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _db.Users
            .Where(u => u.Id == id)
            .Select(u => new
            {
                u.Id,
                u.Username,
                u.Email,
                u.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (user == null)
            return NotFound("Utilisateur introuvable.");

        return Ok(user);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteById(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
            return NotFound("Utilisateur introuvable.");

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return Ok("Utilisateur supprimé.");
    }
}
