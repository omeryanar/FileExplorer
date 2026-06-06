using System;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using FileExplorer.Common;
using FileExplorer.Extension.VideoPreview.Common;
using TagLib;

namespace FileExplorer.Extension.VideoPreview.ViewModel
{
	public abstract class VideoPlayerViewModel
	{
		public abstract void Play();

		public abstract void Pause();

		public abstract void Stop();

		public abstract void Next();

		public abstract void Previous();

		public abstract void Seek(int seconds);

		public abstract int PlaybackPosition { get; }

		public virtual bool Loaded { get; set; }

		public virtual bool Opened { get; set; }

		public virtual bool IsMuted { get; set; }

		public virtual bool IsPlaying { get; set; }

		public virtual double Position { get; set; }

		public virtual double Duration { get; set; }

		public virtual double Volume { get; set; } = 0.5;

		public virtual string FilePath { get; set; }

		public virtual string ErrorMessage { get; set; }

		public virtual object ThumbnailImage { get; set; }

		public virtual object AudioAlbumImage { get; set; }

		public virtual IDialogService DialogService { get { return null; } }

		public DelegateCommand PlayCommand => new DelegateCommand(Play, () => Opened);

		public DelegateCommand PauseCommand => new DelegateCommand(Pause, () => IsPlaying);

		public DelegateCommand StopCommand => new DelegateCommand(Stop, () => Opened);

		public DelegateCommand NextCommand => new DelegateCommand(Next, () => Opened);

		public DelegateCommand PreviousCommand => new DelegateCommand(Previous, () => Opened);

		public DelegateCommand TogglePlayPauseCommand => new DelegateCommand(() =>
		{
			if (IsPlaying)
				Pause();
			else
				Play();
		}, () => Opened);

		public DelegateCommand ToggleLoadEjectCommand => new DelegateCommand(() =>
		{
			if (Loaded)
				Eject();
			else
				Load();
		}, () => FilePath != null);

		public static PersistentDictionary<string, int> History { get; private set; }

		public virtual async Task PreviewFile(string filePath)
		{
			FilePath = filePath;
			Duration = 0;

			File mediaInfo = null;
			bool isVideo, isAudio;
			try
			{
				mediaInfo = File.Create(filePath);

				isVideo = mediaInfo.Properties?.MediaTypes.HasFlag(MediaTypes.Video) == true;
				isAudio = mediaInfo.Properties?.MediaTypes.HasFlag(MediaTypes.Audio) == true;

				if (isVideo || isAudio)
					Duration = mediaInfo.Properties.Duration.TotalSeconds;
			}
			catch
			{
				using (VideoMetadata videoMetadata = new VideoMetadata(filePath))
				{
					await videoMetadata.Read();

					isAudio = false;
					isVideo = videoMetadata.HasVideo;

					if (isVideo)
						Duration = videoMetadata.Duration;
				}
			}				

			if (isAudio && !isVideo && mediaInfo?.Tag != null && mediaInfo.Tag.Pictures?.Length > 0)
				AudioAlbumImage = mediaInfo.Tag.Pictures[0].Data.Data;
			else
				AudioAlbumImage = null;

			if (VideoPreviewSettings.Default.LoadBehavior != LoadBehavior.None)
				Load();

			ThumbnailImage = null;
			if (VideoPreviewSettings.Default.ShowThumbnails && isVideo)
				await GenerateThumbnails(filePath);
		}

		public virtual void UnloadFile()
		{
			Eject();
		}

		public virtual void Load()
		{
			Loaded = true;

			if (VideoPreviewSettings.Default.RememberPlaybackPosition && History.ContainsKey(FilePath))
				Seek(History[FilePath]);
		}

		public virtual void Eject()
		{
			Pause();

			if (VideoPreviewSettings.Default.RememberPlaybackPosition && PlaybackPosition > 0)
				History[FilePath] = PlaybackPosition;

			Opened = false;
			Loaded = false;
		}

		public virtual async Task GenerateThumbnails(string filePath)
		{
			try
			{
				ThumbnailImage = await VideoThumbnailHelper.GenerateThumbnails(new Uri(filePath),
					VideoPreviewSettings.Default.ThumbnailRows, VideoPreviewSettings.Default.ThumbnailColumns, VideoPreviewSettings.Default.TimestampPosition);
			}
			catch
			{
				ThumbnailImage = null;
			}
		}

		public virtual void MediaOpened()
		{
			Opened = true;
			ErrorMessage = String.Empty;
		}

		public virtual void ShowSettings()
		{
			Pause();

			var settings = VideoPreviewSettings.Load();
			if (DialogService.ShowDialog(MessageButton.OKCancel, Properties.Resources.Settings, settings) == MessageResult.OK)
			{
				var oldSettings = VideoPreviewSettings.Load();

				VideoPreviewSettings.Default.SetValues(settings);
				VideoPreviewSettings.Save();

				Messenger.Default.Send(new SettingsChangedMessage(oldSettings));
			}
		}

		static VideoPlayerViewModel()
		{
			History = new PersistentDictionary<string, int>("VideoPreviewHistory");
		}
	}
}
