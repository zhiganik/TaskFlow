using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TaskImprovements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Number",
                table: "WorkspaceTasks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "WorkspaceTasks",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "WorkspaceColumns",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceTasks_WorkspaceId_Number",
                table: "WorkspaceTasks",
                columns: new[] { "WorkspaceId", "Number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkspaceTasks_WorkspaceId_Number",
                table: "WorkspaceTasks");

            migrationBuilder.DropColumn(
                name: "Number",
                table: "WorkspaceTasks");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "WorkspaceTasks");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "WorkspaceColumns");
        }
    }
}
