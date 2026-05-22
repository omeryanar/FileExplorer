using System;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using FileExplorer.Common;
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

		public virtual string ErrorMessage { get; set; }

		public virtual object ThumbnailImage { get; set; }

		public virtual object AudioAlbumImage { get; set; }

		public virtual IDialogService DialogService { get { return null; } }

		public File MediaInfo { get; protected set; }

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
		}, () => MediaInfo != null);

		public static PersistentDictionary<string, int> History { get; private set; }

		public virtual async Task PreviewFile(string filePath)
		{
			MediaInfo = File.Create(filePath);
			bool isVideo = MediaInfo?.Properties?.MediaTypes.HasFlag(MediaTypes.Video) == true;
			bool isAudio = MediaInfo?.Properties?.MediaTypes.HasFlag(MediaTypes.Audio) == true;

			if (isVideo || isAudio)
				Duration = MediaInfo.Properties.Duration.TotalSeconds;

			if (isAudio && !isVideo && MediaInfo?.Tag != null && MediaInfo.Tag.Pictures?.Length > 0)
				AudioAlbumImage = MediaInfo.Tag.Pictures[0].Data.Data;
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

			if (VideoPreviewSettings.Default.RememberPlaybackPosition && History.ContainsKey(MediaInfo.Name))
				Seek(History[MediaInfo.Name]);
		}

		public virtual void Eject()
		{
			Pause();

			if (VideoPreviewSettings.Default.RememberPlaybackPosition && PlaybackPosition > 0)
				History[MediaInfo.Name] = PlaybackPosition;

			Opened = false;
			Loaded = false;
		}

		public virtual async Task GenerateThumbnails(string filePath)
		{
			ThumbnailImage = await VideoThumbnailHelper.GenerateThumbnails(new Uri(filePath),
				VideoPreviewSettings.Default.ThumbnailRows, VideoPreviewSettings.Default.ThumbnailColumns, VideoPreviewSettings.Default.TimestampPosition);
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
