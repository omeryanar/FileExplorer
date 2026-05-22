using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FileExplorer.Common.Helper;
using WPFMediaKit.DirectShow.Controls;
using WPFMediaKit.DirectShow.MediaPlayers;

namespace FileExplorer.Extension.VideoPreview
{
    public static class VideoThumbnailHelper
    {
        public static async Task<ImageSource> GenerateThumbnails(Uri source, int rows, int columns, TimestampPosition timestampPosition)
        {
            string file = Path.ChangeExtension(source.LocalPath, null);
            string cacheFile = $"{file}_{rows}x{columns}.jpg";
            if (timestampPosition != TimestampPosition.None)
                cacheFile = $"{file}_{rows}x{columns}_{timestampPosition}.jpg";

            ImageSource image = await ImageCache.TryGetValue(cacheFile);
            if (image == null)
            {
                using (VideoScreenGrabber grabber = new VideoScreenGrabber())
                {
                    await grabber.Open(source);
                    long duration = grabber.MediaDuration;
                    long slice = duration / (rows * columns + 1);

                    decimal widthScaleFactor = 1M;
                    decimal heighthScaleFactor = 1M;
                    if (rows <= columns)
                        heighthScaleFactor = Decimal.Divide(rows, columns);
                    else
                        widthScaleFactor = Decimal.Divide(columns, rows);

                    int videoWidth = 0;
                    int videoHeight = 0;

                    DrawingVisual drawingVisual = new DrawingVisual();
                    double pixelsPerDip = VisualTreeHelper.GetDpi(drawingVisual).PixelsPerDip;
                    Typeface typeface = new Typeface(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

                    using (DrawingContext drawingContext = drawingVisual.RenderOpen())
                    {
                        for (int i = 0; i < rows; i++)
                        {
                            for (int j = 0; j < columns; j++)
                            {
                                int index = i * columns + j;
                                long position = slice * (index + 1);

                                IntPtr buffer = await grabber.GrabAtPosition(position);
                                D3DImage d3dImage = new D3DImage();
                                D3DImageUtils.SetBackBufferWithLock(d3dImage, buffer);

                                if (videoWidth == 0 || videoHeight == 0)
                                {
                                    videoWidth = Convert.ToInt32(d3dImage.PixelWidth * widthScaleFactor);
                                    videoHeight = Convert.ToInt32(d3dImage.PixelHeight * heighthScaleFactor);
                                }

                                image = d3dImage.ToBitmapSource(Math.Max(rows, columns));
                                Rect rect = new Rect(j * image.Width, i * image.Height, image.Width, image.Height);

                                drawingContext.DrawRectangle(null, new Pen(Brushes.White, 2), rect);
                                drawingContext.DrawImage(image, rect);

                                if (timestampPosition != TimestampPosition.None)
                                {
                                    TimeSpan timeSpan = TimeSpan.FromTicks(position);
                                    FormattedText text = new($"{timeSpan:hh\\:mm\\:ss}", Thread.CurrentThread.CurrentCulture, FlowDirection.LeftToRight, typeface, 15.0, Brushes.Black, pixelsPerDip);

                                    Point textLocation = new Point(rect.TopLeft.X + 10, rect.TopLeft.Y + 10);
                                    Point shadowLocation = new Point(rect.TopLeft.X + 11, rect.TopLeft.Y + 11);
                                    switch (timestampPosition)
                                    {
                                        case TimestampPosition.TopRight:
                                            textLocation = new Point(rect.TopRight.X - 10 - text.Width, rect.TopRight.Y + 10);
                                            shadowLocation = new Point(rect.TopRight.X - 9 - text.Width, rect.TopRight.Y + 11);
                                            break;

                                        case TimestampPosition.BottomLeft:
                                            textLocation = new Point(rect.BottomLeft.X + 10, rect.BottomLeft.Y - 10 - text.Height);
                                            shadowLocation = new Point(rect.BottomLeft.X + 11, rect.BottomLeft.Y - 9 - text.Height);
                                            break;

                                        case TimestampPosition.BottomRight:
                                            textLocation = new Point(rect.BottomRight.X - 10 - text.Width, rect.BottomRight.Y - 10 - text.Height);
                                            shadowLocation = new Point(rect.BottomRight.X - 9 - text.Width, rect.BottomRight.Y - 9 - text.Height);
                                            break;
                                    }

                                    drawingContext.DrawText(text, shadowLocation);
                                    text.SetForegroundBrush(Brushes.White);
                                    drawingContext.DrawText(text, textLocation);
                                }
                            }
                        }
                    }

                    RenderTargetBitmap rtb = new RenderTargetBitmap(videoWidth, videoHeight, 96, 96, PixelFormats.Pbgra32);
                    rtb.Render(drawingVisual);

                    if (VideoPreviewSettings.Default.CacheThumbnails)
                    {
                        JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(rtb));

                        using (MemoryStream memoryStream = new MemoryStream())
                        {
                            encoder.Save(memoryStream);
                            memoryStream.Position = 0;

                            image = await ImageCache.GetOrAddValue(cacheFile, memoryStream);
                        }
                    }
                    else
                        return BitmapFrame.Create(rtb);
                }
            }

            return image;
        }

