using Microsoft.EntityFrameworkCore.Migrations;

namespace TptMain.Migrations
{
    public partial class tptservice_DT185_add_bibleselectionparams : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BibleSelectionParamsId",
                table: "PreviewJobs",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BibleSelectionParams",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SelectedBooks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IncludeAncillary = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BibleSelectionParams", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PreviewJobs_BibleSelectionParamsId",
                table: "PreviewJobs",
                column: "BibleSelectionParamsId");

            migrationBuilder.AddForeignKey(
                name: "FK_PreviewJobs_BibleSelectionParams_BibleSelectionParamsId",
                table: "PreviewJobs",
                column: "BibleSelectionParamsId",
                principalTable: "BibleSelectionParams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PreviewJobs_BibleSelectionParams_BibleSelectionParamsId",
                table: "PreviewJobs");

            migrationBuilder.DropTable(
                name: "BibleSelectionParams");

            migrationBuilder.DropIndex(
                name: "IX_PreviewJobs_BibleSelectionParamsId",
                table: "PreviewJobs");

            migrationBuilder.DropColumn(
                name: "BibleSelectionParamsId",
                table: "PreviewJobs");
        }
    }
}
