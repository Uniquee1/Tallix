using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tallix.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReversalLinkToLedgerEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "reverses_entry_id",
                table: "ledger_entries",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_ledger_entries_reverses_entry_id",
                table: "ledger_entries",
                column: "reverses_entry_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_ledger_entries_reverses_entry",
                table: "ledger_entries",
                column: "reverses_entry_id",
                principalTable: "ledger_entries",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_ledger_entries_reverses_entry",
                table: "ledger_entries");

            migrationBuilder.DropIndex(
                name: "ux_ledger_entries_reverses_entry_id",
                table: "ledger_entries");

            migrationBuilder.DropColumn(
                name: "reverses_entry_id",
                table: "ledger_entries");
        }
    }
}