        private static BitmapSource ToBitmapSource(this D3DImage d3dImage, int divisor)
        {
            int width = d3dImage.PixelWidth / divisor;
            int height = d3dImage.PixelHeight / divisor;

            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext dc = dv.RenderOpen())
            {
                dc.DrawImage(d3dImage, new Rect(0, 0, width, height));
            }

            RenderTargetBitmap rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(dv);

            return BitmapFrame.Create(rtb);
        }

        private class VideoScreenGrabber : IDisposable
        {
            private TaskCompletionSource<bool> taskCompletionOpen;
            private TaskCompletionSource<IntPtr> taskCompletionGrab;
            private CancellationTokenRegistration cancellationRegistrationGrab;

            public MediaUriPlayer Player { get; private set; }

            public IntPtr BackBuffer { get; private set; }

            public long MediaDuration
            {
                get
                {
                    CheckPlayer();
                    return Player.Duration;
                }
            }

            public double MediaDurationSecond
                => (double)MediaDuration / MediaPlayerBase.DSHOW_ONE_SECOND_UNIT;

            public bool IsGrabbing
                => taskCompletionGrab != null;

            public Task Open(Uri source)
            {
                if (Player != null)
                    throw new ArgumentException("Cannot open twice!");
                taskCompletionOpen = new TaskCompletionSource<bool>();

                Player = new MediaUriPlayer();
                Player.EnsureThread(ApartmentState.MTA);
                Player.MediaOpened += MediaUriPlayer_MediaOpened;
                Player.MediaFailed += MediaUriPlayer_MediaFailed;
                Player.NewAllocatorFrame += Player_NewAllocatorFrame;
                Player.NewAllocatorSurface += Player_NewAllocatorSurface;

                Player.Dispatcher.BeginInvoke(new Action(() =>
                {
                    Player.AudioDecoder = null;
                    Player.AudioRenderer = null;
                    Player.Source = source;
                }));

                return taskCompletionOpen.Task;
            }

            public Task<IntPtr> GrabAtSecond(double second)
                => GrabAtPosition((long)(second * MediaPlayerBase.DSHOW_ONE_SECOND_UNIT));

            public Task<IntPtr> GrabAtPosition(long position, CancellationToken cancellationToken = default(CancellationToken))
            {
                CheckPlayer();
                if (taskCompletionGrab != null)
                    throw new InvalidOperationException("Still grabbing previous frame.");
                if (position < 0)
                    throw new ArgumentException("position negative.");
                if (position > MediaDuration)
                    throw new ArgumentException("position beyond the media duration.");

                taskCompletionGrab = new TaskCompletionSource<IntPtr>();
                cancellationRegistrationGrab = cancellationToken.Register(CancelGrab);
                Player.Dispatcher.BeginInvoke(new Action(() =>
                {
                    Player.MediaPosition = position;
                    Player.Pause();
                }));

                return taskCompletionGrab.Task;
            }

            public void CancelGrab()
            {
                taskCompletionGrab?.TrySetCanceled();
                taskCompletionGrab = null;
                cancellationRegistrationGrab.Dispose();
            }

            private void CheckPlayer()
            {
                if (Player == null)
                    throw new InvalidOperationException("Player not opened, call Open() first.");
            }

            private void MediaUriPlayer_MediaFailed(object sender, MediaFailedEventArgs e)
            {
                Exception exc = e.Exception;
                if (exc == null)
                    exc = new WPFMediaKit.WPFMediaKitException(e.Message);
                taskCompletionOpen.TrySetException(exc);
            }

            private void MediaUriPlayer_MediaOpened()
                => taskCompletionOpen.TrySetResult(true);

            private void Player_NewAllocatorSurface(object sender, IntPtr pSurface)
                => BackBuffer = pSurface;

            private void Player_NewAllocatorFrame()
            {
                if (taskCompletionGrab == null)
                    return;
                taskCompletionGrab.TrySetResult(BackBuffer);
                taskCompletionGrab = null;
            }

            public void Dispose()
            {
                Dispose(true);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!disposing)
                    return;
                CancelGrab();
                if (Player != null)
                {
                    Player.Dispose();
                    Player = null;
                }
            }
        }
    }
}
