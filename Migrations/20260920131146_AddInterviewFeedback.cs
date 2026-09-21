using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AltriumRecruitmentSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddInterviewFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FeedbackSubmittedOn",
                table: "Interviews",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InterviewerFeedback",
                table: "Interviews",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Outcome",
                table: "Interviews",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FeedbackSubmittedOn",
                table: "Interviews");

            migrationBuilder.DropColumn(
                name: "InterviewerFeedback",
                table: "Interviews");

            migrationBuilder.DropColumn(
                name: "Outcome",
                table: "Interviews");
        }
    }
}
