using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMadina.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentRecordIntegrationId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IntegrationId",
                table: "PaymentRecords",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IntegrationId",
                table: "PaymentRecords");
        }
    }
}
