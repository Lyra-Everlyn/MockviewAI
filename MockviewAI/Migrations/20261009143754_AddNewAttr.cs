using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MockviewAI.Migrations
{
    /// <inheritdoc />
    public partial class AddNewAttr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExperienceLevel",
                table: "Users",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "GraduationStatus",
                table: "Users",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "IsOnboardingCompleted",
                table: "Users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExperienceLevel",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "GraduationStatus",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsOnboardingCompleted",
                table: "Users");
        }
    }
}
