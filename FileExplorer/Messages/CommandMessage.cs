using System.Collections.Generic;
using FileExplorer.Core;
using FileExplorer.Model;

namespace FileExplorer.Messages
{
    public class CommandMessage
    {
        public CommandType CommandType { get; set; }

        public IEnumerable<FileModel> Parameters { get; set; }
    }
}
