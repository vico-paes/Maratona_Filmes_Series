using System;
using System.Collections.Generic;
using Maratona.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Maratona.Api.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    partial class AppDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "10.0.14")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Maratona.Api.Data.Entities.ApiUsage", b =>
                {
                    b.Property<DateOnly>("Date")
                        .HasColumnType("date")
                        .HasColumnName("date");

                    b.Property<string>("Bucket")
                        .HasMaxLength(30)
                        .HasColumnType("character varying(30)")
                        .HasColumnName("bucket");

                    b.Property<int>("Calls")
                        .HasColumnType("integer")
                        .HasColumnName("calls");

                    b.HasKey("Date", "Bucket")
                        .HasName("pk_api_usage");

                    b.ToTable("api_usage", (string)null);
                });

            modelBuilder.Entity("Maratona.Api.Data.Entities.Award", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Category")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)")
                        .HasColumnName("category");

                    b.Property<string>("ImdbId")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)")
                        .HasColumnName("imdb_id");

                    b.Property<bool>("IsMajor")
                        .HasColumnType("boolean")
                        .HasColumnName("is_major");

                    b.Property<string>("Kind")
                        .IsRequired()
                        .HasMaxLength(10)
                        .HasColumnType("character varying(10)")
                        .HasColumnName("kind");

                    b.Property<string>("Person")
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)")
                        .HasColumnName("person");

                    b.Property<string>("Source")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)")
                        .HasColumnName("source");

                    b.Property<bool>("Won")
                        .HasColumnType("boolean")
                        .HasColumnName("won");

                    b.Property<int?>("Year")
                        .HasColumnType("integer")
                        .HasColumnName("year");

                    b.HasKey("Id")
                        .HasName("pk_awards");

                    b.HasIndex("ImdbId")
                        .HasDatabaseName("ix_awards_imdb_id");

                    b.HasIndex("Source")
                        .HasDatabaseName("ix_awards_source");

                    b.ToTable("awards", (string)null);
                });

            modelBuilder.Entity("Maratona.Api.Data.Entities.Episode", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<DateOnly?>("AirDate")
                        .HasColumnType("date")
                        .HasColumnName("air_date");

                    b.Property<int>("EpisodeNumber")
                        .HasColumnType("integer")
                        .HasColumnName("episode_number");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.Property<string>("Overview")
                        .HasColumnType("text")
                        .HasColumnName("overview");

                    b.Property<int?>("Runtime")
                        .HasColumnType("integer")
                        .HasColumnName("runtime");

                    b.Property<int>("SeasonId")
                        .HasColumnType("integer")
                        .HasColumnName("season_id");

                    b.Property<string>("StillPath")
                        .HasColumnType("text")
                        .HasColumnName("still_path");

                    b.HasKey("Id")
                        .HasName("pk_episodes");

                    b.HasIndex("SeasonId", "EpisodeNumber")
                        .IsUnique()
                        .HasDatabaseName("ix_episodes_season_id_episode_number");

                    b.ToTable("episodes", (string)null);
                });

            modelBuilder.Entity("Maratona.Api.Data.Entities.ExternalRating", b =>
                {
                    b.Property<int>("TitleId")
                        .HasColumnType("integer")
                        .HasColumnName("title_id");

                    b.Property<DateTime>("AtualizadoEm")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("atualizado_em");

                    b.Property<string>("AwardsText")
                        .HasMaxLength(300)
                        .HasColumnType("character varying(300)")
                        .HasColumnName("awards_text");

                    b.Property<int?>("EmmyNominations")
                        .HasColumnType("integer")
                        .HasColumnName("emmy_nominations");

                    b.Property<int?>("EmmyWins")
                        .HasColumnType("integer")
                        .HasColumnName("emmy_wins");

                    b.Property<decimal?>("Imdb")
                        .HasPrecision(3, 1)
                        .HasColumnType("numeric(3,1)")
                        .HasColumnName("imdb");

                    b.Property<int?>("ImdbVotes")
                        .HasColumnType("integer")
                        .HasColumnName("imdb_votes");

                    b.Property<int?>("Metacritic")
                        .HasColumnType("integer")
                        .HasColumnName("metacritic");

                    b.Property<DateTime>("NextRefreshAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("next_refresh_at");

                    b.Property<bool>("NotFound")
                        .HasColumnType("boolean")
                        .HasColumnName("not_found");

                    b.Property<int?>("OscarNominations")
                        .HasColumnType("integer")
                        .HasColumnName("oscar_nominations");

                    b.Property<int?>("OscarWins")
                        .HasColumnType("integer")
                        .HasColumnName("oscar_wins");

                    b.Property<int?>("RottenTomatoes")
                        .HasColumnType("integer")
                        .HasColumnName("rotten_tomatoes");

                    b.HasKey("TitleId")
                        .HasName("pk_external_ratings");

                    b.HasIndex("NextRefreshAt")
                        .HasDatabaseName("ix_external_ratings_next_refresh_at");

                    b.ToTable("external_ratings", (string)null);
                });

            modelBuilder.Entity("Maratona.Api.Data.Entities.Favorite", b =>
                {
                    b.Property<int>("UserId")
                        .HasColumnType("integer")
                        .HasColumnName("user_id");

                    b.Property<string>("MediaType")
                        .HasMaxLength(10)
                        .HasColumnType("character varying(10)")
                        .HasColumnName("media_type");

                    b.Property<int>("Slot")
                        .HasColumnType("integer")
                        .HasColumnName("slot");

                    b.Property<bool>("Crown")
                        .HasColumnType("boolean")
                        .HasColumnName("crown");

                    b.Property<int>("TitleId")
                        .HasColumnType("integer")
                        .HasColumnName("title_id");

                    b.HasKey("UserId", "MediaType", "Slot")
                        .HasName("pk_favorites");

                    b.HasIndex("TitleId")
                        .HasDatabaseName("ix_favorites_title_id");

                    b.HasIndex("UserId", "MediaType")
                        .IsUnique()
                        .HasDatabaseName("ix_favorites_user_id_media_type")
                        .HasFilter("crown");

                    b.HasIndex("UserId", "TitleId")
                        .IsUnique()
                        .HasDatabaseName("ix_favorites_user_id_title_id");

                    b.ToTable("favorites", null, t =>
                        {
                            t.HasCheckConstraint("ck_favorites_slot", "slot BETWEEN 1 AND 4");
                        });
                });

            modelBuilder.Entity("Maratona.Api.Data.Entities.Follow", b =>
                {
                    b.Property<int>("FollowerId")
                        .HasColumnType("integer")
                        .HasColumnName("follower_id");

                    b.Property<int>("FollowedId")
                        .HasColumnType("integer")
                        .HasColumnName("followed_id");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at");

                    b.HasKey("FollowerId", "FollowedId")
                        .HasName("pk_follows");

                    b.HasIndex("FollowedId")
                        .HasDatabaseName("ix_follows_followed_id");

                    b.ToTable("follows", null, t =>
                        {
                            t.HasCheckConstraint("ck_follows_not_self", "follower_id <> followed_id");
                        });
                });

            modelBuilder.Entity("Maratona.Api.Data.Entities.JobRun", b =>
                {
                    b.Property<string>("Name")
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)")
                        .HasColumnName("name");

                    b.Property<DateTime>("LastRunAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("last_run_at");

                    b.HasKey("Name")
                        .HasName("pk_job_runs");

                    b.ToTable("job_runs", (string)null);
                });

            modelBuilder.Entity("Maratona.Api.Data.Entities.LibraryEntry", b =>
                {
                    b.Property<int>("UserId")
                        .HasColumnType("integer")
                        .HasColumnName("user_id");

                    b.Property<int>("TitleId")
                        .HasColumnType("integer")
                        .HasColumnName("title_id");

                    b.Property<DateTime>("AddedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("added_at");

                    b.Property<bool>("InWatchlist")
                        .HasColumnType("boolean")
                        .HasColumnName("in_watchlist");

                    b.Property<decimal?>("Rating")
                        .HasPrecision(3, 1)
                        .HasColumnType("numeric(3,1)")
                        .HasColumnName("rating");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("updated_at");

                    b.Property<bool>("Watched")
                        .HasColumnType("boolean")
                        .HasColumnName("watched");

                    b.Property<DateTime?>("WatchedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("watched_at");

                    b.HasKey("UserId", "TitleId")
                        .HasName("pk_library_entries");

                    b.HasIndex("TitleId")
                        .HasDatabaseName("ix_library_entries_title_id");

                    b.ToTable("library_entries", null, t =>
                        {
                            t.HasCheckConstraint("ck_library_entries_rating", "rating IS NULL OR (rating >= 0 AND rating <= 10 AND rating * 2 = trunc(rating * 2))");
                        });
                });

            modelBuilder.Entity("Maratona.Api.Data.Entities.ListItem", b =>
                {
                    b.Property<int>("ListId")
                        .HasColumnType("integer")
                        .HasColumnName("list_id");

                    b.Property<int>("TitleId")
                        .HasColumnType("integer")
                        .HasColumnName("title_id");

                    b.Property<DateTime>("AddedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("added_at");

                    b.Property<int>("Position")
                        .HasColumnType("integer")
                        .HasColumnName("position");

                    b.HasKey("ListId", "TitleId")
                        .HasName("pk_list_items");

                    b.HasIndex("TitleId")
                        .HasDatabaseName("ix_list_items_title_id");

                    b.ToTable("list_items", (string)null);
                });

            modelBuilder.Entity("Maratona.Api.Data.Entities.OmdbQueueItem", b =>
                {
                    b.Property<int>("TitleId")
                        .HasColumnType("integer")
                        .HasColumnName("title_id");

                    b.Property<DateTime>("LastRequestedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("last_requested_at");

                    b.Property<int>("RequestCount")
                        .HasColumnType("integer")
                        .HasColumnName("request_count");

                    b.HasKey("TitleId")
                        .HasName("pk_omdb_queue");

                    b.HasIndex("RequestCount")
                        .HasDatabaseName("ix_omdb_queue_request_count");

                    b.ToTable("omdb_queue", (string)null);
                });

            modelBuilder.Entity("Maratona.Api.Data.Entities.PrivateNote", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Body")
                        .IsRequired()
                        .HasMaxLength(20000)
                        .HasColumnType("character varying(20000)")
                        .HasColumnName("body");

                    b.Property<int?>("EpisodeId")
                        .HasColumnType("integer")
                        .HasColumnName("episode_id");

                    b.Property<int?>("SeasonId")
                        .HasColumnType("integer")
                        .HasColumnName("season_id");

                    b.Property<int>("TitleId")
                        .HasColumnType("integer")
                        .HasColumnName("title_id");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("updated_at");

                    b.Property<int>("UserId")
                        .HasColumnType("integer")
                        .HasColumnName("user_id");

                    b.HasKey("Id")
                        .HasName("pk_private_notes");

                    b.HasIndex("EpisodeId")
                        .HasDatabaseName("ix_private_notes_episode_id");

                    b.HasIndex("SeasonId")
                        .HasDatabaseName("ix_private_notes_season_id");

                    b.HasIndex("TitleId")
                        .HasDatabaseName("ix_private_notes_title_id");

                    b.HasIndex("UserId", "TitleId", "SeasonId", "EpisodeId")
                        .IsUnique()
                        .HasDatabaseName("ix_private_notes_user_id_title_id_season_id_episode_id");

                    NpgsqlIndexBuilderExtensions.AreNullsDistinct(b.HasIndex("UserId", "TitleId", "SeasonId", "EpisodeId"), false);

                    b.ToTable("private_notes", (string)null);
                });

            modelBuilder.Entity("Maratona.Api.Data.Entities.Review", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Body")
                        .IsRequired()
                        .HasMaxLength(10000)
                        .HasColumnType("character varying(10000)")
                        .HasColumnName("body");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at");

                    b.Property<int?>("SeasonId")
                        .HasColumnType("integer")
                        .HasColumnName("season_id");

                    b.Property<bool>("Spoiler")
                        .HasColumnType("boolean")
                        .HasColumnName("spoiler");

                    b.Property<int>("TitleId")
                        .HasColumnType("integer")
                        .HasColumnName("title_id");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("updated_at");

                    b.Property<int>("UserId")
                        .HasColumnType("integer")
                        .HasColumnName("user_id");

                    b.HasKey("Id")
                        .HasName("pk_reviews");

                    b.HasIndex("CreatedAt")
                        .HasDatabaseName("ix_reviews_created_at");

                    b.HasIndex("SeasonId")
                        .HasDatabaseName("ix_reviews_season_id");

                    b.HasIndex("TitleId")
                        .HasDatabaseName("ix_reviews_title_id");

                    b.HasIndex("UserId", "TitleId", "SeasonId")
                        .IsUnique()
                        .HasDatabaseName("ix_reviews_user_id_title_id_season_id");

                    NpgsqlIndexBuilderExtensions.AreNullsDistinct(b.HasIndex("UserId", "TitleId", "SeasonId"), false);

                    b.ToTable("reviews", (string)null);
                });

            modelBuilder.Entity("Maratona.Api.Data.Entities.ReviewLike", b =>
                {
                    b.Property<int>("UserId")
                        .HasColumnType("integer")
                        .HasColumnName("user_id");

                    b.Property<int>("ReviewId")
                        .HasColumnType("integer")
                        .HasColumnName("review_id");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at");

                    b.HasKey("UserId", "ReviewId")
                        .HasName("pk_review_likes");

                    b.HasIndex("ReviewId")
                        .HasDatabaseName("ix_review_likes_review_id");

                    b.ToTable("review_likes", (string)null);
                });

            modelBuilder.Entity("Maratona.Api.Data.Entities.Season", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<DateOnly?>("AirDate")
                        .HasColumnType("date")
                        .HasColumnName("air_date");

                    b.Property<DateTime?>("AtualizadoEm")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("atualizado_em");

                    b.Property<int>("EpisodeCount")
                        .HasColumnType("integer")
                        .HasColumnName("episode_count");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.Property<string>("Overview")
                        .HasColumnType("text")
                        .HasColumnName("overview");

                    b.Property<string>("PosterPath")
                        .HasColumnType("text")
                        .HasColumnName("poster_path");

                    b.Property<int>("SeasonNumber")
                        .HasColumnType("integer")
                        .HasColumnName("season_number");

                    b.Property<int>("TitleId")
                        .HasColumnType("integer")
                        .HasColumnName("title_id");

                    b.HasKey("Id")
                        .HasName("pk_seasons");

                    b.HasIndex("TitleId", "SeasonNumber")
                        .IsUnique()
                        .HasDatabaseName("ix_seasons_title_id_season_number");

                    b.ToTable("seasons", (string)null);
                });

            modelBuilder.Entity("Maratona.Api.Data.Entities.SeasonEntry", b =>
                {
                    b.Property<int>("UserId")
                        .HasColumnType("integer")
                        .HasColumnName("user_id");

                    b.Property<int>("SeasonId")
                        .HasColumnType("integer")
                        .HasColumnName("season_id");

                    b.Property<decimal?>("Rating")
                        .HasPrecision(3, 1)
                        .HasColumnType("numeric(3,1)")
                        .HasColumnName("rating");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("updated_at");

                    b.Property<bool>("Watched")
                        .HasColumnType("boolean")
                        .HasColumnName("watched");

                    b.Property<DateTime?>("WatchedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("watched_at");

                    b.HasKey("UserId", "SeasonId")
                        .HasName("pk_season_entries");

                    b.HasIndex("SeasonId")
                        .HasDatabaseName("ix_season_entries_season_id");

                    b.ToTable("season_entries", null, t =>
                        {
                            t.HasCheckConstraint("ck_season_entries_rating", "rating IS NULL OR (rating >= 0 AND rating <= 10 AND rating * 2 = trunc(rating * 2))");
                        });
                });

            modelBuilder.Entity("Maratona.Api.Data.Entities.Title", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("AtualizadoEm")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("atualizado_em");

                    b.Property<DateTime?>("AwardsCheckedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("awards_checked_at");

                    b.Property<string>("BackdropPath")
                        .HasColumnType("text")
                        .HasColumnName("backdrop_path");

                    b.PrimitiveCollection<List<string>>("Genres")
                        .IsRequired()
                        .HasColumnType("text[]")
                        .HasColumnName("genres");

                    b.Property<string>("ImdbId")
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)")
                        .HasColumnName("imdb_id");

                    b.Property<DateOnly?>("LastAirDate")
                        .HasColumnType("date")
                        .HasColumnName("last_air_date");

                    b.Property<string>("MediaType")
                        .IsRequired()
                        .HasMaxLength(10)
                        .HasColumnType("character varying(10)")
                        .HasColumnName("media_type");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(300)
                        .HasColumnType("character varying(300)")
                        .HasColumnName("name");

                    b.Property<string>("OriginalName")
                        .HasMaxLength(300)
                        .HasColumnType("character varying(300)")
                        .HasColumnName("original_name");

                    b.Property<string>("Overview")
                        .HasColumnType("text")
                        .HasColumnName("overview");

                    b.Property<double>("Popularity")
                        .HasColumnType("double precision")
                        .HasColumnName("popularity");

                    b.Property<string>("PosterPath")
                        .HasColumnType("text")
                        .HasColumnName("poster_path");

                    b.Property<DateOnly?>("ReleaseDate")
                        .HasColumnType("date")
                        .HasColumnName("release_date");

                    b.Property<int?>("Runtime")
                        .HasColumnType("integer")
                        .HasColumnName("runtime");

                    b.Property<string>("Status")
                        .HasMaxLength(40)
                        .HasColumnType("character varying(40)")
                        .HasColumnName("status");

                    b.Property<int>("TmdbId")
                        .HasColumnType("integer")
                        .HasColumnName("tmdb_id");

                    b.HasKey("Id")
                        .HasName("pk_titles");

                    b.HasIndex("AtualizadoEm")
                        .HasDatabaseName("ix_titles_atualizado_em");

                    b.HasIndex("ImdbId")
                        .HasDatabaseName("ix_titles_imdb_id");

                    b.HasIndex("Popularity")
                        .HasDatabaseName("ix_titles_popularity");

                    b.HasIndex("TmdbId", "MediaType")
                        .IsUnique()
                        .HasDatabaseName("ix_titles_tmdb_id_media_type");

                    b.ToTable("titles", (string)null);
                });

            modelBuilder.Entity("Maratona.Api.Data.Entities.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("AvatarUrl")
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)")
                        .HasColumnName("avatar_url");

                    b.Property<string>("Bio")
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)")
                        .HasColumnName("bio");

                    b.Property<string>("CoverUrl")
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)")
                        .HasColumnName("cover_url");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at");

                    b.Property<string>("DisplayName")
                        .IsRequired()
                        .HasMaxLength(60)
                        .HasColumnType("character varying(60)")
                        .HasColumnName("display_name");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(254)
                        .HasColumnType("character varying(254)")
                        .HasColumnName("email");

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("password_hash");

                    b.Property<string>("RatingStyle")
                        .IsRequired()
                        .HasMaxLength(10)
                        .HasColumnType("character varying(10)")
                        .HasColumnName("rating_style");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("character varying(30)")
                        .HasColumnName("username");

                    b.HasKey("Id")
                        .HasName("pk_users");

                    b.HasIndex("Email")
                        .IsUnique()
                        .HasDatabaseName("ix_users_email");

                    b.HasIndex("Username")
                        .IsUnique()
                        .HasDatabaseName("ix_users_username");

                    b.ToTable("users", (string)null);
                });

            modelBuilder.Entity("Maratona.Api.Data.Entities.UserList", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at");

                    b.Property<string>("Description")
                        .HasMaxLength(1000)
                        .HasColumnType("character varying(1000)")
                        .HasColumnName("description");

                    b.Property<bool>("IsPublic")
                        .HasColumnType("boolean")
                        .HasColumnName("is_public");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)")
                        .HasColumnName("name");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("updated_at");

                    b.Property<int>("UserId")
                        .HasColumnType("integer")
                        .HasColumnName("user_id");

                    b.HasKey("Id")
                        .HasName("pk_lists");

                    b.HasIndex("UserId")
                        .HasDatabaseName("ix_lists_user_id");

                    b.ToTable("lists", (string)null);
                });

            modelBuilder.Entity("Maratona.Api.Data.Entities.Episode", b =>
                {
                    b.HasOne("Maratona.Api.Data.Entities.Season", "Season")
                        .WithMany("Episodes")
                        .HasForeignKey("SeasonId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_episodes_seasons_season_id");

                    b.Navigation("Season");
                });

            modelBuilder.Entity("Maratona.Api.Data.Entities.ExternalRating", b =>
                {
                    b.HasOne("Maratona.Api.Data.Entities.Title", "Title")
                        .WithOne("ExternalRating")
                        .HasForeignKey("Maratona.Api.Data.Entities.ExternalRating", "TitleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_external_ratings_titles_title_id");

                    b.Navigation("Title");
                });

            modelBuilder.Entity("Maratona.Api.Data.Entities.Favorite", b =>
                {
                    b.HasOne("Maratona.Api.Data.Entities.Title", "Title")
                        .WithMany()
                        .HasForeignKey("TitleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_favorites_titles_title_id");

                    b.HasOne("Maratona.Api.Data.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_favorites_users_user_id");

                    b.Navigation("Title");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Maratona.Api.Data.Entities.Follow", b =>
                {
                    b.HasOne("Maratona.Api.Data.Entities.User", "Followed")
                        .WithMany()
                        .HasForeignKey("FollowedId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_follows_users_followed_id");

                    b.HasOne("Maratona.Api.Data.Entities.User", "Follower")
                        .WithMany()
                        .HasForeignKey("FollowerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_follows_users_follower_id");

                    b.Navigation("Followed");

                    b.Navigation("Follower");
                });

            modelBuilder.Entity("Maratona.Api.Data.Entities.LibraryEntry", b =>
                {
                    b.HasOne("Maratona.Api.Data.Entities.Title", "Title")
                        .WithMany()
                        .HasForeignKey("TitleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_library_entries_titles_title_id");

                    b.HasOne("Maratona.Api.Data.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_library_entries_users_user_id");

                    b.Navigation("Title");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Maratona.Api.Data.Entities.ListItem", b =>
                {
                    b.HasOne("Maratona.Api.Data.Entities.UserList", "List")
                        .WithMany("Items")
                        .HasForeignKey("ListId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_list_items_lists_list_id");

                    b.HasOne("Maratona.Api.Data.Entities.Title", "Title")
                        .WithMany()
                        .HasForeignKey("TitleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_list_items_titles_title_id");

                    b.Navigation("List");

                    b.Navigation("Title");
                });

            modelBuilder.Entity("Maratona.Api.Data.Entities.OmdbQueueItem", b =>
                {
                    b.HasOne("Maratona.Api.Data.Entities.Title", "Title")
                        .WithMany()
                        .HasForeignKey("TitleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_omdb_queue_titles_title_id");

                    b.Navigation("Title");
                });

            modelBuilder.Entity("Maratona.Api.Data.Entities.PrivateNote", b =>
                {
                    b.HasOne("Maratona.Api.Data.Entities.Episode", "Episode")
                        .WithMany()
                        .HasForeignKey("EpisodeId")
                        .HasConstraintName("fk_private_notes_episodes_episode_id");

                    b.HasOne("Maratona.Api.Data.Entities.Season", "Season")
                        .WithMany()
                        .HasForeignKey("SeasonId")
                        .HasConstraintName("fk_private_notes_seasons_season_id");

                    b.HasOne("Maratona.Api.Data.Entities.Title", "Title")
                        .WithMany()
                        .HasForeignKey("TitleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_private_notes_titles_title_id");

                    b.HasOne("Maratona.Api.Data.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_private_notes_users_user_id");

                    b.Navigation("Episode");

                    b.Navigation("Season");

                    b.Navigation("Title");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Maratona.Api.Data.Entities.Review", b =>
                {
                    b.HasOne("Maratona.Api.Data.Entities.Season", "Season")
                        .WithMany()
                        .HasForeignKey("SeasonId")
                        .HasConstraintName("fk_reviews_seasons_season_id");

                    b.HasOne("Maratona.Api.Data.Entities.Title", "Title")
                        .WithMany()
                        .HasForeignKey("TitleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_reviews_titles_title_id");

                    b.HasOne("Maratona.Api.Data.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_reviews_users_user_id");

                    b.Navigation("Season");

                    b.Navigation("Title");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Maratona.Api.Data.Entities.ReviewLike", b =>
                {
                    b.HasOne("Maratona.Api.Data.Entities.Review", "Review")
                        .WithMany("Likes")
                        .HasForeignKey("ReviewId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_review_likes_reviews_review_id");

                    b.HasOne("Maratona.Api.Data.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_review_likes_users_user_id");

                    b.Navigation("Review");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Maratona.Api.Data.Entities.Season", b =>
                {
                    b.HasOne("Maratona.Api.Data.Entities.Title", "Title")
                        .WithMany("Seasons")
                        .HasForeignKey("TitleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_seasons_titles_title_id");

                    b.Navigation("Title");
                });

            modelBuilder.Entity("Maratona.Api.Data.Entities.SeasonEntry", b =>
                {
                    b.HasOne("Maratona.Api.Data.Entities.Season", "Season")
                        .WithMany()
                        .HasForeignKey("SeasonId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_season_entries_seasons_season_id");

                    b.HasOne("Maratona.Api.Data.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_season_entries_users_user_id");

                    b.Navigation("Season");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Maratona.Api.Data.Entities.Title", b =>
                {
                    b.OwnsOne("Maratona.Api.Data.Entities.Credits", "Credits", b1 =>
                        {
                            b1.Property<int>("TitleId");

                            b1.PrimitiveCollection<string>("Creators")
                                .IsRequired();

                            b1.PrimitiveCollection<string>("Directors")
                                .IsRequired();

                            b1.PrimitiveCollection<string>("Writers")
                                .IsRequired();

                            b1.HasKey("TitleId");

                            b1.ToTable("titles");

                            b1
                                .ToJson("credits")
                                .HasColumnType("jsonb");

                            b1.WithOwner()
                                .HasForeignKey("TitleId")
                                .HasConstraintName("fk_titles_titles_id");

                            b1.OwnsMany("Maratona.Api.Data.Entities.CastMember", "Cast", b2 =>
                                {
                                    b2.Property<int>("CreditsTitleId");

                                    b2.Property<int>("__synthesizedOrdinal")
                                        .ValueGeneratedOnAdd();

                                    b2.Property<string>("Character");

                                    b2.Property<string>("Name")
                                        .IsRequired();

                                    b2.Property<string>("ProfilePath");

                                    b2.HasKey("CreditsTitleId", "__synthesizedOrdinal")
                                        .HasName("pk_titles");

                                    b2.ToTable("titles");

                                    b2.WithOwner()
                                        .HasForeignKey("CreditsTitleId")
                                        .HasConstraintName("fk_titles_titles_credits_title_id");
                                });

                            b1.Navigation("Cast");
                        });

                    b.Navigation("Credits")
                        .IsRequired();
                });

            modelBuilder.Entity("Maratona.Api.Data.Entities.UserList", b =>
                {
                    b.HasOne("Maratona.Api.Data.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_lists_users_user_id");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Maratona.Api.Data.Entities.Review", b =>
                {
                    b.Navigation("Likes");
                });

            modelBuilder.Entity("Maratona.Api.Data.Entities.Season", b =>
                {
                    b.Navigation("Episodes");
                });

            modelBuilder.Entity("Maratona.Api.Data.Entities.Title", b =>
                {
                    b.Navigation("ExternalRating");

                    b.Navigation("Seasons");
                });

            modelBuilder.Entity("Maratona.Api.Data.Entities.UserList", b =>
                {
                    b.Navigation("Items");
                });
#pragma warning restore 612, 618
        }
    }
}
