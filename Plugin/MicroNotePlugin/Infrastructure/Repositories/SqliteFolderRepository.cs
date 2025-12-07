using MicroNotePlugin.Core.Entities;
using MicroNotePlugin.Core.Interfaces;
using MicroNotePlugin.Infrastructure.Database;

namespace MicroNotePlugin.Infrastructure.Repositories;

/// <summary>
/// SQLite 文件夹仓储实现
/// </summary>
public class SqliteFolderRepository : IFolderRepository
{
    private readonly NoteDbContext _context;

    public SqliteFolderRepository(NoteDbContext context)
    {
        _context = context;
    }

    public async Task<Folder?> GetByIdAsync(string id)
    {
        return await _context.Connection.FindAsync<Folder>(id);
    }

    public async Task<Folder?> GetByPathAsync(string path)
    {
        return await _context.Connection.Table<Folder>()
            .Where(f => f.Path == path)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Folder>> GetAllAsync()
    {
        return await _context.Connection.Table<Folder>()
            .OrderBy(f => f.Path)
            .ToListAsync();
    }

    public async Task<IEnumerable<Folder>> GetChildrenAsync(string? parentId)
    {
        if (parentId == null)
        {
            return await _context.Connection.Table<Folder>()
                .Where(f => f.ParentId == null)
                .OrderBy(f => f.Name)
                .ToListAsync();
        }
        
        return await _context.Connection.Table<Folder>()
            .Where(f => f.ParentId == parentId)
            .OrderBy(f => f.Name)
            .ToListAsync();
    }

    public async Task<Folder> CreateAsync(Folder folder)
    {
        folder.CreatedAt = DateTime.Now;
        await _context.Connection.InsertAsync(folder);
        return folder;
    }

    public async Task<Folder> CreateAsync(string name, string? parentId = null)
    {
        // 构建路径
        string path;
        if (parentId == null)
        {
            path = "/" + name;
        }
        else
        {
            var parent = await GetByIdAsync(parentId);
            path = parent != null ? parent.Path + "/" + name : "/" + name;
        }

        var folder = new Folder
        {
            Name = name,
            ParentId = parentId,
            Path = path,
            CreatedAt = DateTime.Now
        };

        await _context.Connection.InsertAsync(folder);
        return folder;
    }

    public async Task<Folder> EnsurePathExistsAsync(string path)
    {
        var existing = await GetByPathAsync(path);
        if (existing != null) return existing;

        // 递归创建
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        string? currentParentId = null;
        Folder? currentFolder = null;

        foreach (var part in parts)
        {
            var currentPath = currentFolder == null ? "/" + part : currentFolder.Path + "/" + part;
            var folder = await GetByPathAsync(currentPath);
            
            if (folder == null)
            {
                folder = await CreateAsync(part, currentParentId);
            }
            
            currentFolder = folder;
            currentParentId = folder.Id;
        }

        return currentFolder!;
    }

    public async Task UpdateAsync(Folder folder)
    {
        await _context.Connection.UpdateAsync(folder);
    }

    public async Task DeleteAsync(string id)
    {
        // 递归删除子文件夹
        var children = await GetChildrenAsync(id);
        foreach (var child in children)
        {
            await DeleteAsync(child.Id);
        }

        // 将该文件夹下的笔记移到根目录
        await _context.Connection.ExecuteAsync(
            "UPDATE notes SET folder_id = NULL WHERE folder_id = ?", id);

        // 删除文件夹
        await _context.Connection.DeleteAsync<Folder>(id);
    }

    public async Task RenameAsync(string id, string newName)
    {
        var folder = await GetByIdAsync(id);
        if (folder == null) return;

        var oldPath = folder.Path;
        var newPath = folder.ParentId == null
            ? "/" + newName
            : oldPath.Substring(0, oldPath.LastIndexOf('/') + 1) + newName;

        folder.Name = newName;
        folder.Path = newPath;
        await UpdateAsync(folder);

        // 更新所有子文件夹的路径
        await UpdateChildPathsAsync(oldPath, newPath);
    }

    public async Task MoveAsync(string id, string? newParentId)
    {
        var folder = await GetByIdAsync(id);
        if (folder == null) return;

        var oldPath = folder.Path;
        string newPath;

        if (newParentId == null)
        {
            newPath = "/" + folder.Name;
        }
        else
        {
            var newParent = await GetByIdAsync(newParentId);
            if (newParent == null) return;
            newPath = newParent.Path + "/" + folder.Name;
        }

        folder.ParentId = newParentId;
        folder.Path = newPath;
        await UpdateAsync(folder);

        // 更新所有子文件夹的路径
        await UpdateChildPathsAsync(oldPath, newPath);
    }

    private async Task UpdateChildPathsAsync(string oldPath, string newPath)
    {
        // 获取所有以 oldPath 开头的文件夹
        var allFolders = await _context.Connection.Table<Folder>().ToListAsync();
        var descendants = allFolders
            .Where(f => f.Path.StartsWith(oldPath + "/"))
            .ToList();

        foreach (var descendant in descendants)
        {
            descendant.Path = newPath + descendant.Path.Substring(oldPath.Length);
            await _context.Connection.UpdateAsync(descendant);
        }
    }
}
