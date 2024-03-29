﻿/*
Copyright © 2021 by Biblica, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using Microsoft.EntityFrameworkCore.Migrations;

namespace TptMain.Migrations
{
    public partial class tptservice_DT185 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BookFormat",
                table: "PreviewJobs");

            migrationBuilder.DropColumn(
                name: "FontLeadingInPts",
                table: "PreviewJobs");

            migrationBuilder.DropColumn(
                name: "FontSizeInPts",
                table: "PreviewJobs");

            migrationBuilder.DropColumn(
                name: "PageHeaderInPts",
                table: "PreviewJobs");

            migrationBuilder.DropColumn(
                name: "PageHeightInPts",
                table: "PreviewJobs");

            migrationBuilder.DropColumn(
                name: "PageWidthInPts",
                table: "PreviewJobs");

            migrationBuilder.DropColumn(
                name: "ProjectName",
                table: "PreviewJobs");

            migrationBuilder.DropColumn(
                name: "UseCustomFootnotes",
                table: "PreviewJobs");

            migrationBuilder.DropColumn(
                name: "UseProjectFont",
                table: "PreviewJobs");

            migrationBuilder.AddColumn<string>(
                name: "BibleSelectionParamsId",
                table: "PreviewJobs",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "State",
                table: "PreviewJobs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TypesettingParamsId",
                table: "PreviewJobs",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BibleSelectionParams",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProjectName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SelectedBooks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IncludeAncillary = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BibleSelectionParams", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TypesettingParams",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FontSizeInPts = table.Column<float>(type: "real", nullable: true),
                    FontLeadingInPts = table.Column<float>(type: "real", nullable: true),
                    PageWidthInPts = table.Column<float>(type: "real", nullable: true),
                    PageHeightInPts = table.Column<float>(type: "real", nullable: true),
                    PageHeaderInPts = table.Column<float>(type: "real", nullable: true),
                    BookFormat = table.Column<int>(type: "int", nullable: true),
                    UseHyphenation = table.Column<bool>(type: "bit", nullable: false),
                    UseCustomFootnotes = table.Column<bool>(type: "bit", nullable: false),
                    UseProjectFont = table.Column<bool>(type: "bit", nullable: false),
                    IncludeIntros = table.Column<bool>(type: "bit", nullable: false),
                    IncludeFootnotes = table.Column<bool>(type: "bit", nullable: false),
                    IncludeChapterNumbers = table.Column<bool>(type: "bit", nullable: false),
                    IncludeVerseNumbers = table.Column<bool>(type: "bit", nullable: false),
                    IncludeParallelPassages = table.Column<bool>(type: "bit", nullable: false),
                    IncludeAcrosticPoetry = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TypesettingParams", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PreviewJobs_BibleSelectionParamsId",
                table: "PreviewJobs",
                column: "BibleSelectionParamsId");

            migrationBuilder.CreateIndex(
                name: "IX_PreviewJobs_TypesettingParamsId",
                table: "PreviewJobs",
                column: "TypesettingParamsId");

            migrationBuilder.AddForeignKey(
                name: "FK_PreviewJobs_BibleSelectionParams_BibleSelectionParamsId",
                table: "PreviewJobs",
                column: "BibleSelectionParamsId",
                principalTable: "BibleSelectionParams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PreviewJobs_TypesettingParams_TypesettingParamsId",
                table: "PreviewJobs",
                column: "TypesettingParamsId",
                principalTable: "TypesettingParams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PreviewJobs_BibleSelectionParams_BibleSelectionParamsId",
                table: "PreviewJobs");

            migrationBuilder.DropForeignKey(
                name: "FK_PreviewJobs_TypesettingParams_TypesettingParamsId",
                table: "PreviewJobs");

            migrationBuilder.DropTable(
                name: "BibleSelectionParams");

            migrationBuilder.DropTable(
                name: "TypesettingParams");

            migrationBuilder.DropIndex(
                name: "IX_PreviewJobs_BibleSelectionParamsId",
                table: "PreviewJobs");

            migrationBuilder.DropIndex(
                name: "IX_PreviewJobs_TypesettingParamsId",
                table: "PreviewJobs");

            migrationBuilder.DropColumn(
                name: "BibleSelectionParamsId",
                table: "PreviewJobs");

            migrationBuilder.DropColumn(
                name: "State",
                table: "PreviewJobs");

            migrationBuilder.DropColumn(
                name: "TypesettingParamsId",
                table: "PreviewJobs");

            migrationBuilder.AddColumn<int>(
                name: "BookFormat",
                table: "PreviewJobs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "FontLeadingInPts",
                table: "PreviewJobs",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "FontSizeInPts",
                table: "PreviewJobs",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "PageHeaderInPts",
                table: "PreviewJobs",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "PageHeightInPts",
                table: "PreviewJobs",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "PageWidthInPts",
                table: "PreviewJobs",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProjectName",
                table: "PreviewJobs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UseCustomFootnotes",
                table: "PreviewJobs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UseProjectFont",
                table: "PreviewJobs",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
