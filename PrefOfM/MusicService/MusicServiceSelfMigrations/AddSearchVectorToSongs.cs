using Microsoft.EntityFrameworkCore.Migrations;

namespace MusicService.MusicServiceSelfMigrations;

public partial class AddSearchVectorToSongs : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Добавление поля search_vector
        migrationBuilder.AddColumn<string>(
            name: "SearchVector",
            table: "Songs",
            type: "tsvector",
            nullable: true);

        // Создание индекса GIN
        migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS songs_search_vector_idx ON \"Songs\" USING gin(search_vector)");

        // Обновление столбца search_vector с использованием SQL выражения
        migrationBuilder.Sql(@"
            UPDATE ""Songs""
            SET ""SearchVector"" = to_tsvector('simple', coalesce(Title, '') || ' ' || coalesce(Artist, '') || ' ' || coalesce(Emotion, ''))
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Удаление индекса
        migrationBuilder.Sql("DROP INDEX IF EXISTS songs_search_vector_idx");

        // Удаление столбца search_vector
        migrationBuilder.DropColumn(
            name: "SearchVector",
            table: "Songs");
    }
}