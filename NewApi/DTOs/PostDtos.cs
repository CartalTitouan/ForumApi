namespace ForumApi.DTOs;

public record CreatePostRequest(string Title, string Content);

public record PostResponse(
    int Id,
    string Title,
    string Content,
    string AuthorUsername,
    int Likes,
    int Dislikes,
    DateTime CreatedAt
);
