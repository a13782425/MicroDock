using MicroNotePlugin.Entities;
using MicroNotePlugin.Database;

namespace MicroNotePlugin.Services;

/// <summary>
/// SQLite 标签仓储实现
/// </summary>
public class SqliteTagRepository : ITagRepository
{
    private readonly NoteDbContext _context;

    public SqliteTagRepository(NoteDbContext context)
    {
        _context = context;
    }

    public async Task<Tag?> GetByIdAsync(string id)
    {
        return await _context.Connection.FindAsync<Tag>(id);
    }

    public async Task<Tag?> GetByNameAsync(string name)
    {
        return await _context.Connection.Table<Tag>()
            .Where(t => t.Name == name)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Tag>> GetAllAsync()
    {
        return await _context.Connection.Table<Tag>()
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<Tag> CreateAsync(Tag tag)
    {
        await _context.Connection.InsertAsync(tag);
        return tag;
    }

    public async Task<Tag> CreateAsync(string name, string color = "#808080")
    {
        var tag = new Tag
        {
            Name = name,
            Color = color
        };
        await CreateAsync(tag);
        return tag;
    }

    public async Task UpdateAsync(Tag tag)
    {
        await _context.Connection.UpdateAsync(tag);
    }

    public async Task DeleteAsync(string id)
    {
        // 先删除关联
        await _context.Connection.ExecuteAsync(
            "DELETE FROM note_tags WHERE tag_id = ?", id);
        
        // 删除标签
        await _context.Connection.DeleteAsync<Tag>(id);
    }

    public async Task<Tag> GetOrCreateAsync(string name, string? color = null)
    {
        var existing = await GetByNameAsync(name);
        if (existing != null)
        {
            return existing;
        }

        var tag = new Tag
        {
            Name = name,
            Color = color
        };
        await CreateAsync(tag);
        return tag;
    }
}
