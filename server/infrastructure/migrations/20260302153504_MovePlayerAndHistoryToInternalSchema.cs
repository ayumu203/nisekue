using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.infrastructure.migrations
{
    /// <inheritdoc />
    public partial class MovePlayerAndHistoryToInternalSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "internal");

            migrationBuilder.RenameTable(
                name: "players",
                newName: "players",
                newSchema: "internal");

            migrationBuilder.Sql("""
                do $$
                begin
                    if exists (
                        select 1
                        from information_schema.tables
                        where table_schema = 'public'
                          and table_name = '__EFMigrationsHistory'
                    ) then
                        alter table public."__EFMigrationsHistory" set schema internal;
                    end if;
                end
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                do $$
                begin
                    if exists (
                        select 1
                        from information_schema.tables
                        where table_schema = 'internal'
                          and table_name = '__EFMigrationsHistory'
                    ) then
                        alter table internal."__EFMigrationsHistory" set schema public;
                    end if;
                end
                $$;
                """);

            migrationBuilder.RenameTable(
                name: "players",
                schema: "internal",
                newName: "players");
        }
    }
}
