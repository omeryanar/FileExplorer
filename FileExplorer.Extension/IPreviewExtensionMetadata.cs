using System.ComponentModel;

namespace FileExplorer.Extension
{
    public interface IPreviewExtensionMetadata
    {
        string AssemblyName { get; }

        string DisplayName { get; }

        [DefaultValue("1.0")]
        string Version { get; }
    }
}
