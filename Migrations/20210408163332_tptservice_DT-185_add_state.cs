using Microsoft.EntityFrameworkCore.Migrations;

namespace TptMain.Migrations
{
    public partial class tptservice_DT185_add_state : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "State",
                table: "PreviewJobs",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "State",
                table: "PreviewJobs");
        }
    }
}
