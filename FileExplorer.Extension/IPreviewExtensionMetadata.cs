using System.ComponentModel;

namespace FileExplorer.Extension
{
    public interface IPreviewExtensionMetadata
    {
        string DisplayName { get; }

        string SupportedFileTypes { get; }

        [DefaultValue("1.0")]
        string Version { get; }
    }
}
