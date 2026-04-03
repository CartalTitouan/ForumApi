namespace ForumApi.Models;

public class PostReaction
{
    public int Id { get; set; }
    public bool IsLike { get; set; } // true = like, false = dislike

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int PostId { get; set; }
    public Post Post { get; set; } = null!;
}
