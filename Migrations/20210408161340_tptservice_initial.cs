using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace TptMain.Migrations
{
    public partial class tptservice_initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PreviewJobs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DateSubmitted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateStarted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateCompleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateCancelled = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProjectName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    User = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsError = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorDetail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FontSizeInPts = table.Column<float>(type: "real", nullable: true),
                    FontLeadingInPts = table.Column<float>(type: "real", nullable: true),
                    PageWidthInPts = table.Column<float>(type: "real", nullable: true),
                    PageHeightInPts = table.Column<float>(type: "real", nullable: true),
                    PageHeaderInPts = table.Column<float>(type: "real", nullable: true),
                    BookFormat = table.Column<int>(type: "int", nullable: true),
                    UseCustomFootnotes = table.Column<bool>(type: "bit", nullable: false),
                    UseProjectFont = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreviewJobs", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PreviewJobs");
        }
    }
}
