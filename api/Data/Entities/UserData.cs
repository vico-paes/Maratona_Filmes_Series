namespace Maratona.Api.Data.Entities;

public class LibraryEntry
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int TitleId { get; set; }
    public Title Title { get; set; } = null!;
    public bool InWatchlist { get; set; }
    public bool Watched { get; set; }
    public DateTime? WatchedAt { get; set; }
    public decimal? Rating { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class SeasonEntry
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int SeasonId { get; set; }
    public Season Season { get; set; } = null!;
    public bool Watched { get; set; }
    public DateTime? WatchedAt { get; set; }
    public decimal? Rating { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class Review
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int TitleId { get; set; }
    public Title Title { get; set; } = null!;
    public int? SeasonId { get; set; }
    public Season? Season { get; set; }
    public string Body { get; set; } = "";
    public bool Spoiler { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<ReviewLike> Likes { get; set; } = [];
}

public class ReviewLike
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int ReviewId { get; set; }
    public Review Review { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class PrivateNote
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int TitleId { get; set; }
    public Title Title { get; set; } = null!;
    public int? SeasonId { get; set; }
    public Season? Season { get; set; }
    public int? EpisodeId { get; set; }
    public Episode? Episode { get; set; }
    public string Body { get; set; } = "";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class UserList
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public bool IsPublic { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<ListItem> Items { get; set; } = [];
}

public class ListItem
{
    public int ListId { get; set; }
    public UserList List { get; set; } = null!;
    public int TitleId { get; set; }
    public Title Title { get; set; } = null!;
    public int Position { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}

public class Favorite
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public MediaType MediaType { get; set; }
    public int Slot { get; set; }
    public int TitleId { get; set; }
    public Title Title { get; set; } = null!;
    public bool Crown { get; set; }
}
