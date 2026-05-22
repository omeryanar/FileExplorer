using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Reflection;
using System.Text.Json;
using DevExpress.Mvvm.POCO;

namespace FileExplorer.Extension.VideoPreview
{
    public class VideoPreviewSettings
    {
        public static Settings Default { get; private set; }

        public static string FilePath { get; private set; }

        public static void Save()
        {
            JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(Default, options);

            File.WriteAllText(FilePath, json);
        }

        public static Settings Load()
        {
            Settings defaultSettings = ViewModelSource.Create<Settings>();

            try
            {
                string json = File.ReadAllText(FilePath);
                Settings settings = JsonSerializer.Deserialize<Settings>(json);

                defaultSettings.SetValues(settings);
                return defaultSettings;
            }
            catch (Exception)
            {
                return defaultSettings;
            }    
        }

        public class Settings
        {
            public virtual bool UseDirectShow { get; set; }

            public virtual bool ShowThumbnails { get; set; }

            public virtual bool CacheThumbnails { get; set; } = true;

            public virtual bool RememberPlaybackPosition { get; set; } = true;

            public virtual int ThumbnailRows { get; set; } = 2;

            public virtual int ThumbnailColumns { get; set; } = 2;

            public virtual int NextTimeJump { get; set; } = 60;

            public virtual int PreviousTimeJump { get; set; } = 60;

            public virtual LoadBehavior LoadBehavior { get; set; }

            public virtual TimestampPosition TimestampPosition { get; set; } = TimestampPosition.BottomRight;

            public void SetValues(Settings settings)
            {
                UseDirectShow = settings.UseDirectShow;
                ShowThumbnails = settings.ShowThumbnails;
                CacheThumbnails = settings.CacheThumbnails;
                RememberPlaybackPosition = settings.RememberPlaybackPosition;
                ThumbnailRows = settings.ThumbnailRows;
                ThumbnailColumns = settings.ThumbnailColumns;
                NextTimeJump = settings.NextTimeJump;
                PreviousTimeJump = settings.PreviousTimeJump;
                LoadBehavior = settings.LoadBehavior;
                TimestampPosition = settings.TimestampPosition;
            }
        }

        static VideoPreviewSettings()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            FilePath = Path.Combine(Path.GetDirectoryName(assembly.Location), "settings.json");

            Default = Load();
        }
    }

	public enum LoadBehavior
	{
		[Display(Name = "Stop", Description = "StopDescription", ResourceType = typeof(Properties.Resources))]
		None,

		[Display(Name = "Pause", Description = "PauseDescription", ResourceType = typeof(Properties.Resources))]
		Pause,

		[Display(Name = "Play", Description = "PlayDescription", ResourceType = typeof(Properties.Resources))]
		Play
	}

	public enum TimestampPosition
    {
        [Display(Name = "None", ResourceType = typeof(Properties.Resources))]
        None,

        [Display(Name = "TopLeft", ResourceType = typeof(Properties.Resources))]
        TopLeft,

        [Display(Name = "TopRight", ResourceType = typeof(Properties.Resources))]
        TopRight,

        [Display(Name = "BottomLeft", ResourceType = typeof(Properties.Resources))]
        BottomLeft,

        [Display(Name = "BottomRight", ResourceType = typeof(Properties.Resources))]
        BottomRight
    }
}
