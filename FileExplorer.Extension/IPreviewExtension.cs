using System.Threading.Tasks;

namespace FileExplorer.Extension
{
    public interface IPreviewExtension
    {
        Task PreviewFile(string filePath);

        Task UnloadFile();
    }
}
