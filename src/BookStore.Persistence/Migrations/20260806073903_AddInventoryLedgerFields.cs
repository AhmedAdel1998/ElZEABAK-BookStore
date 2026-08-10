using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookStore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryLedgerFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "QuantityAfter",
                table: "InventoryTransactions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "QuantityBefore",
                table: "InventoryTransactions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "InventoryTransactions",
                type: "TEXT",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "InventoryTransactions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "InventoryTransactions",
                type: "TEXT",
                maxLength: 150,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_Date",
                table: "InventoryTransactions",
                column: "Date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InventoryTransactions_Date",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "QuantityAfter",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "QuantityBefore",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "InventoryTransactions");
        }
    }
}
