using DevExpress.Mvvm;
using DevExpress.Mvvm.UI;
using DevExpress.Xpf.Grid;

namespace FileExplorer.Core
{
    public interface IActiveWindowService : ICurrentWindowService
    {
        bool IsActive { get; }
    }

    public interface IDataUpdateService
    {
        void BeginUpdate();
        void EndUpdate();
    }

    public class ActiveWindowService : CurrentWindowService, IActiveWindowService
    {
        public bool IsActive => ActualWindow.IsActive;
    }

    public class DataUpdateService : ServiceBase, IDataUpdateService
    {
        DataControlBase DataControl { get { return AssociatedObject as GridControl; } }

        public void BeginUpdate()
        {
            DataControl.BeginDataUpdate();
        }

        public void EndUpdate()
        {
            DataControl.EndDataUpdate();
        }
    }
}
