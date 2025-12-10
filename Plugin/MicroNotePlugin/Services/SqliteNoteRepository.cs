using MicroNotePlugin.Entities;
using MicroNotePlugin.Database;

namespace MicroNotePlugin.Services;

/// <summary>
/// SQLite 笔记仓储实现
/// </summary>
public class SqliteNoteRepository : INoteRepository
{
    private readonly NoteDbContext _context;

    public SqliteNoteRepository(NoteDbContext context)
    {
        _context = context;
    }

    public async Task<Note?> GetByIdAsync(string id)
    {
        return await _context.Connection.FindAsync<Note>(id);
    }

    public async Task<IEnumerable<Note>> GetAllAsync()
    {
        return await _context.Connection.Table<Note>()
            .OrderByDescending(n => n.ModifiedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Note>> GetByFolderIdAsync(string? folderId)
    {
        if (folderId == null)
        {
            return await _context.Connection.Table<Note>()
                .Where(n => n.FolderId == null)
                .OrderBy(n => n.Name)
                .ToListAsync();
        }
        
        return await _context.Connection.Table<Note>()
            .Where(n => n.FolderId == folderId)
            .OrderBy(n => n.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Note>> GetFavoritesAsync()
    {
        return await _context.Connection.Table<Note>()
            .Where(n => n.IsFavorite)
            .OrderByDescending(n => n.ModifiedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Note>> GetFrequentAsync(int count = 10)
    {
        return await _context.Connection.Table<Note>()
            .Where(n => n.OpenCount > 0)
            .OrderByDescending(n => n.OpenCount)
            .Take(count)
            .ToListAsync();
    }

    public async Task<IEnumerable<Note>> GetRecentAsync(int count = 10)
    {
        return await _context.Connection.Table<Note>()
            .Where(n => n.LastOpenedAt != null)
            .OrderByDescending(n => n.LastOpenedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<Note> CreateAsync(Note note)
    {
        note.CreatedAt = DateTime.Now;
        note.ModifiedAt = DateTime.Now;
        await _context.Connection.InsertAsync(note);
        return note;
    }

    public async Task UpdateAsync(Note note)
    {
        note.ModifiedAt = DateTime.Now;
        await _context.Connection.UpdateAsync(note);
    }

    public async Task DeleteAsync(string id)
    {
        // 删除关联的标签
        await _context.Connection.ExecuteAsync(
            "DELETE FROM note_tags WHERE note_id = ?", id);
        
        // 删除笔记
        await _context.Connection.DeleteAsync<Note>(id);
    }

    public async Task<bool> ToggleFavoriteAsync(string id)
    {
        var note = await GetByIdAsync(id);
        if (note == null) return false;

        note.IsFavorite = !note.IsFavorite;
        await UpdateAsync(note);
        return note.IsFavorite;
    }

    public async Task RecordOpenAsync(string id)
    {
        var note = await GetByIdAsync(id);
        if (note == null) return;

        note.OpenCount++;
        note.LastOpenedAt = DateTime.Now;
        await _context.Connection.UpdateAsync(note);
    }

    public async Task MoveAsync(string id, string? targetFolderId)
    {
        var note = await GetByIdAsync(id);
        if (note == null) return;

        note.FolderId = targetFolderId;
        await UpdateAsync(note);
    }

    public async Task RenameAsync(string id, string newName)
    {
        var note = await GetByIdAsync(id);
        if (note == null) return;

        note.Name = newName;
        await UpdateAsync(note);
    }

    public async Task AddTagAsync(string noteId, string tagId)
    {
        // 检查是否已存在
        var existing = await _context.Connection.Table<NoteTag>()
            .Where(nt => nt.NoteId == noteId && nt.TagId == tagId)
            .FirstOrDefaultAsync();

        if (existing == null)
        {
            await _context.Connection.InsertAsync(new NoteTag 
            { 
                NoteId = noteId, 
                TagId = tagId 
            });
        }
    }

    public async Task RemoveTagAsync(string noteId, string tagId)
    {
        await _context.Connection.ExecuteAsync(
            "DELETE FROM note_tags WHERE note_id = ? AND tag_id = ?", 
            noteId, tagId);
    }

    public async Task<IEnumerable<Tag>> GetTagsAsync(string noteId)
    {
        var tagIds = await _context.Connection.Table<NoteTag>()
            .Where(nt => nt.NoteId == noteId)
            .ToListAsync();

        var tags = new List<Tag>();
        foreach (var nt in tagIds)
        {
            var tag = await _context.Connection.FindAsync<Tag>(nt.TagId);
            if (tag != null)
            {
                tags.Add(tag);
            }
        }
        return tags;
    }

    public async Task<IEnumerable<Note>> GetByTagAsync(string tagId)
    {
        var noteIds = await _context.Connection.Table<NoteTag>()
            .Where(nt => nt.TagId == tagId)
            .ToListAsync();

        var notes = new List<Note>();
        foreach (var nt in noteIds)
        {
            var note = await GetByIdAsync(nt.NoteId);
            if (note != null)
            {
                notes.Add(note);
            }
        }
        return notes;
    }
}
