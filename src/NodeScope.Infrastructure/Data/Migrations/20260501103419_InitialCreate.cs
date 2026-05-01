using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NodeScope.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    role = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    source_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_projects_users_owner_user_id",
                        column: x => x.owner_user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    project_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    target_type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    target_id = table.Column<Guid>(type: "uuid", nullable: true),
                    metadata_json = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_audit_logs_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_audit_logs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "import_jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    original_file_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    stored_file_path = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    processor_version = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    row_count = table.Column<int>(type: "integer", nullable: true),
                    issue_count = table.Column<int>(type: "integer", nullable: true),
                    report_html_path = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    normalized_json_path = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    summary_json = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_import_jobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_import_jobs_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "dataset_columns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    import_job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    data_type_detected = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    distinct_count = table.Column<int>(type: "integer", nullable: false),
                    null_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dataset_columns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_dataset_columns_import_jobs_import_job_id",
                        column: x => x.import_job_id,
                        principalTable: "import_jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "dataset_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    import_job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    record_index = table.Column<int>(type: "integer", nullable: false),
                    payload_json = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dataset_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_dataset_records_import_jobs_import_job_id",
                        column: x => x.import_job_id,
                        principalTable: "import_jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "generated_artifacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    import_job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    path = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    mime_type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_generated_artifacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_generated_artifacts_import_jobs_import_job_id",
                        column: x => x.import_job_id,
                        principalTable: "import_jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "validation_issues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    import_job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    severity = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    message = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    column_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    row_index = table.Column<int>(type: "integer", nullable: true),
                    raw_value = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_validation_issues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_validation_issues_import_jobs_import_job_id",
                        column: x => x.import_job_id,
                        principalTable: "import_jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_created_at",
                table: "audit_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_project_id",
                table: "audit_logs",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_user_created_at",
                table: "audit_logs",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_dataset_columns_import_job_id",
                table: "dataset_columns",
                column: "import_job_id");

            migrationBuilder.CreateIndex(
                name: "ix_dataset_columns_import_normalized_name",
                table: "dataset_columns",
                columns: new[] { "import_job_id", "normalized_name" });

            migrationBuilder.CreateIndex(
                name: "ix_dataset_records_import_job_id",
                table: "dataset_records",
                column: "import_job_id");

            migrationBuilder.CreateIndex(
                name: "uq_dataset_records_import_row_index",
                table: "dataset_records",
                columns: new[] { "import_job_id", "record_index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_generated_artifacts_import_job_id",
                table: "generated_artifacts",
                column: "import_job_id");

            migrationBuilder.CreateIndex(
                name: "ix_generated_artifacts_import_type",
                table: "generated_artifacts",
                columns: new[] { "import_job_id", "type" });

            migrationBuilder.CreateIndex(
                name: "ix_import_jobs_project_id_status",
                table: "import_jobs",
                columns: new[] { "project_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_import_jobs_started_at",
                table: "import_jobs",
                column: "started_at");

            migrationBuilder.CreateIndex(
                name: "ix_projects_owner_user_id",
                table: "projects",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_email_unique",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_validation_issues_import_job_id",
                table: "validation_issues",
                column: "import_job_id");

            migrationBuilder.CreateIndex(
                name: "ix_validation_issues_import_severity",
                table: "validation_issues",
                columns: new[] { "import_job_id", "severity" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "dataset_columns");

            migrationBuilder.DropTable(
                name: "dataset_records");

            migrationBuilder.DropTable(
                name: "generated_artifacts");

            migrationBuilder.DropTable(
                name: "validation_issues");

            migrationBuilder.DropTable(
                name: "import_jobs");

            migrationBuilder.DropTable(
                name: "projects");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
