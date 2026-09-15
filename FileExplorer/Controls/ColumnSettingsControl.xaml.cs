using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using FileExplorer.Helpers;
using FileExplorer.Properties;

namespace FileExplorer.Controls
{
	public partial class ColumnSettingsControl : UserControl
	{
		public ColumnSettingsControl()
		{
			InitializeComponent();

			Loaded += (s, e) =>
			{
				if (DefaultLayoutStream == null)
				{
					DefaultLayoutStream = new MemoryStream();
					FileListView.SaveLayoutToStream(DefaultLayoutStream);
				}

				if (!String.IsNullOrEmpty(Settings.Default.ColumnSettings))
				{
					using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(Settings.Default.ColumnSettings)))
					{
						FileListView.RestoreLayoutFromStream(stream);
					}
				}
			};
		}

		public void LoadDefaultLayout()
		{
			DefaultLayoutStream.Position = 0;
			FileListView.RestoreLayoutFromStream(DefaultLayoutStream);

			DefaultLayoutLoaded = true;
		}

		private void GridLayoutHelper_LayoutChanged(object sender, LayoutChangedEventArgs e)
		{
			if (DefaultLayoutLoaded)
			{
				Settings.Default.ColumnSettings = String.Empty;
				DefaultLayoutLoaded = false;
			}
			else if (e.LayoutChangedTypes.Intersect(BasicChangedTypes).Any() &&
				e.LayoutChangedTypes.Contains(LayoutChangedType.ColumnsCollection) == false)
			{
				using (MemoryStream stream = new MemoryStream())
				{
					FileListView.SaveLayoutToStream(stream);
					stream.Position = 0;

					using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
					{
						Settings.Default.ColumnSettings = reader.ReadToEnd();
					}
				}
			}
		}

		private bool DefaultLayoutLoaded;

		private static readonly LayoutChangedType[] BasicChangedTypes =
		[
			LayoutChangedType.SortingChanged,
			LayoutChangedType.ColumnGroupIndex,
			LayoutChangedType.ColumnVisibleIndex,
			LayoutChangedType.ColumnVisible
		];

		private static MemoryStream DefaultLayoutStream;
	}
}
