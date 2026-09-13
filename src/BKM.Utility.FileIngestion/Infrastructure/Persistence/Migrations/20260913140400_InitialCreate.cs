using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BKM.Utility.Infrastructure.Persistence.Migrations.FileIngestion
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "IngestedRecords",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RecordType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RowNumber = table.Column<long>(type: "bigint", nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PayloadHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IngestedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngestedRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IngestionBatches",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RecordType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ParseOptions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalRows = table.Column<long>(type: "bigint", nullable: false),
                    SuccessRows = table.Column<long>(type: "bigint", nullable: false),
                    DuplicateRows = table.Column<long>(type: "bigint", nullable: false),
                    ErrorRows = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngestionBatches", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IngestedRecords_BatchId_PayloadHash",
                schema: "dbo",
                table: "IngestedRecords",
                columns: new[] { "BatchId", "PayloadHash" });

            migrationBuilder.CreateIndex(
                name: "IX_IngestedRecords_BatchId_RowNumber",
                schema: "dbo",
                table: "IngestedRecords",
                columns: new[] { "BatchId", "RowNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_IngestedRecords_FileType_RecordType",
                schema: "dbo",
                table: "IngestedRecords",
                columns: new[] { "FileType", "RecordType" });

            migrationBuilder.CreateIndex(
                name: "IX_IngestedRecords_IngestedAt",
                schema: "dbo",
                table: "IngestedRecords",
                column: "IngestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_IngestionBatches_StartedAt",
                schema: "dbo",
                table: "IngestionBatches",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_IngestionBatches_Status",
                schema: "dbo",
                table: "IngestionBatches",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IngestedRecords",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "IngestionBatches",
                schema: "dbo");
        }
    }
}
