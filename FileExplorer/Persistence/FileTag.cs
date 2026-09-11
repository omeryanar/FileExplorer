using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using FileExplorer.Core;
using FileExplorer.Helpers;
using MahApps.Metro.IconPacks;

namespace FileExplorer.Persistence
{
	public class TagFolder : PersistentTrackedItem
	{
		[Required]
		[Unique(CollectionName = "TagFolders", ErrorMessageResourceName = "UniqueTagName", ErrorMessageResourceType = typeof(Properties.Resources))]
		public string Name
		{
			get => name;
			set
			{
				if (name != value)
				{
					name = value;
					RaisePropertyChanged(nameof(Name));
				}
			}
		}
		private string name;

		[Required]
		public string IconData
		{
			get => iconData;
			set
			{
				if (iconData != value)
				{
					iconData = value;
					RaisePropertyChanged(nameof(IconData));
				}
			}
		}
		private string iconData;

		[Required]
		public string IconColor
		{
			get => iconColor;
			set
			{
				if (iconColor != value)
				{
					iconColor = value;
					RaisePropertyChanged(nameof(IconColor));
				}
			}
		}
		private string iconColor;

		public ObservableCollection<string> TaggedFiles { get; set; } = new ObservableCollection<string>();

		public bool AddFileTag(string filePath)
		{
			return App.TagManager.AddFileTag(filePath, this);
		}

		public bool RemoveFileTag(string filePath)
		{
			return App.TagManager.RemoveFileTag(filePath, this);
		}

		public TagFolder()
		{
			Name = Properties.Resources.New;
			IconColor = "#FFFFEB3B";
			IconData = FontIconHelper.GetIconData(PackIconFontAwesomeKind.TagSolid);
		}

		public override string ToString()
		{
			return Name;
		}
	}
}
