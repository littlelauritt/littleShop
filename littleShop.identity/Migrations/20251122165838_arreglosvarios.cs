using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace littleShop.identity.Migrations
{
    /// <inheritdoc />
    public partial class arreglosvarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "rol-admin-id-fijo-123");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "rol-user-id-fijo-456");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "rol-admin-id-fijo-123", "7a59905d-4270-4328-9ff6-d48f62d7a07e", "Admin", "ADMIN" },
                    { "rol-user-id-fijo-456", "46a8f8a0-2883-4052-811e-047ee9159dc6", "User", "USER" }
                });
        }
    }
}
