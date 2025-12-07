using MicroNotePlugin.Core.Entities;
using MicroNotePlugin.Core.Interfaces;
using MicroNotePlugin.Infrastructure.Database;

namespace MicroNotePlugin.Infrastructure.Repositories;

/// <summary>
/// SQLite 版本仓储实现
/// </summary>
public class SqliteVersionRepository : IVersionService
{
    private readonly NoteDbContext _context;

    public SqliteVersionRepository(NoteDbContext context)
    {
        _context = context;
    }

    public async Task<NoteVersion> CreateVersionAsync(string noteId, string content)
    {
        // 获取当前最大版本号
        var maxVersion = await GetLatestVersionNumberAsync(noteId);

        var version = new NoteVersion
        {
            NoteId = noteId,
            Version = maxVersion + 1,
            Content = content,
            CreatedAt = DateTime.Now
        };

        await _context.Connection.InsertAsync(version);

        // 清理旧版本 (默认保留 50 个)
        await CleanupOldVersionsAsync(noteId);

        return version;
    }

    public async Task<IEnumerable<NoteVersion>> GetVersionsAsync(string noteId)
    {
        return await _context.Connection.Table<NoteVersion>()
            .Where(v => v.NoteId == noteId)
            .OrderByDescending(v => v.Version)
            .ToListAsync();
    }

    public async Task<NoteVersion?> GetVersionAsync(string versionId)
    {
        return await _context.Connection.FindAsync<NoteVersion>(versionId);
    }

    public async Task<int> GetLatestVersionNumberAsync(string noteId)
    {
        var versions = await _context.Connection.Table<NoteVersion>()
            .Where(v => v.NoteId == noteId)
            .OrderByDescending(v => v.Version)
            .FirstOrDefaultAsync();
            
        return versions?.Version ?? 0;
    }

    public async Task<string> RestoreVersionAsync(string noteId, string versionId)
    {
        var ver = await GetVersionAsync(versionId);
        return ver?.Content ?? string.Empty;
    }

    public async Task DeleteAllVersionsAsync(string noteId)
    {
        await _context.Connection.ExecuteAsync(
            "DELETE FROM note_versions WHERE note_id = ?", noteId);
    }

    public async Task CleanupOldVersionsAsync(string noteId, int keepCount = 50)
    {
        var versions = await _context.Connection.Table<NoteVersion>()
            .Where(v => v.NoteId == noteId)
            .OrderByDescending(v => v.Version)
            .ToListAsync();

        if (versions.Count > keepCount)
        {
            var toDelete = versions.Skip(keepCount).ToList();
            foreach (var v in toDelete)
            {
                await _context.Connection.DeleteAsync(v);
            }
        }
    }
}
