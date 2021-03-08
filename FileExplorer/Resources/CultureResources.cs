using System.Globalization;
using System.Threading;
using System.Windows.Data;
using DevExpress.Xpf.Core;

namespace FileExplorer.Resources
{
    public static class CultureResources
    {
        static CultureResources()
        {
            ObjectDataProvider = new ObjectDataProvider();
            ObjectDataProvider.ObjectInstance = new Properties.Resources();
        }

        public static ObjectDataProvider ObjectDataProvider { get; private set; }

        public static void ChangeCulture(string cultureName)
        {
            try
            {
                CultureInfo culture = new CultureInfo(cultureName);
                ChangeCulture(culture);
            }
            catch { return; }
        }

        public static void ChangeCulture(CultureInfo culture)
        {
            try
            {
                Properties.Resources.Culture = culture;
                DXMessageBoxLocalizer.Active = new CustomMessageBoxLocalizer();

                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;

                ObjectDataProvider.Refresh();
            }
            catch { return; }
        }
    }

    public class CustomMessageBoxLocalizer : DXMessageBoxLocalizer
    {
        protected override void PopulateStringTable()
        {
            base.PopulateStringTable();

            AddString(DXMessageBoxStringId.Ok, Properties.Resources.OK);
            AddString(DXMessageBoxStringId.Cancel, Properties.Resources.Cancel);
            AddString(DXMessageBoxStringId.Yes, Properties.Resources.Yes);
            AddString(DXMessageBoxStringId.No, Properties.Resources.No);
        }
    }
}
