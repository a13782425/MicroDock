using SQLite;
using System;

namespace MicroDock.Database;

public class IconDB
{
    [PrimaryKey]
    public string Sha256Hash { get; set; } = string.Empty;
    
    public byte[] IconData { get; set; } = Array.Empty<byte>();
    
    public int ReferenceCount { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    [Indexed]
    public DateTime LastAccessedAt { get; set; }
}

