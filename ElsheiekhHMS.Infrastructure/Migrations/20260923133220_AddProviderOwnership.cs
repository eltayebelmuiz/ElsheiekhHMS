using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElsheiekhHMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DoctorApplicationUserLinks",
                schema: "dbo",
                columns: table => new
                {
                    DoctorId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorApplicationUserLinks", x => x.DoctorId);
                    table.ForeignKey(
                        name: "FK_DoctorApplicationUserLinks_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DoctorApplicationUserLinks_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalSchema: "dbo",
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UX_DoctorApplicationUserLinks_UserId",
                schema: "dbo",
                table: "DoctorApplicationUserLinks",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DoctorApplicationUserLinks",
                schema: "dbo");
        }
    }
}
