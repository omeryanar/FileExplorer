using System;
using System.Windows.Media;
using System.Windows.Media.Animation;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;

namespace FileExplorer.Extension.VideoPreview.ViewModel
{
	public class DefaultVideoPlayerViewModel : VideoPlayerViewModel
	{
		[ServiceProperty(Key = "DefaultVideoPlayerService")]
		public virtual IUIObjectService VideoPlayerService { get { return null; } }

		public MediaClock MediaClock { get; private set; }

		public dynamic MediaPlayer => VideoPlayerService.Object;

		public override int PlaybackPosition => Convert.ToInt32(Position);

		public override void Load()
		{
			MediaTimeline mediaTimeline = new MediaTimeline(new Uri(MediaInfo.Name));
			MediaClock = mediaTimeline.CreateClock();

			MediaClock.Completed += OnCompleted;
			MediaClock.CurrentTimeInvalidated += OnCurrentTimeInvalidated;
			MediaClock.CurrentGlobalSpeedInvalidated += OnCurrentGlobalSpeedInvalidated;

			if (VideoPreviewSettings.Default.LoadBehavior == LoadBehavior.Play)
				Play();
			else
				Pause();

			base.Load();
		}

		public override void Eject()
		{
			MediaPlayer.ScrubbingEnabled = false;

			if (MediaClock != null)
			{
				MediaClock.Completed -= OnCompleted;
				MediaClock.CurrentTimeInvalidated -= OnCurrentTimeInvalidated;
				MediaClock.CurrentGlobalSpeedInvalidated -= OnCurrentGlobalSpeedInvalidated;

				MediaClock.Controller.Remove();
				MediaClock = null;
			}

			base.Eject();
		}

		public override void Play()
		{
			MediaPlayer.Clock = MediaClock;
			MediaClock?.Controller.Resume();
		}

		public override void Pause()
		{
			MediaPlayer.Clock = MediaClock;
			MediaClock?.Controller.Pause();
		}

		public override void Stop()
		{
			MediaClock?.Controller.Begin();
			MediaClock?.Controller.Pause();

			Position = 0;
		}

		public override void Next()
		{
			int jump = VideoPreviewSettings.Default.NextTimeJump;

			if (Duration - Position > jump)
				MediaClock?.Controller.Seek(TimeSpan.FromSeconds(Position + jump), TimeSeekOrigin.BeginTime);
			else
				MediaClock?.Controller.SkipToFill();
		}

		public override void Previous()
		{
			int jump = VideoPreviewSettings.Default.PreviousTimeJump;

			if (Position > jump)
				MediaClock?.Controller.Seek(TimeSpan.FromSeconds(Position - jump), TimeSeekOrigin.BeginTime);
			else
				MediaClock?.Controller.Begin();
		}

		public override void Seek(int seconds)
		{
			MediaPlayer.Clock = MediaClock;
			MediaClock?.Controller?.Seek(TimeSpan.FromSeconds(seconds), TimeSeekOrigin.BeginTime);
		}

		public override void MediaOpened()
		{
			base.MediaOpened();

			PlayCommand.RaiseCanExecuteChanged();
			MediaPlayer.ScrubbingEnabled = true;
			Duration = MediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
		}

		protected void OnPositionChanged()
		{
			if (MediaClock?.IsPaused == true)
				MediaClock.Controller?.Seek(TimeSpan.FromSeconds(Position), TimeSeekOrigin.BeginTime);
		}

		private void OnCompleted(object sender, EventArgs e)
		{
			Stop();
		}

		private void OnCurrentGlobalSpeedInvalidated(object sender, EventArgs e)
		{
			IsPlaying = MediaClock?.IsPaused == false;
		}

		private void OnCurrentTimeInvalidated(object sender, EventArgs e)
		{
			if (MediaClock?.CurrentTime.HasValue == true)
			{
				double position = Math.Truncate(MediaClock.CurrentTime.Value.TotalSeconds);
				if (Position != position)
					Position = position;
			}
		}
	}
}
