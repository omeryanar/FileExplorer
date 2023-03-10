using System.Threading.Tasks;

namespace FileExplorer.Extension
{
    public interface IPreviewExtension
    {
        bool CanPreviewFile(string filePath);

        Task PreviewFile(string filePath);

        Task UnloadFile();
    }
}
