using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MockviewAI.Migrations
{
    /// <inheritdoc />
    public partial class AddLiveModeVoiceAndFollowUps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FollowUpOf",
                table: "SessionAnswers",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "FollowUpPointId",
                table: "SessionAnswers",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<double>(
                name: "PitchVariation",
                table: "SessionAnswers",
                type: "double",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "QuietRatio",
                table: "SessionAnswers",
                type: "double",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "VoiceLevelDb",
                table: "SessionAnswers",
                type: "double",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Mode",
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
                name: "FollowUpOf",
                table: "SessionAnswers");

            migrationBuilder.DropColumn(
                name: "FollowUpPointId",
                table: "SessionAnswers");

            migrationBuilder.DropColumn(
                name: "PitchVariation",
                table: "SessionAnswers");

            migrationBuilder.DropColumn(
                name: "QuietRatio",
                table: "SessionAnswers");

            migrationBuilder.DropColumn(
                name: "VoiceLevelDb",
                table: "SessionAnswers");

            migrationBuilder.DropColumn(
                name: "Mode",
                table: "InterviewSessions");
        }
    }
}
