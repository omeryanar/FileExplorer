using System;
using System.Windows.Threading;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using WPFMediaKit.DirectShow.MediaPlayers;

namespace FileExplorer.Extension.VideoPreview.ViewModel
{
	public class DirectShowVideoPlayerViewModel : VideoPlayerViewModel
	{
		[ServiceProperty(Key = "DirectShowVideoPlayerService")]
		public virtual IUIObjectService VideoPlayerService { get { return null; } }

		public dynamic VideoPlayer => VideoPlayerService.Object;

		public override int PlaybackPosition => Convert.ToInt32(Position / TimeSpan.TicksPerSecond);

		public DirectShowVideoPlayerViewModel()
		{
			DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Background);
			timer.Interval = TimeSpan.FromMilliseconds(100);
			timer.Tick += (s, e) =>
			{
				if (VideoPlayerService == null)
					return;

				IsPlaying = VideoPlayer.IsPlaying;
				if (IsPlaying)
				{
					PlayCommand.RaiseCanExecuteChanged();
					PauseCommand.RaiseCanExecuteChanged();
					StopCommand.RaiseCanExecuteChanged();
					NextCommand.RaiseCanExecuteChanged();
					PreviousCommand.RaiseCanExecuteChanged();
				}
			};
			timer.Start();
		}

		public override void UnloadFile()
		{
			base.UnloadFile();
			VideoPlayer.Source = null;
		}

		public override void Load()
		{
			if (VideoPreviewSettings.Default.LoadBehavior == LoadBehavior.Play)
				VideoPlayer.LoadedBehavior = MediaState.Play;
			else
				VideoPlayer.LoadedBehavior = MediaState.Pause;

			VideoPlayer.Source = new Uri(FilePath);
			base.Load();
		}

		public override void Eject()
		{
			VideoPlayer.Source = null;
			base.Eject();
		}

		public override void Play()
		{
			if (VideoPlayer.Source != null && !VideoPlayer.IsPlaying)
				VideoPlayer.Play();
		}

		public override void Pause()
		{
			if (VideoPlayer.Source != null && VideoPlayer.IsPlaying)
				VideoPlayer.Pause();
		}

		public override void Stop()
		{
			VideoPlayer.Stop();
			Position = 0;
		}

		public override void Next()
		{
			long jump = VideoPreviewSettings.Default.NextTimeJump * TimeSpan.TicksPerSecond;

			if (VideoPlayer.MediaDuration - VideoPlayer.MediaPosition > jump)
				VideoPlayer.MediaPosition += jump;
			else
				VideoPlayer.MediaPosition = VideoPlayer.MediaDuration;
		}

		public override void Previous()
		{
			long jump = VideoPreviewSettings.Default.PreviousTimeJump * TimeSpan.TicksPerSecond;

			if (VideoPlayer.MediaPosition > jump)
				VideoPlayer.MediaPosition -= jump;
			else
				VideoPlayer.MediaPosition = 0;
		}

		public override void Seek(int seconds)
		{
			Position = seconds * TimeSpan.TicksPerSecond;
		}

		public override void MediaOpened()
		{
			base.MediaOpened();

			PlayCommand.RaiseCanExecuteChanged();
			Duration = VideoPlayer.MediaDuration;
		}

		protected void OnIsMutedChanged()
		{
			if (IsMuted)
			{
				VolumeBeforeMute = VideoPlayer.Volume;
				VideoPlayer.Volume = 0;
			}
			else
			{
				if (VolumeBeforeMute > 0)
					VideoPlayer.Volume = VolumeBeforeMute;
				else
					VideoPlayer.Volume = 0.5;
			}
		}

		private double VolumeBeforeMute = 0.5;
	}
}
