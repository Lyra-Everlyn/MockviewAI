using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MockviewAI.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionBankAnswerCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "SessionAnswers",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CorePointsMet",
                table: "SessionAnswers",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CorePointsMissing",
                table: "SessionAnswers",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Feedback",
                table: "SessionAnswers",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "MatchedPath",
                table: "SessionAnswers",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "QuestionId",
                table: "SessionAnswers",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "QuestionBankVersion",
                table: "InterviewSessions",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ScoringMode",
                table: "InterviewSessions",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "SessionAnswers");

            migrationBuilder.DropColumn(
                name: "CorePointsMet",
                table: "SessionAnswers");

            migrationBuilder.DropColumn(
                name: "CorePointsMissing",
                table: "SessionAnswers");

            migrationBuilder.DropColumn(
                name: "Feedback",
                table: "SessionAnswers");

            migrationBuilder.DropColumn(
                name: "MatchedPath",
                table: "SessionAnswers");

            migrationBuilder.DropColumn(
                name: "QuestionId",
                table: "SessionAnswers");

            migrationBuilder.DropColumn(
                name: "QuestionBankVersion",
                table: "InterviewSessions");

            migrationBuilder.DropColumn(
                name: "ScoringMode",
                table: "InterviewSessions");
        }
    }
}
