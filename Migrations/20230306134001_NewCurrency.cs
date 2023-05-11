using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace stankin3.Migrations
{
    /// <inheritdoc />
    public partial class NewCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Value",
                table: "Rates",
                newName: "USD");

            migrationBuilder.AddColumn<double>(
                name: "ILS",
                table: "Rates",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ILS",
                table: "Rates");

            migrationBuilder.RenameColumn(
                name: "USD",
                table: "Rates",
                newName: "Value");
        }
    }
}
