using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using DevExpress.Mvvm;
using DevExpress.Xpf.Grid;
using FileExplorer.Helpers;
using FileExplorer.Model;
using FileExplorer.Persistence;

namespace FileExplorer.Core
{
	public abstract class SelectedItemsConverter : MarkupExtension, IValueConverter
	{
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (parameter is GridControl gridControl && gridControl.SelectedItems.Count > 0)
				return Convert(gridControl.SelectedItems.OfType<FileModel>().ToList());

			return null;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return null;
		}

		public abstract object Convert(IList<FileModel> items);
	}

	public class TagBarCheckItemConverter : SelectedItemsConverter
	{
		public override object Convert(IList<FileModel> items)
		{
			List<TagBarCheckItemModel> tags = new List<TagBarCheckItemModel>();
			
			foreach (TagFolder tagFolder in App.Repository.TagFolders)
			{
				TagBarCheckItemModel tagBarCheckItemModel = new TagBarCheckItemModel(tagFolder, items);
				tagBarCheckItemModel.IsChecked = items.All(x => App.TagManager.GetTagFolders(x.FullPath)?.Any(y => y == tagFolder) == true);

				tags.Add(tagBarCheckItemModel);
			}

			return tags;
		}

		public class TagBarCheckItemModel
		{
			public string Name { get; set; }

			public bool IsChecked { get; set; }

			public ImageSource Glyph { get; set; }

			public TagFolder TagFolder { get; private set; }

			public IList<FileModel> Items { get; private set; }

			public ICommand TagItemsCommand { get; private set; }

			public TagBarCheckItemModel(TagFolder tagFolder, IList<FileModel> items)
			{
				TagFolder = tagFolder;
				Items = items;

				Name = tagFolder.Name;
				Glyph = FontIconHelper.GetIconImage(tagFolder.IconData, tagFolder.IconColor);

				TagItemsCommand = new DelegateCommand(() =>
				{
					bool needUpdate = false;
					foreach (FileModel item in Items)
					{
						bool addOrRemove = false;
						
						if (IsChecked)
							addOrRemove = TagFolder.AddFileTag(item.FullPath);
						else
							addOrRemove = TagFolder.RemoveFileTag(item.FullPath);

						needUpdate |= addOrRemove;
					}

					if (needUpdate)
						App.Repository.TagFolders.Update(TagFolder);
				}, () =>
				{
					return Items?.Count > 0 && Items.All(x => !x.IsRoot);
				});
			}
		}
	}
}
