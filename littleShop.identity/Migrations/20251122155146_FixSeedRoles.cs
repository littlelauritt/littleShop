using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace littleShop.identity.Migrations
{
    /// <inheritdoc />
    public partial class FixSeedRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "rol-admin-id-fijo-123",
                column: "ConcurrencyStamp",
                value: "7a59905d-4270-4328-9ff6-d48f62d7a07e");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "rol-user-id-fijo-456",
                column: "ConcurrencyStamp",
                value: "46a8f8a0-2883-4052-811e-047ee9159dc6");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
    }
}
