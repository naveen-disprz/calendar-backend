using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Calendar.Migrations
{
    /// <inheritdoc />
    public partial class AppointmentTypeData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AppointmentTypes",
                columns: new[] { "AppointmentTypeId", "ColorCode", "TypeName" },
                values: new object[,]
                {
                    { 1, "#1E90FF", "Sprint Planning" },
                    { 2, "#32CD32", "Code Review" },
                    { 3, "#FFD700", "Stand-up Meeting" },
                    { 4, "#FF4500", "Client Demo" },
                    { 5, "#8A2BE2", "Retrospective" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AppointmentTypes",
                keyColumn: "AppointmentTypeId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "AppointmentTypes",
                keyColumn: "AppointmentTypeId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "AppointmentTypes",
                keyColumn: "AppointmentTypeId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "AppointmentTypes",
                keyColumn: "AppointmentTypeId",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "AppointmentTypes",
                keyColumn: "AppointmentTypeId",
                keyValue: 5);
        }
    }
}
