using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnityProjectPlugin.Services
{
    public interface IFilePickerService
    {
        Task<string?> PickSingleFolderAsync(string title);
    }
}
