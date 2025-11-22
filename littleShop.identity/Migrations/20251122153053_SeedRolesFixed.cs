using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace littleShop.identity.Migrations
{
    /// <inheritdoc />
    public partial class SeedRolesFixed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "rol-admin-id-fijo-123",
                column: "ConcurrencyStamp",
                value: "17ec0941-c6fa-42a5-8a66-c0018d5df29e");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "rol-user-id-fijo-456",
                column: "ConcurrencyStamp",
                value: "cafe32cd-3dff-44f0-a2b2-fc591eeaf085");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "rol-admin-id-fijo-123",
                column: "ConcurrencyStamp",
                value: "6b72a998-84b9-4436-a1e7-4b857c76d7bb");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "rol-user-id-fijo-456",
                column: "ConcurrencyStamp",
                value: "5633560b-d80a-4fbd-a857-39615c202b76");
        }
    }
}
