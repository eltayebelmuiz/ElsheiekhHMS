using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElsheiekhHMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase07AllocatorInfrastructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PatientCodeAllocations",
                schema: "dbo",
                columns: table => new
                {
                    CodeYear = table.Column<int>(type: "int", nullable: false),
                    NextSequenceNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientCodeAllocations", x => x.CodeYear);
                    table.CheckConstraint("CK_PatientCodeAllocations_NextSequenceNumber", "[NextSequenceNumber] >= 1 AND [NextSequenceNumber] <= 100000");
                });

            migrationBuilder.CreateTable(
                name: "QueueTicketAllocations",
                schema: "dbo",
                columns: table => new
                {
                    QueueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    NextSequenceNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueueTicketAllocations", x => x.QueueDate);
                    table.CheckConstraint("CK_QueueTicketAllocations_NextSequenceNumber", "[NextSequenceNumber] >= 1 AND [NextSequenceNumber] <= 1000");
                });

            migrationBuilder.CreateIndex(
                name: "UX_WalkInQueueEntries_QueueDate_SequenceNumber",
                schema: "dbo",
                table: "WalkInQueueEntries",
                columns: new[] { "QueueDate", "SequenceNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PatientCodeAllocations",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "QueueTicketAllocations",
                schema: "dbo");

            migrationBuilder.DropIndex(
                name: "UX_WalkInQueueEntries_QueueDate_SequenceNumber",
                schema: "dbo",
                table: "WalkInQueueEntries");
        }
    }
}
