using System.Collections.Generic;
using System.IO;
using System.Windows;
using DevExpress.Mvvm;
using FileExplorer.Core;
using FileExplorer.Helpers;

namespace FileExplorer.ViewModel
{
	public class ShortcutViewModel : MessageViewModel
	{
		public virtual bool IsFolder { get; set; }

		public virtual string Path { get; set; }

		public virtual string Filter { get; set; }

		public List<UICommand> UICommandList { get; private set; }

		public ShortcutViewModel()
		{
			Icon = IconType.Question;
			Title = Properties.Resources.NotFound;
			Content = $"{Properties.Resources.NotFoundMessage}\r\n\r\n{Properties.Resources.DeleteUpdateTag}";

			UICommandList = new List<UICommand>
			{
				new UICommand
				{
					Glyph = FontIconHelper.ErrorIcon,
					GlyphAlignment = GlyphAlignment.Left,
					Caption = Properties.Resources.Delete,
					IsDefault = true,
					IsCancel = false
				},
				new UICommand
				{
					Glyph = FontIconHelper.RefreshIcon,
					GlyphAlignment = GlyphAlignment.Left,
					Caption = Properties.Resources.Update,
					Command = new DelegateCommand(() => { }, () => (IsFolder && Directory.Exists(Path) || File.Exists(Path))),
					IsDefault = false,
					IsCancel = false
				},
				new UICommand
				{
					Caption = Properties.Resources.Cancel,
					Id = MessageBoxResult.Cancel,
					IsDefault = false,
					IsCancel = true
				}
			};
		}
	}
}
