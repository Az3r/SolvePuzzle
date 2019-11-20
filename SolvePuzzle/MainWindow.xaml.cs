using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace SolvePuzzle
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            DataContext = this;
            InitializeComponent();
        }

        private void OnWindowDragging(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void WindowUnloaded(object sender, RoutedEventArgs e)
        {
            Trace.Listeners.Remove(mListener);
            mListener.Dispose();
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            Trace.Listeners.Add(mListener);
            // set timer
            TimerLabel.Content = $"{mLimitTime:0.00}";
            mTimer.Elapsed += OnUpdateTimer;
            // set default difficulty
            mGame.MapSize = 3;
            //set timer handler
            // load all image paths in images directory
            if (!Directory.Exists(mImageDirectory)) Directory.CreateDirectory(mImageDirectory);
            List<string> imgPaths = new List<string>();
            foreach (string pattern in IMAGE_PATTERNS)
            {
                imgPaths.AddRange(Directory.GetFiles(mImageDirectory, pattern, SearchOption.TopDirectoryOnly));
            }

            // add to list
            foreach (string path in imgPaths)
            {
                mImagePaths.Add(path);
            }
            mCurrentIndex = -1;
            mGame.CreateMap();
        }

        private void OnUpdateTimer(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                double time = mLimitTime - mStopWatch.Elapsed.TotalSeconds;
                if (mLimitTime == 0) time = mStopWatch.Elapsed.TotalSeconds;
                TimerLabel.Content = $"{time:0.00}";
            });
            if (mLimitTime != 0 && mStopWatch.Elapsed.TotalSeconds >= mLimitTime)
            {
                StopTimer();
                mGameStatus = GAME_END;
                Dispatcher.Invoke(() =>
                {
                    Result.Content = "LOSE";
                });
                Result.Foreground = Brushes.Red;
            }
        }

        private void WindowKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Q) Close();
            else if (e.Key == Key.F11)
            {
                if (WindowState != WindowState.Maximized)
                {
                    MenuOverlay.Width = 400;
                    WindowState = WindowState.Maximized;
                    OnRedrawMap();
                }
                else
                {
                    MenuOverlay.Width = 300;
                    WindowState = WindowState.Normal;
                    OnRedrawMap();
                }
            }
            else if (e.Key == Key.R)
            {
                ResetTimer();
                mGame.Restart();
                OnRedrawMap();
                mGameStatus = 0;
            }
            else if (e.Key == Key.N)
            {
                if (mImagePaths.Count == 0)
                {
                    MessageBox.Show("Image files not found!", "Error", MessageBoxButton.OK);
                    return;
                }
                ++mCurrentIndex;
                if (mCurrentIndex >= mImagePaths.Count) mCurrentIndex = 0;
                BitmapSource src = GetBitmapSource(mImagePaths[mCurrentIndex]);
                while (src == null)
                {
                    mImagePaths.RemoveAt(mCurrentIndex);
                    if (mImagePaths.Count == 0)
                    {
                        MessageBox.Show("Image files not found!", "Error", MessageBoxButton.OK);
                        return;
                    }
                    // mCurrentIndex could be at the end of list but the list is not empty yet
                    if (mCurrentIndex >= mImagePaths.Count) mCurrentIndex = 0;
                    src = GetBitmapSource(mImagePaths[mCurrentIndex]);
                }
                ReCreate(src);

            }
            else if (e.Key == Key.B)
            {
                if (mImagePaths.Count == 0)
                {
                    MessageBox.Show("Image files not found!", "Error", MessageBoxButton.OK);
                    return;
                }
                --mCurrentIndex;
                if (mCurrentIndex < 0) mCurrentIndex = mImagePaths.Count - 1;
                BitmapSource src = GetBitmapSource(mImagePaths[mCurrentIndex]);
                while (src == null)
                {
                    // remove corrupted path and index to next path on the left
                    mImagePaths.RemoveAt(mCurrentIndex--);
                    if (mImagePaths.Count == 0)
                    {
                        MessageBox.Show("Image files not found!", "Error", MessageBoxButton.OK);
                        return;
                    }

                    // mCurrentIndex could be at the start of list but the list is not empty yet
                    if (mCurrentIndex < 0) mCurrentIndex = mImagePaths.Count - 1;
                    src = GetBitmapSource(mImagePaths[mCurrentIndex]);
                }
                ReCreate(src);

            }
            else if (e.Key == Key.M)
            {
                if (mImagePaths.Count == 0)
                {
                    MessageBox.Show("Image files not found!", "Error", MessageBoxButton.OK);
                    return;
                }
                int i = mGenerator.Next(0, mImagePaths.Count);
                BitmapSource src = GetBitmapSource(mImagePaths[i]);
                while (src == null)
                {
                    mImagePaths.RemoveAt(i);
                    if (mImagePaths.Count == 0)
                    {
                        MessageBox.Show("Image files not found!", "Error", MessageBoxButton.OK);
                        return;
                    }
                    i = mGenerator.Next(0, mImagePaths.Count);
                    src = GetBitmapSource(mImagePaths[mCurrentIndex]);
                }
                mCurrentIndex = i;
                ReCreate(src);
            }
            else if (e.Key == Key.C)
            {
                mGame.MapSize += 1;
                if (mGame.MapSize > 5) mGame.MapSize = 2;
                mGame.CreateMap();
                OnRedrawMap();
            }
            else if (e.Key == Key.H)
            {
                OnShowGuide(null, null);
            }
            else if (e.Key == Key.F1)
            {
                string location = System.IO.Path.Combine(Environment.CurrentDirectory, "save.pzz");
                if (!mGame.Load(location, out string current))
                {
                    if (!File.Exists(location)) MessageBox.Show("Saved file not found!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    MessageBox.Show("Saved file is corrupted!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    int index = mImagePaths.IndexOf(current);
                    if (index < 0)
                    {
                        MessageBox.Show("Missing image file", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        mCurrentIndex = index;
                        BitmapSource src = GetBitmapSource(mImagePaths[mCurrentIndex]);
                        if (src == null) MessageBox.Show("Image file is corrupted!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                        else
                        {
                            mSelectedImage = src;
                            OnRedrawMap();
                        }
                    }
                }
            }
            //else if (e.Key == Key.W || e.Key == Key.Up)
            //{
            //    if (mGame.BlankPiece - mGame.MapSize < 0) return;
            //    mGame.Update(mGame.BlankPiece, mGame.BlankPiece - mGame.MapSize);
            //}
            //else if (e.Key == Key.S || e.Key == Key.Down)
            //{
            //    if (mGame.BlankPiece - 1 < 0) return;
            //    mGame.Update(mGame.BlankPiece, mGame.BlankPiece + mGame.MapSize);
            //}
            //else if (e.Key == Key.A || e.Key == Key.Left)
            //{
            //    if (mGame.BlankPiece + mGame.MapSize >= mGame.TotalPieces) return;
            //    mGame.Update(mGame.BlankPiece, mGame.BlankPiece - 1);

            //else if (e.Key == Key.D || e.Key == Key.Right)
            //    {
            //        if (mGame.BlankPiece - mGame.MapSize < 0) return;
            //        mGame.Update(mGame.BlankPiece, mGame.BlankPiece + 1);
            //    }
            //}
            else if (e.Key == Key.F2)
            {
                if (mCurrentIndex < 0) return;
                string location = System.IO.Path.Combine(Environment.CurrentDirectory, "save.pzz");
                mGame.Save(location, mImagePaths[mCurrentIndex]);
                MessageBox.Show("File saved successfully", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (e.Key == Key.G)
            {
                Image image = new Image { Source = mSelectedImage, Stretch = Stretch.Uniform };
                Canvas.SetLeft(image, 0);
                Canvas.SetTop(image, 0);
                PuzzleCanvas.Children.Add(image);
            }
        }

        private BitmapSource GetBitmapSource(string path)
        {
            if (path is null) throw new NullReferenceException(nameof(path));
            if (File.Exists(path))
            {
                try
                {
                    Uri uri = new Uri(path, UriKind.Absolute);
                    return new BitmapImage(uri);
                }
                catch (Exception e)
                {
                    Trace.WriteLine($"{e.GetType()}: {e.Message}");
                    return null;
                }
            }
            else return null;
        }
        private DependencyObject GetParent(DependencyObject reference, int level)
        {
            if (reference is null) return null;
            DependencyObject parent = reference;
            for (int i = 0; i < level; ++i)
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            return parent;
        }
        private void OnDragging(object sender, MouseEventArgs e)
        {
            if (mDragged == null || e.RightButton != MouseButtonState.Pressed) return;
            Point newMousePos = e.GetPosition(this);
            Point offset = new Point(mDragged.PickUpMousePoint.X - newMousePos.X, mDragged.PickUpMousePoint.Y - newMousePos.Y);

            double x = mDragged.PickUpPoint.X - offset.X;
            double y = mDragged.PickUpPoint.Y - offset.Y;

            Point topLeftBound = new Point(-mPiece.Width / 2.15, -mPiece.Height / 2.15);
            Point bottomRightBound = new Point(mPiece.Width * mGame.MapSize - mPiece.Width / 1.95, mPiece.Height * mGame.MapSize - mPiece.Height / 1.95);

            x = (x > bottomRightBound.X) ? bottomRightBound.X : x;
            y = (y > bottomRightBound.Y) ? bottomRightBound.Y : y;

            x = (x < topLeftBound.X) ? topLeftBound.X : x;
            y = (y < topLeftBound.Y) ? topLeftBound.Y : y;

            mDragged.DraggingPoint = new Point(x, y);


            Canvas.SetLeft(mDragged.Dragged, mDragged.DraggingPoint.X);
            Canvas.SetTop(mDragged.Dragged, mDragged.DraggingPoint.Y);
        }
        private void StartDragging(object sender, MouseButtonEventArgs e)
        {
            HitTestResult hitResult = VisualTreeHelper.HitTest(this, e.GetPosition(this));
            if (!(GetParent(hitResult.VisualHit, 1) is Border element)) return;
            mDragged = new DraggedElement()
            {
                Dragged = element,
                DraggingPoint = new Point(Canvas.GetLeft(element), Canvas.GetTop(element)),
                PickUpMousePoint = e.GetPosition(this),
                PickUpPoint = new Point(Canvas.GetLeft(element), Canvas.GetTop(element))
            };
            Panel.SetZIndex(element, int.MaxValue);
            CaptureMouse();
        }

        private void StopDragging(object sender, MouseButtonEventArgs e)
        {
            if (mDragged == null) return;

            // Update Logic
            Point location = new Point()
            {
                X = Math.Round(mDragged.DraggingPoint.X / mPiece.Width) * mPiece.Width,
                Y = Math.Round(mDragged.DraggingPoint.Y / mPiece.Height) * mPiece.Height
            };
            Point droppedIndex = new Point(location.X / mPiece.Width, location.Y / mPiece.Height);
            Point pickupIndex = new Point(mDragged.PickUpPoint.X / mPiece.Width, mDragged.PickUpPoint.Y / mPiece.Height);
            Point mouse = e.GetPosition(this);


            int start = Convert.ToInt32(droppedIndex.X + droppedIndex.Y * mGame.MapSize);
            int dest = Convert.ToInt32(pickupIndex.X + pickupIndex.Y * mGame.MapSize);

            if (mGameStatus == GAME_PAUSED || mGameStatus == GAME_END) start = dest;
            int result = mGame.Update(start, dest);
            if (result != 0)
            {
                // Snap the piece to where it is when picked up
                Canvas.SetLeft(mDragged.Dragged, mDragged.PickUpPoint.X);
                Canvas.SetTop(mDragged.Dragged, mDragged.PickUpPoint.Y);
                Panel.SetZIndex(mDragged.Dragged, 0);
                Trace.WriteLine($"Invalid move: snap back to {pickupIndex}");
            }
            else
            {
                if (mGame.IsSolved())
                {
                    int i = mGame.Map.Length - 1;
                    double left = (i % mGame.MapSize) * mPiece.Width;
                    double top = (i / mGame.MapSize) * mPiece.Height;
                    Border lastPiece = mImageElements[i];
                    Canvas.SetLeft(lastPiece, left);
                    Canvas.SetTop(lastPiece, top);
                    PuzzleCanvas.Children.Add(lastPiece);

                    StopTimer();
                    mGameStatus = GAME_END;
                    Result.Content = "WIN";
                    Result.Foreground = Brushes.Green;
                }

                Panel.SetZIndex(mDragged.Dragged, int.MinValue);

                // Check if there is UIElement at dropped location
                HitTestResult hitElement = VisualTreeHelper.HitTest(GameOverlay, new Point(mouse.X, mouse.Y));
                if (hitElement != null)
                {
                    // Swapping position
                    DependencyObject parent = GetParent(hitElement.VisualHit, 1);
                    if (parent is Border piece)
                    {
                        Canvas.SetLeft(piece, mDragged.PickUpPoint.X);
                        Canvas.SetTop(piece, mDragged.PickUpPoint.Y);
                    }
                }

                Canvas.SetLeft(mDragged.Dragged, location.X);
                Canvas.SetTop(mDragged.Dragged, location.Y);
                Panel.SetZIndex(mDragged.Dragged, 0);
                Trace.WriteLine($"Snapping to grid {droppedIndex}");
            }

            ReleaseMouseCapture();
            mDragged = null;
        }
        private void ReCreate(BitmapSource src)
        {
            if (src is null) throw new NullReferenceException(nameof(src));
            ResetTimer();
            mGame.CreateMap();
            mSelectedImage = src;
            OnRedrawMap();
        }
        private void AssignSource(BitmapSource source)
        {
            if (source is null) throw new NullReferenceException(nameof(source));
            // make sure we have enough images for the map
            int size = mGame.MapSize * mGame.MapSize;
            mPiece = new Size(source.PixelWidth / mGame.MapSize, source.PixelHeight / mGame.MapSize);
            if (mImageElements is null || mImageElements.Count != size)
            {
                Trace.WriteLineIf(mImageElements is null, $"{nameof(mImageElements)} is not initialized. Begin initializing...");
                if (mImageElements != null) Trace.WriteLineIf(mImageElements.Count != size, $"Detecting change in {nameof(mImageElements)}.Count: old={mImageElements.Count} new={size}");
                mImageElements = new List<Border>(size);
                for (int i = 0; i < size; ++i) mImageElements.Add(new Border() { Style = FindResource("BorderTemplate") as Style, Child = null });
            }

            // change border's content to be new image's source
            int width = Convert.ToInt32(Math.Floor(mPiece.Width));
            int height = Convert.ToInt32(Math.Floor(mPiece.Height));
            for (int i = 0; i < mImageElements.Count; ++i)
            {
                int left = i % mGame.MapSize * width;
                int top = i / mGame.MapSize * height;
                Int32Rect rect = new Int32Rect(left, top, width, height);
                CroppedBitmap bitmap = new CroppedBitmap(source, rect);
                Image img = new Image() { Style = FindResource("ImageTemplate") as Style, Width = mPiece.Width, Height = mPiece.Height, Source = bitmap };
                mImageElements[i].Child = img;
            }
        }

        private class DraggedElement
        {
            public UIElement Dragged { get; set; }
            public Point PickUpPoint { get; set; }
            public Point DraggingPoint { get; set; }
            public Point PickUpMousePoint { get; set; }
        }

        private void OnRedrawMap()
        {
            if (mSelectedImage is null) return;
            Size window = new Size(GameOverlay.ActualWidth, GameOverlay.ActualHeight);
            Size image = new Size(mSelectedImage.PixelWidth, mSelectedImage.PixelHeight);

            Point factor = new Point(window.Width / image.Width, window.Height / image.Height);
            double f = (factor.X < factor.Y) ? factor.X : factor.Y;
            mSelectedImage = new TransformedBitmap(mSelectedImage, new ScaleTransform(f, f));

            // Update PuzzleImage
            PuzzleImage.Source = mSelectedImage;
            // Update canvas's size
            PuzzleCanvas.Width = mSelectedImage.PixelWidth;
            PuzzleCanvas.Height = mSelectedImage.PixelHeight;

            AssignSource(mSelectedImage);
            int size = mGame.MapSize * mGame.MapSize;
            PuzzleCanvas.Children.Clear();
            for (int i = 0; i < size; ++i)
            {
                if (mGame.Map[i] != size - 1)
                {
                    int left = i % mGame.MapSize * Convert.ToInt32(mPiece.Width);
                    int top = i / mGame.MapSize * Convert.ToInt32(mPiece.Height);
                    UIElement element = mImageElements[mGame.Map[i]];
                    Canvas.SetLeft(element, left);
                    Canvas.SetTop(element, top);
                    PuzzleCanvas.Children.Add(element);
                }
            }
        }

        private void OnChangeDifficulty(object sender, RoutedEventArgs e)
        {
            if (mGameStatus == 0)
            {
                Button button = sender as Button;
                string diffStr = button.Content as string;
                string[] tokens = diffStr.Split(new char[] { 'x' }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (int.TryParse(tokens[0], out int diff)) mGame.MapSize = diff;
                else mGame.MapSize = 3;
                Trace.WriteLine($"Change difficulty to {diffStr}");
                mGame.CreateMap();
                OnRedrawMap();
            }
        }
        private void ResetTimer()
        {
            mTimer.Stop();
            mStopWatch.Reset();
            TimerLabel.Content = $"{mLimitTime:0.00}";
        }
        private void StartTimer()
        {
            mTimer.Start();
            mStopWatch.Start();
        }
        private void StopTimer()
        {
            mTimer.Stop();
            mStopWatch.Stop();
        }
        private void OnChangeTimer(object sender, RoutedEventArgs e)
        {
            if (mGameStatus == 0)
            {
                Button button = sender as Button;
                string timerStr = button.Content as string;
                if (timerStr == "∞") mLimitTime = 0;
                else mLimitTime = int.Parse(timerStr);

                TimerLabel.Content = mLimitTime;

                Trace.WriteLine($"Change timer to {mLimitTime}");
            }
        }

        private void OnDisplayDifficultyOption(object sender, RoutedEventArgs e)
        {
            MapSizeOption.Opacity = Math.Abs(MapSizeOption.Opacity - 1);
            TimerOption.Opacity = Math.Abs(MapSizeOption.Opacity - 1);
        }

        private void OnShowGuide(object sender, RoutedEventArgs e)
        {
            if (HelpOverlay.Visibility == Visibility.Visible)
            {
                HelpOverlay.Visibility = Visibility.Collapsed;
                Panel.SetZIndex(HelpOverlay, int.MinValue);
            }
            else if (HelpOverlay.Visibility == Visibility.Collapsed)
            {
                HelpOverlay.Visibility = Visibility.Visible;
                Panel.SetZIndex(HelpOverlay, int.MaxValue);
            }
        }

        private void OnAddImage(object sender, RoutedEventArgs e)
        {
            VistaOpenFileDialog dialog = new VistaOpenFileDialog()
            {
                Multiselect = true,
                CheckFileExists = true,
                RestoreDirectory = true,
                Filter = IMAGE_FILTER
            };

            bool? result = dialog.ShowDialog();
            if (result.Value)
            {
                mCurrentIndex = mImagePaths.Count;
                foreach (string path in dialog.FileNames)
                {
                    mImagePaths.Add(path);
                }
                mSelectedImage = new BitmapImage(new Uri(mImagePaths[mCurrentIndex], UriKind.Absolute));

                mGame.CreateMap();
                OnRedrawMap();
            }
        }

        private void TimerClicked(object sender, RoutedEventArgs e)
        {
            if (mGameStatus == 0)
            {
                StartTimer();
                mGameStatus = GAME_RUNNING;
            }
            else if (mGameStatus == GAME_RUNNING)
            {
                StopTimer();
                mGameStatus = GAME_PAUSED;
            }
            else if (mGameStatus == GAME_PAUSED)
            {
                StartTimer();
                mGameStatus = GAME_RUNNING;
            }
        }

        private double mLimitTime = 10;
        private List<string> mImagePaths = new List<string>();
        private int mCurrentIndex = 0;
        private List<Border> mImageElements;
        private BitmapSource mSelectedImage;
        private Size mPiece;
        private DraggedElement mDragged;
        private readonly Game mGame = new Game();
        private readonly string mImageDirectory = System.IO.Path.Combine(Environment.CurrentDirectory, "Images");
        private readonly ConsoleTraceListener mListener = new ConsoleTraceListener();
        private readonly Random mGenerator = new Random();
        private readonly Stopwatch mStopWatch = new Stopwatch();
        private readonly Timer mTimer = new Timer() { Interval = 1.0 / 60.0 * 1000.0 };
        private const string IMAGE_FILTER = "images|*.png;*.bmp;*.gif;*.jpg; *.jpeg; *.jpe";
        private readonly string[] IMAGE_PATTERNS = new string[] { "*.jpg", "*.png", "*.jpeg", "*.bmp", "*.ico", "*.gif", "*.jpe" };

        private int mGameStatus = 0;
        private const int GAME_RUNNING = 1;
        private const int GAME_PAUSED = 2;
        private const int GAME_END = 3;
        private const int IMAGE_NOT_FOUND = -1;
        private const int DIRECTORY_NOT_FOUND = -2;

    }
}
