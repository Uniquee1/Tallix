using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tallix.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthSubjectToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "auth_subject",
                table: "users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ux_users_auth_subject",
                table: "users",
                column: "auth_subject",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_users_auth_subject",
                table: "users");

            migrationBuilder.DropColumn(
                name: "auth_subject",
                table: "users");
        }
    }
}
