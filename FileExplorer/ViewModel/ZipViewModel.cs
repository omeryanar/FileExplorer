using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.POCO;

namespace FileExplorer.ViewModel
{
    public abstract class ZipViewModel
    {
        public virtual string FilePath { get; set; }

        public virtual string Password { get; set; }

        public virtual double Progress { get; protected set; }        

        public virtual bool PasswordProtected { get; protected set; }        

        public virtual ICurrentWindowService CurrentWindowService { get { return null; } }

        public List<UICommand> UICommandList { get; private set; }

        public ZipViewModel()
        {
            DefaultCommand = new UICommand
            {
                Caption = Properties.Resources.OK,
                IsDefault = true,
                IsCancel = false,
                Command = this.GetAsyncCommand(x => x.Run(null))
            };

            CancelCommand = new UICommand
            {
                Caption = Properties.Resources.Cancel,
                IsDefault = false,
                IsCancel = true
            };

            UICommandList = new List<UICommand> { DefaultCommand, CancelCommand };
        }

        public async Task Run(CancelEventArgs args)
        {
            args.Cancel = true;

            AsyncCommand<CancelEventArgs> defaultCommand = DefaultCommand.Command as AsyncCommand<CancelEventArgs>;
            CancelCommand.Command = defaultCommand.CancelCommand;

            await RunCore(defaultCommand.CancellationTokenSource);

            CurrentWindowService.Close();
        }

        protected abstract Task RunCore(CancellationTokenSource cancellationToken);

        protected UICommand DefaultCommand;

        protected UICommand CancelCommand;
    }
}
