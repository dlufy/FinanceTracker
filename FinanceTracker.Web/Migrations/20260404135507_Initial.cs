using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceTracker.Web.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cash_holdings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bank_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    account_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Savings"),
                    balance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    last_updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cash_holdings", x => x.id);
                    table.ForeignKey(
                        name: "FK_cash_holdings_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "equity_holdings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_tag = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    symbol = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    isin = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    exchange = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "NSE"),
                    company_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    average_buy_price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    current_price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    last_price_update = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    added_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_equity_holdings", x => x.id);
                    table.ForeignKey(
                        name: "FK_equity_holdings_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "expenses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    tags = table.Column<string[]>(type: "text[]", nullable: false),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Manual"),
                    added_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_expenses", x => x.id);
                    table.ForeignKey(
                        name: "FK_expenses_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "monthly_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    month = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    total_invested = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_current_value = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    equity_value = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    mutual_fund_value = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    cash_value = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    us_stock_value = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    snapshot_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_monthly_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "FK_monthly_snapshots_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mutual_fund_holdings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_tag = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    scheme_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    scheme_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    amc = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    folio_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    units = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    average_nav = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    current_nav = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    last_nav_update = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    added_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mutual_fund_holdings", x => x.id);
                    table.ForeignKey(
                        name: "FK_mutual_fund_holdings_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "us_stock_holdings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    symbol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    company_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    average_buy_price_usd = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    current_price_usd = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    exchange_rate_usd_inr = table.Column<decimal>(type: "numeric(10,4)", nullable: false),
                    last_price_update = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    added_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_us_stock_holdings", x => x.id);
                    table.ForeignKey(
                        name: "FK_us_stock_holdings_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cash_holdings_user_id",
                table: "cash_holdings",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_equity_holdings_user_id",
                table: "equity_holdings",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_equity_holdings_user_id_account_tag",
                table: "equity_holdings",
                columns: new[] { "user_id", "account_tag" });

            migrationBuilder.CreateIndex(
                name: "IX_expenses_user_id",
                table: "expenses",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_expenses_user_id_date",
                table: "expenses",
                columns: new[] { "user_id", "date" });

            migrationBuilder.CreateIndex(
                name: "IX_monthly_snapshots_user_id",
                table: "monthly_snapshots",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_monthly_snapshots_user_id_month",
                table: "monthly_snapshots",
                columns: new[] { "user_id", "month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mutual_fund_holdings_user_id",
                table: "mutual_fund_holdings",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_mutual_fund_holdings_user_id_account_tag",
                table: "mutual_fund_holdings",
                columns: new[] { "user_id", "account_tag" });

            migrationBuilder.CreateIndex(
                name: "IX_us_stock_holdings_user_id",
                table: "us_stock_holdings",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_user_name",
                table: "users",
                column: "user_name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cash_holdings");

            migrationBuilder.DropTable(
                name: "equity_holdings");

            migrationBuilder.DropTable(
                name: "expenses");

            migrationBuilder.DropTable(
                name: "monthly_snapshots");

            migrationBuilder.DropTable(
                name: "mutual_fund_holdings");

            migrationBuilder.DropTable(
                name: "us_stock_holdings");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
