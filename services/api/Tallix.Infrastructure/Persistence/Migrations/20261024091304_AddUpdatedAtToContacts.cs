using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tallix.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUpdatedAtToContacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                table: "contacts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));
            migrationBuilder.Sql("UPDATE contacts SET updated_at = created_at;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "contacts");
        }
    }
}
