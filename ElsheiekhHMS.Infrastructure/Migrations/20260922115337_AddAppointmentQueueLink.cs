using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElsheiekhHMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentQueueLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AppointmentId",
                schema: "dbo",
                table: "WalkInQueueEntries",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "UX_WalkInQueueEntries_AppointmentId",
                schema: "dbo",
                table: "WalkInQueueEntries",
                column: "AppointmentId",
                unique: true,
                filter: "[AppointmentId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_WalkInQueueEntries_Appointments_AppointmentId",
                schema: "dbo",
                table: "WalkInQueueEntries",
                column: "AppointmentId",
                principalSchema: "dbo",
                principalTable: "Appointments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WalkInQueueEntries_Appointments_AppointmentId",
                schema: "dbo",
                table: "WalkInQueueEntries");

            migrationBuilder.DropIndex(
                name: "UX_WalkInQueueEntries_AppointmentId",
                schema: "dbo",
                table: "WalkInQueueEntries");

            migrationBuilder.DropColumn(
                name: "AppointmentId",
                schema: "dbo",
                table: "WalkInQueueEntries");
        }
    }
}
