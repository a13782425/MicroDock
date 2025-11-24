using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnityProjectPlugin.Services
{
    public class FilePickerService : IFilePickerService
    {
        private readonly Visual _visual;

        public FilePickerService(Visual visual)
        {
            _visual = visual ?? throw new ArgumentNullException(nameof(visual));
        }

        public async Task<string?> PickSingleFolderAsync(string title)
        {
            var topLevel = TopLevel.GetTopLevel(_visual);
            if (topLevel == null) return null;

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = title,
                AllowMultiple = false
            });

            if (folders.Count > 0)
            {
                return folders[0].Path.LocalPath;
            }

            return null;
        }
    }
}
