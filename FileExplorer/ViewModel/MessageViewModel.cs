using FileExplorer.Core;

namespace FileExplorer.ViewModel
{
    public class MessageViewModel
    {
        public virtual IconType Icon { get; set; }

        public virtual string Title { get; set; }

        public virtual string Content { get; set; }

        public virtual string Details { get; set; }
    }
}
