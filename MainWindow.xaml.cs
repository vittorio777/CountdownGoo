using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Input;
using System.Media;
using System.Windows.Media;
using System.Windows.Forms;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Point = System.Windows.Point;
using MessageBox = System.Windows.MessageBox;

namespace CountdownGo
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer timer;
        private bool isRunning;
        private bool showPreviewEnabled = true;
        private readonly MediaPlayer alarmSound;
        private const int MaxAlarmPlayCount = 3;
        private int alarmPlayCount = 0;

        public bool IsTimerRunning()
        {
            return isRunning;
        }

        public void StartTimer()
        {
            if (remainingTime.TotalSeconds <= 0) return;
            isRunning = true;
            timer.Start();
            StartButton.IsEnabled = false;
            PauseButton.IsEnabled = true;
            if (previewWindow != null)
            {
                previewWindow.UpdatePlayPauseMenuItemStatus();
            }
        }

        public void PauseTimer()
        {
            isRunning = false;
            timer.Stop();
            StartButton.IsEnabled = true;
            PauseButton.IsEnabled = false;
            if (previewWindow != null)
            {
                previewWindow.UpdatePlayPauseMenuItemStatus();
            }
        }

        private void DragWindow(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void MinimizeWindow(object sender, RoutedEventArgs e)
        {
            if (notifyIcon == null)
            {
                MessageBox.Show("系统托盘图标未正确初始化", "CountdownGo");
                return;
            }

            try
            {
                if (showPreviewEnabled)
                {
                    if (previewWindow == null)
                    {
                        previewWindow = new PreviewWindow(this);
                        previewWindow.Show();
                        UpdateAllDisplays();
                    }
                }
                else if (previewWindow != null)
                {
                    previewWindow.Close();
                    previewWindow = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"预览窗口操作失败：{ex.Message}", "CountdownGo");
                return;
            }

            notifyIcon.Visible = true;
            Hide();
            WindowState = WindowState.Minimized;

        }

        private void MaximizeWindow(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        public void RestoreWindow()
        {
            // 先设置窗口状态
            WindowState = WindowState.Normal;
            
            // 显示并激活窗口
            Show();
            Activate();
            Focus();
            
            // 临时置顶以确保窗口显示在最前
            Topmost = true;
            Topmost = false;
            
            // 关闭预览窗口
            if (previewWindow != null)
            {
                previewWindow.Close();
                previewWindow = null;
            }
        }

        private void InitializeDefaultSound()
        {
            try
            {
                var soundsDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds");
                if (!System.IO.Directory.Exists(soundsDir))
                {
                    System.IO.Directory.CreateDirectory(soundsDir);
                    return;
                }

                var lastSelectedSound = Properties.Settings.Default.LastSelectedSound;
                if (string.IsNullOrEmpty(lastSelectedSound))
                {
                    // 如果没有上次选择的铃声，使用第一个找到的MP3文件
                    var soundFiles = System.IO.Directory.GetFiles(soundsDir, "*.mp3");
                    if (soundFiles.Length > 0)
                    {
                        lastSelectedSound = System.IO.Path.GetFileName(soundFiles[0]);
                        Properties.Settings.Default.LastSelectedSound = lastSelectedSound;
                        Properties.Settings.Default.Save();
                    }
                }

                if (!string.IsNullOrEmpty(lastSelectedSound))
                {
                    var fullPath = System.IO.Path.Combine(soundsDir, lastSelectedSound);
                    if (System.IO.File.Exists(fullPath))
                    {
                        var uri = new Uri(fullPath, UriKind.Absolute);
                        alarmSound.Open(uri);
                        alarmSound.Volume = 1.0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化默认铃声失败：{ex.Message}", "CountdownGo");
            }
        }

        private void LoadSoundFiles(ToolStripMenuItem soundsMenu)
        {
            try
            {
                var soundsDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds");
                if (!System.IO.Directory.Exists(soundsDir))
                {
                    System.IO.Directory.CreateDirectory(soundsDir);
                }

                var soundFiles = System.IO.Directory.GetFiles(soundsDir, "*.mp3");
                foreach (var soundFile in soundFiles)
                {
                    var fileName = System.IO.Path.GetFileNameWithoutExtension(soundFile);
                    var menuItem = new ToolStripMenuItem(fileName, null, (s, e) => {
                        var items = soundsMenu.DropDownItems.Cast<ToolStripMenuItem>();
                        foreach (var item in items)
                        {
                            item.Checked = false;
                        }
                        ((ToolStripMenuItem)s).Checked = true;
                        ChangeSoundFile(soundFile);
                        Properties.Settings.Default.LastSelectedSound = System.IO.Path.GetFileName(soundFile);
                        Properties.Settings.Default.Save();
                    });
                    if (System.IO.Path.GetFileName(soundFile) == Properties.Settings.Default.LastSelectedSound)
                    {
                        menuItem.Checked = true;
                    }
                    soundsMenu.DropDownItems.Add(menuItem);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载音频文件失败：{ex.Message}", "CountdownGo");
            }
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            // 停止计时器
            if (isRunning)
            {
                timer.Stop();
                isRunning = false;
            }

            // 停止音频播放
            if (alarmSound != null)
            {
                alarmPlayCount = 0;
                alarmSound.Stop();
            }

            // 关闭预览窗口
            if (previewWindow != null)
            {
                previewWindow.Close();
                previewWindow = null;
            }

            // 清理系统托盘图标
            if (notifyIcon != null)
            {
                notifyIcon.Visible = false;
                notifyIcon.Dispose();
            }

            // 关闭主窗口
            Close();
        }
        private TimeSpan remainingTime;
        private readonly NotifyIcon notifyIcon;
        private PreviewWindow? previewWindow;
        private bool isDragging;
        private Point lastMousePosition;
        private TextBlock? activeTimeUnit;

        public MainWindow()
        {
            InitializeComponent();
            Title = "CountdownGo";
            
            // 绑定鼠标移动事件
            MouseMove += Window_MouseMove;

            // 初始化按钮状态
            StartButton.IsEnabled = true;
            PauseButton.IsEnabled = false;

            // 绑定按钮事件
            StartButton.Click += (s, e) => StartTimer();
            PauseButton.Click += (s, e) => PauseTimer();
            ResetButton.Click += (s, e) => ResetTimer();
            
            // 绑定快速设置时间按钮事件
            Set10MinButton.Click += (s, e) => SetTime(10);
            Set20MinButton.Click += (s, e) => SetTime(20);
            Set30MinButton.Click += (s, e) => SetTime(30);
            Set45MinButton.Click += (s, e) => SetTime(45);

            var contextMenu = new ContextMenuStrip();
            var showPreviewItem = new ToolStripMenuItem("显示预览窗口") { Checked = true, CheckOnClick = true };
            var soundsMenu = new ToolStripMenuItem("选择铃声");
            LoadSoundFiles(soundsMenu);
            showPreviewItem.Click += (s, e) => {
                showPreviewEnabled = showPreviewItem.Checked;
                if (showPreviewEnabled && previewWindow == null)
                {
                    previewWindow = new PreviewWindow(this);
                    previewWindow.Show();
                    UpdateAllDisplays();
                }
                else if (!showPreviewEnabled && previewWindow != null)
                {
                    previewWindow.Close();
                    previewWindow = null;
                }
            };
            var exitItem = new ToolStripMenuItem("退出");
            exitItem.Click += (s, e) => {
                if (notifyIcon != null)
                {
                    notifyIcon.Visible = false;
                    notifyIcon.Dispose();
                }
                System.Windows.Application.Current.Shutdown();
            };
            
            contextMenu.Items.Add(showPreviewItem);
            contextMenu.Items.Add(soundsMenu);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(exitItem);

            notifyIcon = new NotifyIcon
            {
                Text = "CountdownGo",
                Visible = true,
                ContextMenuStrip = contextMenu,
                Icon = System.Drawing.SystemIcons.Application
            };

            var iconPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "..\\..\\..\\icon.ico");
            try
            {
                if (System.IO.File.Exists(iconPath))
                {
                    var icon = new System.Drawing.Icon(iconPath);
                    notifyIcon.Icon = icon;
                    Icon = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                        icon.Handle,
                        Int32Rect.Empty,
                        System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                }
                else
                {
                    MessageBox.Show($"找不到图标文件：{iconPath}\n系统托盘图标将使用默认图标", "CountdownGo");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法加载图标文件：{ex.Message}\n系统托盘图标将使用默认图标", "CountdownGo");
            }
            notifyIcon.MouseClick += (s, e) => {
                if (e.Button == System.Windows.Forms.MouseButtons.Left)
                {
                    RestoreWindow();
                }
            };
            notifyIcon.MouseDoubleClick += (s, e) => {
                if (e.Button == System.Windows.Forms.MouseButtons.Left)
                {
                    RestoreWindow();
                }
            };
            
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += Timer_Tick;

            // 初始化音频播放器
            alarmSound = new MediaPlayer();
            alarmSound.MediaFailed += (s, e) => MessageBox.Show($"音频加载失败：{e.ErrorException.Message}", "CountdownGo");
            alarmSound.MediaEnded += (s, e) => {
                alarmPlayCount++;
                if (alarmPlayCount < MaxAlarmPlayCount)
                {
                    alarmSound.Position = TimeSpan.Zero;
                    alarmSound.Play();
                }
                else
                {
                    alarmPlayCount = 0;
                    alarmSound.Stop();
                }
            };
            
            // 初始化默认铃声
            InitializeDefaultSound();
        }

        private void ChangeSoundFile(string soundFilePath)
        {
            try
            {
                var soundsDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds");
                var fullPath = System.IO.Path.GetFileName(soundFilePath);
                fullPath = System.IO.Path.Combine(soundsDir, fullPath);

                if (System.IO.File.Exists(fullPath))
                {
                    // 停止当前音频播放并重置状态
                    StopAlarm();
                    alarmSound.Close();

                    try
                    {
                        // 使用完整的URI格式加载新音频
                        var uri = new Uri(fullPath, UriKind.Absolute);
                        alarmSound.Open(uri);
                        alarmSound.Volume = 1.0;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"音频文件格式错误或无法访问：{ex.Message}", "CountdownGo");
                        return;
                    }
                }
                else
                {
                    MessageBox.Show($"找不到音频文件：{fullPath}", "CountdownGo");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"音频文件操作失败：{ex.Message}", "CountdownGo");
            }
        }
        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                StopAlarm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"停止闹铃失败：{ex.Message}", "CountdownGo");
            }
        }

        private void TimeUnit_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isDragging = true;
            activeTimeUnit = sender as TextBlock;
            lastMousePosition = e.GetPosition(activeTimeUnit);
            activeTimeUnit.CaptureMouse();
        }

        private void TimeUnit_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && activeTimeUnit != null)
            {
                Point currentPosition = e.GetPosition(activeTimeUnit);
                double deltaY = currentPosition.Y - lastMousePosition.Y;

                if (Math.Abs(deltaY) > 10)
                {
                    int change = deltaY > 0 ? -1 : 1;
                    UpdateTimeUnit(activeTimeUnit.Name, change);
                    lastMousePosition = currentPosition;
                }
            }
        }

        private void TimeUnit_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isDragging && activeTimeUnit != null)
            {
                activeTimeUnit.ReleaseMouseCapture();
                isDragging = false;
                activeTimeUnit = null;
            }
        }

        private void TimeUnit_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            TextBlock timeUnit = sender as TextBlock;
            int change = e.Delta > 0 ? 1 : -1;
            UpdateTimeUnit(timeUnit.Name, change);
        }

        private void SetTime(int minutes)
        {
            remainingTime = TimeSpan.FromMinutes(minutes);
            UpdateAllDisplays();
        }

        private void UpdateTimeUnit(string unitName, int change)
        {
            switch (unitName)
            {
                case "HourDisplay":
                    remainingTime = remainingTime.Add(TimeSpan.FromHours(change));
                    break;
                case "MinuteDisplay":
                    remainingTime = remainingTime.Add(TimeSpan.FromMinutes(change));
                    break;
                case "SecondDisplay":
                    remainingTime = remainingTime.Add(TimeSpan.FromSeconds(change));
                    break;
            }

            if (remainingTime < TimeSpan.Zero)
            {
                remainingTime = TimeSpan.Zero;
            }

            UpdateAllDisplays();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (remainingTime.TotalSeconds > 0)
            {
                remainingTime = remainingTime.Subtract(TimeSpan.FromSeconds(1));
                
                if (remainingTime.TotalSeconds <= 0)
                {
                    remainingTime = TimeSpan.Zero;
                    timer.Stop();
                    isRunning = false;
                    StartButton.IsEnabled = true;
                    PauseButton.IsEnabled = false;
                    UpdateAllDisplays();
                    if (alarmSound != null && alarmSound.Source != null)
                    {
                        StopAlarm();
                        alarmSound.Play();
                    }
                    return;
                }
                
                UpdateAllDisplays();
            }
        }

        private void UpdateAllDisplays()
        {
            HourDisplay.Text = remainingTime.Hours.ToString("00");
            MinuteDisplay.Text = remainingTime.Minutes.ToString("00");
            SecondDisplay.Text = remainingTime.Seconds.ToString("00");

            if (previewWindow != null)
            {
                previewWindow.UpdateDisplay(remainingTime);
            }
        }

        public void StopAlarm()
        {
            if (alarmSound != null)
            {
                alarmPlayCount = 0;
                alarmSound.Stop();
                if (alarmSound.Source != null)
                {
                    alarmSound.Position = TimeSpan.Zero;
                }
            }
        }

        private void ResetTimer()
        {
            if (isRunning)
            {
                timer.Stop();
                isRunning = false;
            }
            remainingTime = TimeSpan.Zero;
            StartButton.IsEnabled = true;
            PauseButton.IsEnabled = false;
            UpdateAllDisplays();
            if (previewWindow != null)
            {
                previewWindow.UpdatePlayPauseMenuItemStatus();
            }
        }
    }
}