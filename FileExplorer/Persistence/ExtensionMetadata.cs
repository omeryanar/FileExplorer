using FileExplorer.Extension;

namespace FileExplorer.Persistence
{
    public class ExtensionMetadata : PersistentItem, IPreviewExtensionMetadata
    {
        public string AssemblyName { get; set; }

        public string DisplayName { get; set; }

        public string Version { get; set; }

        public string Preferred { get; set; }

        public string Error { get; set; }

        public bool Disabled { get; set; }
        
        public ExtensionMetadata() { }

        public ExtensionMetadata(IPreviewExtensionMetadata extensionMetadata) 
        {
            AssemblyName = extensionMetadata.AssemblyName;
            DisplayName = extensionMetadata.DisplayName;
            Version = extensionMetadata.Version;
        }

        public override string ToString()
        {
            return AssemblyName;
        }
    }
}
