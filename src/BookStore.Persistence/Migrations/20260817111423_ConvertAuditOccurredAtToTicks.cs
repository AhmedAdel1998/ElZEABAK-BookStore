using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookStore.Persistence.Migrations
{
    /// <summary>
    /// Stores audit timestamps as UTC ticks so SQLite can filter and sort them, matching
    /// Sale.SaleDate and InventoryTransaction.Date.
    /// </summary>
    /// <remarks>
    /// The values are rewritten before the column type changes. SQLite implements AlterColumn as a
    /// table rebuild that CASTs the old value, and casting an ISO-8601 string to INTEGER yields the
    /// leading year digits ("2026-08-17 07:40:48.22+00:00" becomes 2026), which would silently
    /// destroy every existing timestamp. Converting the text to its tick representation first makes
    /// that cast lossless to the millisecond.
    /// </remarks>
    public partial class ConvertAuditOccurredAtToTicks : Migration
    {
        // Ticks between 0001-01-01 and the Unix epoch, i.e. DateTime.UnixEpoch.Ticks.
        private const string UnixEpochTicks = "621355968000000000";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                $"""
                UPDATE AuditLogEntries
                SET OccurredAt = CAST(
                        {UnixEpochTicks}
                        + (CAST(strftime('%s', OccurredAt) AS INTEGER) * 10000000)
                        + (CAST(ROUND((CAST(strftime('%f', OccurredAt) AS REAL)
                                       - CAST(strftime('%S', OccurredAt) AS INTEGER)) * 1000) AS INTEGER) * 10000)
                    AS TEXT)
                WHERE OccurredAt IS NOT NULL AND strftime('%s', OccurredAt) IS NOT NULL;
                """);

            migrationBuilder.AlterColumn<long>(
                name: "OccurredAt",
                table: "AuditLogEntries",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "TEXT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "OccurredAt",
                table: "AuditLogEntries",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            // Rebuild the original UTC ISO-8601 text from the tick value.
            migrationBuilder.Sql(
                $"""
                UPDATE AuditLogEntries
                SET OccurredAt = strftime('%Y-%m-%d %H:%M:%f',
                        (CAST(OccurredAt AS INTEGER) - {UnixEpochTicks}) / 10000000.0,
                        'unixepoch') || '+00:00'
                WHERE OccurredAt IS NOT NULL;
                """);
        }
    }
}
