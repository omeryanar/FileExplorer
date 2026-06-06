using System;
using System.Threading;
using System.Threading.Tasks;
using WPFMediaKit.DirectShow.MediaPlayers;

namespace FileExplorer.Extension.VideoPreview.Common
{
	public class VideoMetadata : IDisposable
	{
		public string FilePath { get; }

		public bool HasVideo => Player.HasVideo;

		public double Duration => Player.Duration / MediaPlayerBase.DSHOW_ONE_SECOND_UNIT;

		public MediaUriPlayer Player { get; private set; }

		public VideoMetadata(string filePath)
		{
			FilePath = filePath;
			Player = new MediaUriPlayer();
		}

		public Task Read()
		{
			taskCompletion = new TaskCompletionSource<bool>();
			
			Player.EnsureThread(ApartmentState.MTA);
			Player.MediaOpened += Player_MediaOpened;
			Player.MediaFailed += Player_MediaFailed;

			Player.Dispatcher.BeginInvoke(new Action(() =>
			{
				Player.AudioDecoder = null;
				Player.AudioRenderer = null;
				Player.Source = new Uri(FilePath);
			}));

			return taskCompletion.Task;
		}

		public void Dispose()
		{
			Dispose(true);
		}

		private void Player_MediaOpened()
		{
			taskCompletion.TrySetResult(true);
		}

		private void Player_MediaFailed(object sender, MediaFailedEventArgs e)
		{
			Exception exception = e.Exception;
			if (exception == null)
				exception = new WPFMediaKit.WPFMediaKitException(e.Message);

			taskCompletion.TrySetException(exception);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposing)
				return;

			if (Player != null)
			{
				Player.Dispose();
				Player = null;
			}
		}

		private TaskCompletionSource<bool> taskCompletion;
	}
}
