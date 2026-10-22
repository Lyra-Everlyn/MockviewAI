using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MockviewAI.Migrations
{
    /// <inheritdoc />
    public partial class AddAnxietyIndicatorAndAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AnxietyIndicator",
                table: "InterviewSessions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailResult",
                table: "InterviewSessions",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Attempts",
                table: "SessionAnswers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "LongestPauseSeconds",
                table: "SessionAnswers",
                type: "double",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "AnxietyIndicator", table: "InterviewSessions");
            migrationBuilder.DropColumn(name: "EmailResult", table: "InterviewSessions");
            migrationBuilder.DropColumn(name: "Attempts", table: "SessionAnswers");
            migrationBuilder.DropColumn(name: "LongestPauseSeconds", table: "SessionAnswers");
        }
    }
}
