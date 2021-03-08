using System.Linq;
using DevExpress.Mvvm;
using DevExpress.Xpf.Bars;

namespace FileExplorer.Controls
{
    public partial class MenuSubItemControl : BarSubItem
    {
        public MenuSubItemControl()
        {
            InitializeComponent();

            Command = new DelegateCommand(AnyItemExecute, CanAnyItemExecute);
        }

        private void AnyItemExecute()
        {
        }

        private bool CanAnyItemExecute()
        {
            return Items.OfType<BarItem>().Any(x => x.Command.CanExecute(x.CommandParameter));
        }
    }
}
