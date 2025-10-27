using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CvCreator.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStoragePathToResumes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StoragePath",
                table: "Resumes",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StoragePath",
                table: "Resumes");
        }
    }
}
