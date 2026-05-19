using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediConnectAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientUserRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserID",
                table: "Patients",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Patients_UserID",
                table: "Patients",
                column: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Patients_Users_UserID",
                table: "Patients",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "UserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Patients_Users_UserID",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_Patients_UserID",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "UserID",
                table: "Patients");
        }
    }
}
