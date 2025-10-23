using SQLite;

namespace MicroDock.Database;

public class ApplicationDB
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    
    [Indexed]
    public string? IconHash { get; set; }
}
