using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using DevExpress.Mvvm;
using FileExplorer.Helpers;
using FileExplorer.Model;
using FileExplorer.Persistence;

namespace FileExplorer.Core
{
	public abstract class SelectedItemsConverter : MarkupExtension, IMultiValueConverter
	{
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}

		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			if (values.Length > 0 && values[0] is IList selectedItems)
			{
				foreach (INotifyCollectionChanged collection in values)
					collection.CollectionChanged += (s, e) => { Convert(selectedItems); };

				return Convert(selectedItems);
			}

			return null;
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			return null;
		}

		public abstract object Convert(IList items);
	}

	public class TagBarCheckItemConverter : SelectedItemsConverter
	{
		public ObservableCollection<TagBarCheckItemModel> TagBarCheckItems { get; private set; }

		public override object Convert(IList items)
		{
			if (TagBarCheckItems == null)
			{
				TagBarCheckItems = new ObservableCollection<TagBarCheckItemModel>();
				App.Repository.TagFolders.ItemUpdated += (s, e) => { Convert(items); };
			}

			TagBarCheckItems.Clear();
			if (items.Count == 0)
				return TagBarCheckItems;

			List<FileModel> fileModels = items.OfType<FileModel>().ToList();
			foreach (TagFolder tagFolder in App.Repository.TagFolders)
			{
				TagBarCheckItemModel tagBarCheckItemModel = new TagBarCheckItemModel(tagFolder, fileModels);
				tagBarCheckItemModel.IsChecked = fileModels.All(x => App.TagManager.GetTagFolders(x.FullPath)?.Any(y => y == tagFolder) == true);

				TagBarCheckItems.Add(tagBarCheckItemModel);
			}

			return TagBarCheckItems;
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

						if (addOrRemove)
							item.Tags = App.TagManager.GetTagFolders(item.FullPath);

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
