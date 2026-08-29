using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookStore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSaleItemHistoricalUnitCost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "UnitCost",
                table: "SaleItems",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            // Historical source cost did not exist before this migration. Preserve the best
            // recoverable value once, at upgrade time, so later product-price edits cannot keep
            // rewriting legacy profit reports.
            migrationBuilder.Sql(
                "UPDATE SaleItems SET UnitCost = COALESCE((SELECT PurchasePrice FROM Products WHERE Products.Id = SaleItems.ProductId), 0);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UnitCost",
                table: "SaleItems");
        }
    }
}
