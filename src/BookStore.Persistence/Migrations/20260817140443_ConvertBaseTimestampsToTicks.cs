using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookStore.Persistence.Migrations
{
    /// <summary>
    /// Stores every entity's <c>CreatedAt</c>/<c>UpdatedAt</c> (and the two <c>User</c> lockout
    /// timestamps) as UTC ticks, matching Sale.SaleDate, InventoryTransaction.Date, and
    /// AuditLogEntry.OccurredAt. SQLite supports neither comparison nor ORDER BY on
    /// DateTimeOffset, so persisting it directly is a trap shared by every entity in the system.
    /// </summary>
    /// <remarks>
    /// The values are rewritten before each column's type changes. SQLite implements AlterColumn
    /// as a table rebuild that CASTs the old value, and casting an ISO-8601 string to INTEGER
    /// yields only its leading digits ("2026-08-17 07:40:48.22+00:00" becomes 2026), which would
    /// silently destroy every existing timestamp across every table. This is the same hazard the
    /// scaffolded audit-timestamp migration had, at 13-table scale instead of one.
    /// </remarks>
    public partial class ConvertBaseTimestampsToTicks : Migration
    {
        // Ticks between 0001-01-01 and the Unix epoch, i.e. DateTime.UnixEpoch.Ticks.
        private const string UnixEpochTicks = "621355968000000000";

        // Every (table, column, nullable) pair carrying a DateTimeOffset value stamped by
        // BaseEntity, plus the two User columns that store the same shape independently.
        private static readonly (string Table, string Column, bool Nullable)[] Columns =
        [
            ("Categories", "CreatedAt", false), ("Categories", "UpdatedAt", true),
            ("Products", "CreatedAt", false), ("Products", "UpdatedAt", true),
            ("ProductSuppliers", "CreatedAt", false), ("ProductSuppliers", "UpdatedAt", true),
            ("Customers", "CreatedAt", false), ("Customers", "UpdatedAt", true),
            ("Suppliers", "CreatedAt", false), ("Suppliers", "UpdatedAt", true),
            ("Sales", "CreatedAt", false), ("Sales", "UpdatedAt", true),
            ("SaleItems", "CreatedAt", false), ("SaleItems", "UpdatedAt", true),
            ("InventoryTransactions", "CreatedAt", false), ("InventoryTransactions", "UpdatedAt", true),
            ("Roles", "CreatedAt", false), ("Roles", "UpdatedAt", true),
            ("Permissions", "CreatedAt", false), ("Permissions", "UpdatedAt", true),
            ("Users", "CreatedAt", false), ("Users", "UpdatedAt", true),
            ("Users", "LastFailedLogin", true), ("Users", "LockoutUntil", true),
            ("AuditLogEntries", "CreatedAt", false), ("AuditLogEntries", "UpdatedAt", true),
            ("ApplicationSettings", "CreatedAt", false), ("ApplicationSettings", "UpdatedAt", true)
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var (table, column, _) in Columns)
            {
                migrationBuilder.Sql(TextToTicksSql(table, column));
            }

            foreach (var (table, column, nullable) in Columns)
            {
                migrationBuilder.AlterColumn<long>(
                    name: column,
                    table: table,
                    type: "INTEGER",
                    nullable: nullable,
                    oldClrType: typeof(DateTimeOffset),
                    oldType: "TEXT",
                    oldNullable: nullable);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var (table, column, nullable) in Columns)
            {
                migrationBuilder.AlterColumn<DateTimeOffset>(
                    name: column,
                    table: table,
                    type: "TEXT",
                    nullable: nullable,
                    oldClrType: typeof(long),
                    oldType: "INTEGER",
                    oldNullable: nullable);
            }

            foreach (var (table, column, _) in Columns)
            {
                migrationBuilder.Sql(TicksToTextSql(table, column));
            }
        }

        private static string TextToTicksSql(string table, string column) =>
            $"""
            UPDATE {table}
            SET {column} = CAST(
                    {UnixEpochTicks}
                    + (CAST(strftime('%s', {column}) AS INTEGER) * 10000000)
                    + (CAST(ROUND((CAST(strftime('%f', {column}) AS REAL)
                                   - CAST(strftime('%S', {column}) AS INTEGER)) * 1000) AS INTEGER) * 10000)
                AS TEXT)
            WHERE {column} IS NOT NULL AND strftime('%s', {column}) IS NOT NULL;
            """;

        private static string TicksToTextSql(string table, string column) =>
            $"""
            UPDATE {table}
            SET {column} = strftime('%Y-%m-%d %H:%M:%f',
                    (CAST({column} AS INTEGER) - {UnixEpochTicks}) / 10000000.0,
                    'unixepoch') || '+00:00'
            WHERE {column} IS NOT NULL;
            """;
    }
}
