using DevExpress.Mvvm;
using DevExpress.Mvvm.UI;

namespace FileExplorer.Core
{
    public interface IActiveWindowService : ICurrentWindowService
    {
        bool IsActive { get; }
    }

    public class ActiveWindowService : CurrentWindowService, IActiveWindowService
    {
        public bool IsActive => ActualWindow.IsActive;
    }
}
