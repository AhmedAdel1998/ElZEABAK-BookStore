using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookStore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReportAnalyticsIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sales_CustomerId",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_UserId",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Products_CategoryId",
                table: "Products");

            migrationBuilder.AlterColumn<long>(
                name: "SaleDate",
                table: "Sales",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<long>(
                name: "Date",
                table: "InventoryTransactions",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "TEXT");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_CustomerId_SaleDate",
                table: "Sales",
                columns: new[] { "CustomerId", "SaleDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Sales_PaymentMethod_SaleDate",
                table: "Sales",
                columns: new[] { "PaymentMethod", "SaleDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Sales_SaleDate",
                table: "Sales",
                column: "SaleDate");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_SaleDate_Status",
                table: "Sales",
                columns: new[] { "SaleDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Sales_UserId_SaleDate",
                table: "Sales",
                columns: new[] { "UserId", "SaleDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId_IsActive",
                table: "Products",
                columns: new[] { "CategoryId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Quantity_MinimumStock",
                table: "Products",
                columns: new[] { "Quantity", "MinimumStock" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_Date_TransactionType",
                table: "InventoryTransactions",
                columns: new[] { "Date", "TransactionType" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_UserId_Date",
                table: "InventoryTransactions",
                columns: new[] { "UserId", "Date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sales_CustomerId_SaleDate",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_PaymentMethod_SaleDate",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_SaleDate",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_SaleDate_Status",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_UserId_SaleDate",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Products_CategoryId_IsActive",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_Quantity_MinimumStock",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_InventoryTransactions_Date_TransactionType",
                table: "InventoryTransactions");

            migrationBuilder.DropIndex(
                name: "IX_InventoryTransactions_UserId_Date",
                table: "InventoryTransactions");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "SaleDate",
                table: "Sales",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "Date",
                table: "InventoryTransactions",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_CustomerId",
                table: "Sales",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_UserId",
                table: "Sales",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId",
                table: "Products",
                column: "CategoryId");
        }
    }
}
