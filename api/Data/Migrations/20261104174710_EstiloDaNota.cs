using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maratona.Api.Data.Migrations
{
    public partial class EstiloDaNota : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_season_entries_rating",
                table: "season_entries");

            migrationBuilder.DropCheckConstraint(
                name: "ck_library_entries_rating",
                table: "library_entries");

            migrationBuilder.AddColumn<string>(
                name: "rating_style",
                table: "season_entries",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rating_style",
                table: "library_entries",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_season_entries_rating",
                table: "season_entries",
                sql: "(rating IS NULL AND rating_style IS NULL) OR (rating * 2 = trunc(rating * 2) AND (\n    (rating_style = 'Numeric' AND rating BETWEEN 0 AND 10) OR\n    (rating_style = 'Stars' AND rating BETWEEN 0.5 AND 5)))");

            migrationBuilder.AddCheckConstraint(
                name: "ck_library_entries_rating",
                table: "library_entries",
                sql: "(rating IS NULL AND rating_style IS NULL) OR (rating * 2 = trunc(rating * 2) AND (\n    (rating_style = 'Numeric' AND rating BETWEEN 0 AND 10) OR\n    (rating_style = 'Stars' AND rating BETWEEN 0.5 AND 5)))");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_season_entries_rating",
                table: "season_entries");

            migrationBuilder.DropCheckConstraint(
                name: "ck_library_entries_rating",
                table: "library_entries");

            migrationBuilder.DropColumn(
                name: "rating_style",
                table: "season_entries");

            migrationBuilder.DropColumn(
                name: "rating_style",
                table: "library_entries");

            migrationBuilder.AddCheckConstraint(
                name: "ck_season_entries_rating",
                table: "season_entries",
                sql: "rating IS NULL OR (rating >= 0 AND rating <= 10 AND rating * 2 = trunc(rating * 2))");

            migrationBuilder.AddCheckConstraint(
                name: "ck_library_entries_rating",
                table: "library_entries",
                sql: "rating IS NULL OR (rating >= 0 AND rating <= 10 AND rating * 2 = trunc(rating * 2))");
        }
    }
}
