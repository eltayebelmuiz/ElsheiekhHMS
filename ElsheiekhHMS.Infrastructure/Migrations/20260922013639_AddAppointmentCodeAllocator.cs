using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElsheiekhHMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentCodeAllocator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppointmentCodeAllocations",
                schema: "dbo",
                columns: table => new
                {
                    AllocationYear = table.Column<int>(type: "int", nullable: false),
                    NextSequenceNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentCodeAllocations", x => x.AllocationYear);
                    table.CheckConstraint("CK_AppointmentCodeAllocations_NextSequenceNumber", "[NextSequenceNumber] >= 1");
                });

            migrationBuilder.CreateIndex(
                name: "UX_Appointments_AppointmentCode",
                schema: "dbo",
                table: "Appointments",
                column: "AppointmentCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppointmentCodeAllocations",
                schema: "dbo");

            migrationBuilder.DropIndex(
                name: "UX_Appointments_AppointmentCode",
                schema: "dbo",
                table: "Appointments");
        }
    }
}
