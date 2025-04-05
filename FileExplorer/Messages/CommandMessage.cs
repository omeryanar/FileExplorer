using System.Collections.Generic;
using FileExplorer.Model;
using FileExplorer.Persistence;

namespace FileExplorer.Messages
{
    public class CommandMessage
    {
        public string Arguments { get; set; }

        public string Directory { get; set; }

        public MenuItem MenuItem { get; set; }

        public IEnumerable<FileModel> Parameters { get; set; }
    }
}
