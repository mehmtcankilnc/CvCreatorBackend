using CvCreator.Domain.Models;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CvCreator.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ResumeCoverLetterValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ResumeFormValuesModel>(
                name: "ResumeFormValues",
                table: "Resumes",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<CoverLetterFormValuesModel>(
                name: "CoverLetterFormValues",
                table: "CoverLetters",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResumeFormValues",
                table: "Resumes");

            migrationBuilder.DropColumn(
                name: "CoverLetterFormValues",
                table: "CoverLetters");
        }
    }
}
