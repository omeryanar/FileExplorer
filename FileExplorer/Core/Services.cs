using System.Collections.Generic;
using System.Linq;
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
        IList<string> GetAllFields();
        IList<string> GetVisibleFields();
    }

    public class ActiveWindowService : CurrentWindowService, IActiveWindowService
    {
        public bool IsActive => ActualWindow.IsActive;
    }

    public class DataUpdateService : ServiceBase, IDataUpdateService
    {
        GridControl DataControl { get { return AssociatedObject as GridControl; } }

        public void BeginUpdate()
        {
            DataControl.BeginDataUpdate();
        }

        public void EndUpdate()
        {
            DataControl.EndDataUpdate();
        }

        public IList<string> GetAllFields()
        {
            return DataControl.Columns.Select(x => x.FieldName).ToList();
        }

        public IList<string> GetVisibleFields()
        {
            return DataControl.Columns.Where(x => x.Visible).Select(x => x.FieldName).ToList();
        }
    }
}
