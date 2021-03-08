using System;
using System.ComponentModel.DataAnnotations;

namespace FileExplorer.Persistence
{
    public class Expression : PersistentItem
    {
        public string Name { get; set; }

        [Required]
        public string Statement { get; set; }

        public override string ToString()
        {
            return String.IsNullOrEmpty(Name) ? Statement : Name;
        }
    }
}
