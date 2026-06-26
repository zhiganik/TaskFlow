using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLabelsAndPriorityConfigs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkspaceLabels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkspaceLabels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkspaceLabels_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkspacePriorityConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Priority = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkspacePriorityConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkspacePriorityConfigs_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceLabels_WorkspaceId",
                table: "WorkspaceLabels",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkspacePriorityConfigs_WorkspaceId_Priority",
                table: "WorkspacePriorityConfigs",
                columns: new[] { "WorkspaceId", "Priority" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaskLabel");

            migrationBuilder.DropTable(
                name: "WorkspacePriorityConfigs");

            migrationBuilder.DropTable(
                name: "WorkspaceLabels");
        }
    }
}
