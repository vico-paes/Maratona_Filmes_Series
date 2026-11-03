using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Maratona.Api.Data.Migrations
{
    public partial class Inicial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "api_usage",
                columns: table => new
                {
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    bucket = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    calls = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_api_usage", x => new { x.date, x.bucket });
                });

            migrationBuilder.CreateTable(
                name: "job_runs",
                columns: table => new
                {
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    last_run_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_runs", x => x.name);
                });

            migrationBuilder.CreateTable(
                name: "titles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tmdb_id = table.Column<int>(type: "integer", nullable: false),
                    media_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    imdb_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    original_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    overview = table.Column<string>(type: "text", nullable: true),
                    poster_path = table.Column<string>(type: "text", nullable: true),
                    backdrop_path = table.Column<string>(type: "text", nullable: true),
                    release_date = table.Column<DateOnly>(type: "date", nullable: true),
                    last_air_date = table.Column<DateOnly>(type: "date", nullable: true),
                    runtime = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    popularity = table.Column<double>(type: "double precision", nullable: false),
                    genres = table.Column<List<string>>(type: "text[]", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    awards_checked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    credits = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_titles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    username = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    display_name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    bio = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    avatar_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    cover_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    rating_style = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "awards",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    title_id = table.Column<int>(type: "integer", nullable: false),
                    kind = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    category = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    person = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    won = table.Column<bool>(type: "boolean", nullable: false),
                    is_major = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_awards", x => x.id);
                    table.ForeignKey(
                        name: "fk_awards_titles_title_id",
                        column: x => x.title_id,
                        principalTable: "titles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "external_ratings",
                columns: table => new
                {
                    title_id = table.Column<int>(type: "integer", nullable: false),
                    imdb = table.Column<decimal>(type: "numeric(3,1)", precision: 3, scale: 1, nullable: true),
                    imdb_votes = table.Column<int>(type: "integer", nullable: true),
                    rotten_tomatoes = table.Column<int>(type: "integer", nullable: true),
                    metacritic = table.Column<int>(type: "integer", nullable: true),
                    not_found = table.Column<bool>(type: "boolean", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    next_refresh_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_external_ratings", x => x.title_id);
                    table.ForeignKey(
                        name: "fk_external_ratings_titles_title_id",
                        column: x => x.title_id,
                        principalTable: "titles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "omdb_queue",
                columns: table => new
                {
                    title_id = table.Column<int>(type: "integer", nullable: false),
                    request_count = table.Column<int>(type: "integer", nullable: false),
                    last_requested_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_omdb_queue", x => x.title_id);
                    table.ForeignKey(
                        name: "fk_omdb_queue_titles_title_id",
                        column: x => x.title_id,
                        principalTable: "titles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "seasons",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    title_id = table.Column<int>(type: "integer", nullable: false),
                    season_number = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    overview = table.Column<string>(type: "text", nullable: true),
                    poster_path = table.Column<string>(type: "text", nullable: true),
                    air_date = table.Column<DateOnly>(type: "date", nullable: true),
                    episode_count = table.Column<int>(type: "integer", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_seasons", x => x.id);
                    table.ForeignKey(
                        name: "fk_seasons_titles_title_id",
                        column: x => x.title_id,
                        principalTable: "titles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "favorites",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    media_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    slot = table.Column<int>(type: "integer", nullable: false),
                    title_id = table.Column<int>(type: "integer", nullable: false),
                    crown = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_favorites", x => new { x.user_id, x.media_type, x.slot });
                    table.CheckConstraint("ck_favorites_slot", "slot BETWEEN 1 AND 4");
                    table.ForeignKey(
                        name: "fk_favorites_titles_title_id",
                        column: x => x.title_id,
                        principalTable: "titles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_favorites_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "follows",
                columns: table => new
                {
                    follower_id = table.Column<int>(type: "integer", nullable: false),
                    followed_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_follows", x => new { x.follower_id, x.followed_id });
                    table.CheckConstraint("ck_follows_not_self", "follower_id <> followed_id");
                    table.ForeignKey(
                        name: "fk_follows_users_followed_id",
                        column: x => x.followed_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_follows_users_follower_id",
                        column: x => x.follower_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "library_entries",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    title_id = table.Column<int>(type: "integer", nullable: false),
                    in_watchlist = table.Column<bool>(type: "boolean", nullable: false),
                    watched = table.Column<bool>(type: "boolean", nullable: false),
                    watched_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rating = table.Column<decimal>(type: "numeric(3,1)", precision: 3, scale: 1, nullable: true),
                    added_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_library_entries", x => new { x.user_id, x.title_id });
                    table.CheckConstraint("ck_library_entries_rating", "rating IS NULL OR (rating >= 0 AND rating <= 10 AND rating * 2 = trunc(rating * 2))");
                    table.ForeignKey(
                        name: "fk_library_entries_titles_title_id",
                        column: x => x.title_id,
                        principalTable: "titles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_library_entries_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lists",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_public = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lists", x => x.id);
                    table.ForeignKey(
                        name: "fk_lists_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "episodes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    season_id = table.Column<int>(type: "integer", nullable: false),
                    episode_number = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    overview = table.Column<string>(type: "text", nullable: true),
                    still_path = table.Column<string>(type: "text", nullable: true),
                    air_date = table.Column<DateOnly>(type: "date", nullable: true),
                    runtime = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_episodes", x => x.id);
                    table.ForeignKey(
                        name: "fk_episodes_seasons_season_id",
                        column: x => x.season_id,
                        principalTable: "seasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reviews",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    title_id = table.Column<int>(type: "integer", nullable: false),
                    season_id = table.Column<int>(type: "integer", nullable: true),
                    body = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    spoiler = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reviews", x => x.id);
                    table.ForeignKey(
                        name: "fk_reviews_seasons_season_id",
                        column: x => x.season_id,
                        principalTable: "seasons",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_reviews_titles_title_id",
                        column: x => x.title_id,
                        principalTable: "titles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_reviews_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "season_entries",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    season_id = table.Column<int>(type: "integer", nullable: false),
                    watched = table.Column<bool>(type: "boolean", nullable: false),
                    watched_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rating = table.Column<decimal>(type: "numeric(3,1)", precision: 3, scale: 1, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_season_entries", x => new { x.user_id, x.season_id });
                    table.CheckConstraint("ck_season_entries_rating", "rating IS NULL OR (rating >= 0 AND rating <= 10 AND rating * 2 = trunc(rating * 2))");
                    table.ForeignKey(
                        name: "fk_season_entries_seasons_season_id",
                        column: x => x.season_id,
                        principalTable: "seasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_season_entries_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "list_items",
                columns: table => new
                {
                    list_id = table.Column<int>(type: "integer", nullable: false),
                    title_id = table.Column<int>(type: "integer", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    added_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_list_items", x => new { x.list_id, x.title_id });
                    table.ForeignKey(
                        name: "fk_list_items_lists_list_id",
                        column: x => x.list_id,
                        principalTable: "lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_list_items_titles_title_id",
                        column: x => x.title_id,
                        principalTable: "titles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "private_notes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    title_id = table.Column<int>(type: "integer", nullable: false),
                    season_id = table.Column<int>(type: "integer", nullable: true),
                    episode_id = table.Column<int>(type: "integer", nullable: true),
                    body = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_private_notes", x => x.id);
                    table.ForeignKey(
                        name: "fk_private_notes_episodes_episode_id",
                        column: x => x.episode_id,
                        principalTable: "episodes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_private_notes_seasons_season_id",
                        column: x => x.season_id,
                        principalTable: "seasons",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_private_notes_titles_title_id",
                        column: x => x.title_id,
                        principalTable: "titles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_private_notes_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "review_likes",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    review_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_review_likes", x => new { x.user_id, x.review_id });
                    table.ForeignKey(
                        name: "fk_review_likes_reviews_review_id",
                        column: x => x.review_id,
                        principalTable: "reviews",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_review_likes_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_awards_title_id",
                table: "awards",
                column: "title_id");

            migrationBuilder.CreateIndex(
                name: "ix_episodes_season_id_episode_number",
                table: "episodes",
                columns: new[] { "season_id", "episode_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_external_ratings_next_refresh_at",
                table: "external_ratings",
                column: "next_refresh_at");

            migrationBuilder.CreateIndex(
                name: "ix_favorites_title_id",
                table: "favorites",
                column: "title_id");

            migrationBuilder.CreateIndex(
                name: "ix_favorites_user_id_media_type",
                table: "favorites",
                columns: new[] { "user_id", "media_type" },
                unique: true,
                filter: "crown");

            migrationBuilder.CreateIndex(
                name: "ix_favorites_user_id_title_id",
                table: "favorites",
                columns: new[] { "user_id", "title_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_follows_followed_id",
                table: "follows",
                column: "followed_id");

            migrationBuilder.CreateIndex(
                name: "ix_library_entries_title_id",
                table: "library_entries",
                column: "title_id");

            migrationBuilder.CreateIndex(
                name: "ix_list_items_title_id",
                table: "list_items",
                column: "title_id");

            migrationBuilder.CreateIndex(
                name: "ix_lists_user_id",
                table: "lists",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_omdb_queue_request_count",
                table: "omdb_queue",
                column: "request_count");

            migrationBuilder.CreateIndex(
                name: "ix_private_notes_episode_id",
                table: "private_notes",
                column: "episode_id");

            migrationBuilder.CreateIndex(
                name: "ix_private_notes_season_id",
                table: "private_notes",
                column: "season_id");

            migrationBuilder.CreateIndex(
                name: "ix_private_notes_title_id",
                table: "private_notes",
                column: "title_id");

            migrationBuilder.CreateIndex(
                name: "ix_private_notes_user_id_title_id_season_id_episode_id",
                table: "private_notes",
                columns: new[] { "user_id", "title_id", "season_id", "episode_id" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "ix_review_likes_review_id",
                table: "review_likes",
                column: "review_id");

            migrationBuilder.CreateIndex(
                name: "ix_reviews_created_at",
                table: "reviews",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_reviews_season_id",
                table: "reviews",
                column: "season_id");

            migrationBuilder.CreateIndex(
                name: "ix_reviews_title_id",
                table: "reviews",
                column: "title_id");

            migrationBuilder.CreateIndex(
                name: "ix_reviews_user_id_title_id_season_id",
                table: "reviews",
                columns: new[] { "user_id", "title_id", "season_id" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "ix_season_entries_season_id",
                table: "season_entries",
                column: "season_id");

            migrationBuilder.CreateIndex(
                name: "ix_seasons_title_id_season_number",
                table: "seasons",
                columns: new[] { "title_id", "season_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_titles_atualizado_em",
                table: "titles",
                column: "atualizado_em");

            migrationBuilder.CreateIndex(
                name: "ix_titles_imdb_id",
                table: "titles",
                column: "imdb_id");

            migrationBuilder.CreateIndex(
                name: "ix_titles_popularity",
                table: "titles",
                column: "popularity");

            migrationBuilder.CreateIndex(
                name: "ix_titles_tmdb_id_media_type",
                table: "titles",
                columns: new[] { "tmdb_id", "media_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_username",
                table: "users",
                column: "username",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "api_usage");

            migrationBuilder.DropTable(
                name: "awards");

            migrationBuilder.DropTable(
                name: "external_ratings");

            migrationBuilder.DropTable(
                name: "favorites");

            migrationBuilder.DropTable(
                name: "follows");

            migrationBuilder.DropTable(
                name: "job_runs");

            migrationBuilder.DropTable(
                name: "library_entries");

            migrationBuilder.DropTable(
                name: "list_items");

            migrationBuilder.DropTable(
                name: "omdb_queue");

            migrationBuilder.DropTable(
                name: "private_notes");

            migrationBuilder.DropTable(
                name: "review_likes");

            migrationBuilder.DropTable(
                name: "season_entries");

            migrationBuilder.DropTable(
                name: "lists");

            migrationBuilder.DropTable(
                name: "episodes");

            migrationBuilder.DropTable(
                name: "reviews");

            migrationBuilder.DropTable(
                name: "seasons");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "titles");
        }
    }
}
