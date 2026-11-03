using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maratona.Api.Data.Migrations
{
    public partial class Premios : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_awards_titles_title_id",
                table: "awards");

            migrationBuilder.DropIndex(
                name: "ix_awards_title_id",
                table: "awards");

            migrationBuilder.DropColumn(
                name: "title_id",
                table: "awards");

            migrationBuilder.AddColumn<string>(
                name: "awards_text",
                table: "external_ratings",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "emmy_nominations",
                table: "external_ratings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "emmy_wins",
                table: "external_ratings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "oscar_nominations",
                table: "external_ratings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "oscar_wins",
                table: "external_ratings",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "year",
                table: "awards",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "imdb_id",
                table: "awards",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "source",
                table: "awards",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_awards_imdb_id",
                table: "awards",
                column: "imdb_id");

            migrationBuilder.CreateIndex(
                name: "ix_awards_source",
                table: "awards",
                column: "source");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_awards_imdb_id",
                table: "awards");

            migrationBuilder.DropIndex(
                name: "ix_awards_source",
                table: "awards");

            migrationBuilder.DropColumn(
                name: "awards_text",
                table: "external_ratings");

            migrationBuilder.DropColumn(
                name: "emmy_nominations",
                table: "external_ratings");

            migrationBuilder.DropColumn(
                name: "emmy_wins",
                table: "external_ratings");

            migrationBuilder.DropColumn(
                name: "oscar_nominations",
                table: "external_ratings");

            migrationBuilder.DropColumn(
                name: "oscar_wins",
                table: "external_ratings");

            migrationBuilder.DropColumn(
                name: "imdb_id",
                table: "awards");

            migrationBuilder.DropColumn(
                name: "source",
                table: "awards");

            migrationBuilder.AlterColumn<int>(
                name: "year",
                table: "awards",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "title_id",
                table: "awards",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_awards_title_id",
                table: "awards",
                column: "title_id");

            migrationBuilder.AddForeignKey(
                name: "fk_awards_titles_title_id",
                table: "awards",
                column: "title_id",
                principalTable: "titles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
