using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace littleShop.identity.Migrations
{
    /// <inheritdoc />
    public partial class FixModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "rol-admin-id-fijo-123",
                column: "ConcurrencyStamp",
                value: "0042a95c-b822-48fe-9d3d-4d694a513243");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "rol-user-id-fijo-456",
                column: "ConcurrencyStamp",
                value: "3756fb0f-ed86-4e11-8bff-4d25d94a01aa");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
    }
}
