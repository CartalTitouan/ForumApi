namespace ForumApi.DTOs;

public record CreateCommentRequest(string Content);

public record CommentResponse(
    int Id,
    string Content,
    string AuthorUsername,
    DateTime CreatedAt
);
