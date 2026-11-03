using Maratona.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Maratona.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    private const string RatingCheck = "rating IS NULL OR (rating >= 0 AND rating <= 10 AND rating * 2 = trunc(rating * 2))";

    public DbSet<User> Users => Set<User>();
    public DbSet<Follow> Follows => Set<Follow>();
    public DbSet<Title> Titles => Set<Title>();
    public DbSet<Season> Seasons => Set<Season>();
    public DbSet<Episode> Episodes => Set<Episode>();
    public DbSet<ExternalRating> ExternalRatings => Set<ExternalRating>();
    public DbSet<OmdbQueueItem> OmdbQueue => Set<OmdbQueueItem>();
    public DbSet<ApiUsage> ApiUsage => Set<ApiUsage>();
    public DbSet<JobRun> JobRuns => Set<JobRun>();
    public DbSet<Award> Awards => Set<Award>();
    public DbSet<LibraryEntry> LibraryEntries => Set<LibraryEntry>();
    public DbSet<SeasonEntry> SeasonEntries => Set<SeasonEntry>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<ReviewLike> ReviewLikes => Set<ReviewLike>();
    public DbSet<PrivateNote> PrivateNotes => Set<PrivateNote>();
    public DbSet<UserList> Lists => Set<UserList>();
    public DbSet<ListItem> ListItems => Set<ListItem>();
    public DbSet<Favorite> Favorites => Set<Favorite>();

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Properties<MediaType>().HaveConversion<string>().HaveMaxLength(10);
        builder.Properties<RatingStyle>().HaveConversion<string>().HaveMaxLength(10);
        builder.Properties<AwardKind>().HaveConversion<string>().HaveMaxLength(10);
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.Property(x => x.Username).HasMaxLength(30);
            e.Property(x => x.Email).HasMaxLength(254);
            e.Property(x => x.DisplayName).HasMaxLength(60);
            e.Property(x => x.Bio).HasMaxLength(500);
            e.Property(x => x.AvatarUrl).HasMaxLength(500);
            e.Property(x => x.CoverUrl).HasMaxLength(500);
            e.HasIndex(x => x.Username).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();
        });

        b.Entity<Follow>(e =>
        {
            e.HasKey(x => new { x.FollowerId, x.FollowedId });
            e.HasOne(x => x.Follower).WithMany().HasForeignKey(x => x.FollowerId);
            e.HasOne(x => x.Followed).WithMany().HasForeignKey(x => x.FollowedId);
            e.HasIndex(x => x.FollowedId);
            e.ToTable(t => t.HasCheckConstraint("ck_follows_not_self", "follower_id <> followed_id"));
        });

        b.Entity<Title>(e =>
        {
            e.HasIndex(x => new { x.TmdbId, x.MediaType }).IsUnique();
            e.HasIndex(x => x.ImdbId);
            e.HasIndex(x => x.AtualizadoEm);
            e.HasIndex(x => x.Popularity);
            e.Property(x => x.Name).HasMaxLength(300);
            e.Property(x => x.OriginalName).HasMaxLength(300);
            e.Property(x => x.ImdbId).HasMaxLength(20);
            e.Property(x => x.Status).HasMaxLength(40);
            e.OwnsOne(x => x.Credits, c =>
            {
                c.ToJson();
                c.OwnsMany(x => x.Cast);
            });
        });

        b.Entity<Season>(e =>
        {
            e.HasIndex(x => new { x.TitleId, x.SeasonNumber }).IsUnique();
            e.HasOne(x => x.Title).WithMany(x => x.Seasons).HasForeignKey(x => x.TitleId);
        });

        b.Entity<Episode>(e =>
        {
            e.HasIndex(x => new { x.SeasonId, x.EpisodeNumber }).IsUnique();
            e.HasOne(x => x.Season).WithMany(x => x.Episodes).HasForeignKey(x => x.SeasonId);
        });

        b.Entity<ExternalRating>(e =>
        {
            e.HasKey(x => x.TitleId);
            e.HasOne(x => x.Title).WithOne(x => x.ExternalRating).HasForeignKey<ExternalRating>(x => x.TitleId);
            e.Property(x => x.Imdb).HasPrecision(3, 1);
            e.HasIndex(x => x.NextRefreshAt);
        });

        b.Entity<OmdbQueueItem>(e =>
        {
            e.ToTable("omdb_queue");
            e.HasKey(x => x.TitleId);
            e.HasOne(x => x.Title).WithMany().HasForeignKey(x => x.TitleId);
            e.HasIndex(x => x.RequestCount);
        });

        b.Entity<ApiUsage>(e =>
        {
            e.ToTable("api_usage");
            e.HasKey(x => new { x.Date, x.Bucket });
            e.Property(x => x.Bucket).HasMaxLength(30);
        });

        b.Entity<JobRun>(e =>
        {
            e.HasKey(x => x.Name);
            e.Property(x => x.Name).HasMaxLength(50);
        });

        b.Entity<Award>(e =>
        {
            e.HasOne(x => x.Title).WithMany(x => x.Awards).HasForeignKey(x => x.TitleId);
            e.HasIndex(x => x.TitleId);
            e.Property(x => x.Category).HasMaxLength(200);
            e.Property(x => x.Person).HasMaxLength(200);
        });

        b.Entity<LibraryEntry>(e =>
        {
            e.HasKey(x => new { x.UserId, x.TitleId });
            e.Property(x => x.Rating).HasPrecision(3, 1);
            e.HasIndex(x => x.TitleId);
            e.ToTable(t => t.HasCheckConstraint("ck_library_entries_rating", RatingCheck));
        });

        b.Entity<SeasonEntry>(e =>
        {
            e.HasKey(x => new { x.UserId, x.SeasonId });
            e.Property(x => x.Rating).HasPrecision(3, 1);
            e.HasIndex(x => x.SeasonId);
            e.ToTable(t => t.HasCheckConstraint("ck_season_entries_rating", RatingCheck));
        });

        b.Entity<Review>(e =>
        {
            e.Property(x => x.Body).HasMaxLength(10000);
            e.HasIndex(x => new { x.UserId, x.TitleId, x.SeasonId }).IsUnique().AreNullsDistinct(false);
            e.HasIndex(x => x.CreatedAt);
            e.HasIndex(x => x.TitleId);
        });

        b.Entity<ReviewLike>(e =>
        {
            e.HasKey(x => new { x.UserId, x.ReviewId });
            e.HasOne(x => x.Review).WithMany(x => x.Likes).HasForeignKey(x => x.ReviewId);
            e.HasIndex(x => x.ReviewId);
        });

        b.Entity<PrivateNote>(e =>
        {
            e.Property(x => x.Body).HasMaxLength(20000);
            e.HasIndex(x => new { x.UserId, x.TitleId, x.SeasonId, x.EpisodeId }).IsUnique().AreNullsDistinct(false);
        });

        b.Entity<UserList>(e =>
        {
            e.ToTable("lists");
            e.Property(x => x.Name).HasMaxLength(100);
            e.Property(x => x.Description).HasMaxLength(1000);
            e.HasIndex(x => x.UserId);
        });

        b.Entity<ListItem>(e =>
        {
            e.HasKey(x => new { x.ListId, x.TitleId });
            e.HasOne(x => x.List).WithMany(x => x.Items).HasForeignKey(x => x.ListId);
        });

        b.Entity<Favorite>(e =>
        {
            e.HasKey(x => new { x.UserId, x.MediaType, x.Slot });
            e.HasIndex(x => new { x.UserId, x.TitleId }).IsUnique();
            e.HasIndex(x => new { x.UserId, x.MediaType }).IsUnique().HasFilter("crown");
            e.ToTable(t => t.HasCheckConstraint("ck_favorites_slot", "slot BETWEEN 1 AND 4"));
        });
    }
}
