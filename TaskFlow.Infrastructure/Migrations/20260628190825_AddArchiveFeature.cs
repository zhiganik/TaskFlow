using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddArchiveFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedAt",
                table: "WorkspaceTasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "WorkspaceTasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "WorkspaceTasks",
                type: "text",
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.AddColumn<int>(
                name: "ArchiveAfterDays",
                table: "Workspaces",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<bool>(
                name: "IsDoneColumn",
                table: "WorkspaceColumns",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceTasks_Status_CompletedAt",
                table: "WorkspaceTasks",
                columns: new[] { "Status", "CompletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceTasks_WorkspaceId_ClosedAt",
                table: "WorkspaceTasks",
                columns: new[] { "WorkspaceId", "ClosedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceTasks_WorkspaceId_Status",
                table: "WorkspaceTasks",
                columns: new[] { "WorkspaceId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkspaceTasks_Status_CompletedAt",
                table: "WorkspaceTasks");

            migrationBuilder.DropIndex(
                name: "IX_WorkspaceTasks_WorkspaceId_ClosedAt",
                table: "WorkspaceTasks");

            migrationBuilder.DropIndex(
                name: "IX_WorkspaceTasks_WorkspaceId_Status",
                table: "WorkspaceTasks");

            migrationBuilder.DropColumn(
                name: "ClosedAt",
                table: "WorkspaceTasks");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "WorkspaceTasks");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "WorkspaceTasks");

            migrationBuilder.DropColumn(
                name: "ArchiveAfterDays",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "IsDoneColumn",
                table: "WorkspaceColumns");
        }
    }
}
