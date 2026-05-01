using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NodeScope.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ImportJobFailureMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "failure_message",
                table: "import_jobs",
                type: "character varying(16384)",
                maxLength: 16384,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "failure_message",
                table: "import_jobs");
        }
    }
}
