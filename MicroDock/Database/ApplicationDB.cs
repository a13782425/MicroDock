using SQLite;

namespace MicroDock.Database;

public class ApplicationDB
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; }
    public string FilePath { get; set; }
    public byte[]? Icon { get; set; }
}
