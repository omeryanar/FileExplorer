using System.Collections.Generic;
using FileExplorer.Core;

namespace FileExplorer.Messages
{
    public class CommandMessage
    {
        public CommandType CommandType { get; set; }

        public IEnumerable<object> Parameters { get; set; }
    }
}
