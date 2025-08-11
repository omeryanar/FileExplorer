using FileExplorer.Extension;
using LiteDB;

namespace FileExplorer.Persistence
{
    public class ExtensionMetadata : PersistentItem, IPreviewExtensionMetadata
    {
        public string AssemblyName { get; set; }

        public string DisplayName { get; set; }

        public string SupportedFileTypes { get; set; }

        public string Version { get; set; }

        public string Preferred { get; set; }

        public string Error { get; set; }

        public bool Disabled { get; set; }

        [BsonCtor]
        public ExtensionMetadata() { }

        public ExtensionMetadata(IPreviewExtensionMetadata extensionMetadata) 
        {
            DisplayName = extensionMetadata.DisplayName;
            SupportedFileTypes = extensionMetadata.SupportedFileTypes;
            Version = extensionMetadata.Version;
        }

        public override string ToString()
        {
            return AssemblyName;
        }
    }
}
