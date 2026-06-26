using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BackfillPriorityConfigsAndExplicitTaskLabel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // TaskLabel had no data (feature was just introduced), safe to drop and recreate with
            // the explicit entity name TaskLabels and corrected PK order (TaskId, LabelId).
            migrationBuilder.DropTable(
                name: "TaskLabel");

            migrationBuilder.CreateTable(
                name: "TaskLabels",
                columns: table => new
                {
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    LabelId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskLabels", x => new { x.TaskId, x.LabelId });
                    table.ForeignKey(
                        name: "FK_TaskLabels_WorkspaceLabels_LabelId",
                        column: x => x.LabelId,
                        principalTable: "WorkspaceLabels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskLabels_WorkspaceTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "WorkspaceTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskLabels_LabelId",
                table: "TaskLabels",
                column: "LabelId");

            // Backfill priority configs for all workspaces created before this feature.
            migrationBuilder.Sql("""
                INSERT INTO "WorkspacePriorityConfigs" ("Id", "WorkspaceId", "Priority", "DisplayName", "Color")
                SELECT gen_random_uuid(), w."Id", 'Low', 'Low', '#22c55e'
                FROM "Workspaces" w
                WHERE NOT EXISTS (
                    SELECT 1 FROM "WorkspacePriorityConfigs" pc
                    WHERE pc."WorkspaceId" = w."Id" AND pc."Priority" = 'Low'
                );

                INSERT INTO "WorkspacePriorityConfigs" ("Id", "WorkspaceId", "Priority", "DisplayName", "Color")
                SELECT gen_random_uuid(), w."Id", 'Medium', 'Medium', '#f59e0b'
                FROM "Workspaces" w
                WHERE NOT EXISTS (
                    SELECT 1 FROM "WorkspacePriorityConfigs" pc
                    WHERE pc."WorkspaceId" = w."Id" AND pc."Priority" = 'Medium'
                );

                INSERT INTO "WorkspacePriorityConfigs" ("Id", "WorkspaceId", "Priority", "DisplayName", "Color")
                SELECT gen_random_uuid(), w."Id", 'High', 'High', '#ef4444'
                FROM "Workspaces" w
                WHERE NOT EXISTS (
                    SELECT 1 FROM "WorkspacePriorityConfigs" pc
                    WHERE pc."WorkspaceId" = w."Id" AND pc."Priority" = 'High'
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaskLabels");

            migrationBuilder.CreateTable(
                name: "TaskLabel",
                columns: table => new
                {
                    LabelId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskLabel", x => new { x.LabelId, x.TaskId });
                    table.ForeignKey(
                        name: "FK_TaskLabel_WorkspaceLabels_LabelId",
                        column: x => x.LabelId,
                        principalTable: "WorkspaceLabels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskLabel_WorkspaceTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "WorkspaceTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskLabel_TaskId",
                table: "TaskLabel",
                column: "TaskId");
        }
    }
}
