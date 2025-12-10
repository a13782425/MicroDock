using SQLite;
using MicroNotePlugin.Entities;

namespace MicroNotePlugin.Database;

/// <summary>
/// 数据库上下文 - 管理 SQLite 连接
/// </summary>
public class NoteDbContext : IDisposable
{
    private readonly SQLiteAsyncConnection _connection;
    private bool _disposed;

    public NoteDbContext(string dataPath)
    {
        var dbPath = Path.Combine(dataPath, "notes.db");
        _connection = new SQLiteAsyncConnection(dbPath);
    }

    /// <summary>
    /// 获取异步数据库连接
    /// </summary>
    public SQLiteAsyncConnection Connection => _connection;

    /// <summary>
    /// 初始化数据库表
    /// </summary>
    public async Task InitializeAsync()
    {
        // 创建所有表
        await _connection.CreateTableAsync<Note>();
        await _connection.CreateTableAsync<Folder>();
        await _connection.CreateTableAsync<Tag>();
        await _connection.CreateTableAsync<NoteTag>();
        await _connection.CreateTableAsync<NoteVersion>();
        await _connection.CreateTableAsync<Image>();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _connection.CloseAsync().Wait();
            _disposed = true;
        }
    }
}
