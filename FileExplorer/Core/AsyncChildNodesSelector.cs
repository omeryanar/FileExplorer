using System;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.Xpf.Grid;
using FileExplorer.Helpers;
using FileExplorer.Model;

namespace FileExplorer.Core
{
    public class AsyncChildNodesSelector : IAsyncChildNodesSelector
    {
        public async Task<bool> HasChildNode(object item, CancellationToken token)
        {
            if (item is FileModel fileModel)
            {
                if (fileModel.Folders == null)
                    fileModel.Folders = await FileSystemHelper.GetFolders(fileModel);

                return fileModel.Folders.Count > 0;
            }

            return false;
        }

        public IEnumerable SelectChildren(object item)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable> SelectChildrenAsync(object item, CancellationToken token)
        {
            if (item is FileModel fileModel)
            {
                if (fileModel.Folders == null)
                    fileModel.Folders = await FileSystemHelper.GetFolders(fileModel);

                return fileModel.Folders;
            }

            return null;
        }
    }
}
