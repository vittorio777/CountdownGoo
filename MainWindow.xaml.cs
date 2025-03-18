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
            
            // 隐藏托盘图标
            notifyIcon.Visible = false;
        }

        private void LoadSoundFiles(ToolStripMenuItem soundsMenu)
        {
            try
            {
                var soundsDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\Sounds");
                if (!System.IO.Directory.Exists(soundsDir))
                {
                    System.IO.Directory.CreateDirectory(soundsDir);
                }

                var defaultSoundPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ahh.mp3");
                var defaultSoundItem = new ToolStripMenuItem("默认铃声", null, (s, e) => {
                    var items = soundsMenu.DropDownItems.Cast<ToolStripMenuItem>();
                    foreach (var item in items)
                    {
                        item.Checked = false;
                    }
                    ((ToolStripMenuItem)s).Checked = true;
                    ChangeSoundFile(defaultSoundPath);
                    Properties.Settings.Default.LastSelectedSound = "ahh.mp3";
                    Properties.Settings.Default.Save();
                });
                soundsMenu.DropDownItems.Add(defaultSoundItem);
                
                // 如果LastSelectedSound是默认铃声或未设置，则默认选中默认铃声
                if (string.IsNullOrEmpty(Properties.Settings.Default.LastSelectedSound) || 
                    Properties.Settings.Default.LastSelectedSound == "ahh.mp3")
                {
                    defaultSoundItem.Checked = true;
                    ChangeSoundFile(defaultSoundPath);
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
                        defaultSoundItem.Checked = false;
                        ChangeSoundFile(soundFile);
                    }
                    soundsMenu.DropDownItems.Add(menuItem);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载音频文件失败：{ex.Message}", "CountdownGo");
            }
        }

        private void ChangeSoundFile(string soundPath)
        {
            try
            {
                if (alarmSound == null)
                {
                    MessageBox.Show("音频播放器未正确初始化", "CountdownGo");
                    return;
                }

                if (!System.IO.File.Exists(soundPath))
                {
                    MessageBox.Show($"找不到音频文件：{soundPath}", "CountdownGo");
                    return;
                }

                alarmSound.Stop();
                alarmSound.Open(new Uri(soundPath));
                alarmSound.Volume = 1.0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"切换音频文件失败：{ex.Message}", "CountdownGo");
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
                Visible = false,
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

            StartButton.Click += StartButton_Click;
            PauseButton.Click += PauseButton_Click;
            ResetButton.Click += ResetButton_Click;
            Set10MinButton.Click += (s, e) => SetQuickTime(10);
            Set20MinButton.Click += (s, e) => SetQuickTime(20);
            Set30MinButton.Click += (s, e) => SetQuickTime(30);
            Set45MinButton.Click += (s, e) => SetQuickTime(45);
            this.MouseMove += Window_MouseMove;
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
            try
            {
                var lastSelectedSound = Properties.Settings.Default.LastSelectedSound;
                var audioPath = lastSelectedSound == "ahh.mp3" ?
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ahh.mp3") :
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\Sounds", lastSelectedSound);
                if (System.IO.File.Exists(audioPath))
                {
                    alarmSound.Open(new Uri(audioPath));
                    alarmSound.Volume = 1.0;
                }
                else
                {
                    MessageBox.Show("找不到音频文件：" + audioPath, "CountdownGo");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法加载音频文件：{ex.Message}", "CountdownGo");
            }

            ResetTimer();
            UpdateAllDisplays();
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            StartTimer();
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            PauseTimer();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            ResetTimer();
        }

        private void ResetTimer()
        {
            isRunning = false;
            timer.Stop();
            remainingTime = TimeSpan.Zero;
            StartButton.IsEnabled = true;
            PauseButton.IsEnabled = false;
            UpdateAllDisplays();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            try
            {
                if (remainingTime.TotalSeconds <= 0)
                {
                    timer.Stop();
                    isRunning = false;
                    try
                    {
                        if (alarmSound?.Source != null)
                        {
                            alarmPlayCount = 0;
                            alarmSound.Stop();
                            alarmSound.Position = TimeSpan.Zero;
                            alarmSound.Play();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"音频播放失败：{ex.Message}", "CountdownGo");
                    }
                    ResetTimer();
                    return;
                }

                remainingTime = remainingTime.Subtract(TimeSpan.FromSeconds(1));
                UpdateAllDisplays();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"计时器更新失败：{ex.Message}", "CountdownGo");
                ResetTimer();
            }
        }

        private void UpdateAllDisplays()
        {
            try
            {
                HourDisplay.Text = remainingTime.Hours.ToString("00");
                MinuteDisplay.Text = remainingTime.Minutes.ToString("00");
                SecondDisplay.Text = remainingTime.Seconds.ToString("00");

                if (previewWindow != null && !previewWindow.IsClosing)
                {
                    previewWindow.UpdateTime($"{remainingTime.Hours:00}:{remainingTime.Minutes:00}:{remainingTime.Seconds:00}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"显示更新失败：{ex.Message}", "CountdownGo");
            }
        }

        private void TimeUnit_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (alarmPlayCount > 0)
            {
                StopAlarm();
                return;
            }

            if (isRunning) return;
            isDragging = true;
            activeTimeUnit = sender as TextBlock;
            lastMousePosition = e.GetPosition(activeTimeUnit);
            activeTimeUnit?.CaptureMouse();
        }

        private void TimeUnit_MouseMove(object sender, MouseEventArgs e)
        {
            if (alarmPlayCount > 0)
            {
                StopAlarm();
                return;
            }

            if (!isDragging || activeTimeUnit == null) return;

            var currentPosition = e.GetPosition(activeTimeUnit);
            var deltaY = currentPosition.Y - lastMousePosition.Y;

            if (Math.Abs(deltaY) < 4) return;

            var increment = deltaY > 0 ? -1 : 1;
            var hours = remainingTime.Hours;
            var minutes = remainingTime.Minutes;
            var seconds = remainingTime.Seconds;

            if (activeTimeUnit == HourDisplay)
            {
                hours = Math.Max(0, Math.Min(23, hours + increment));
                activeTimeUnit.Opacity = 0.7;
            }
            else if (activeTimeUnit == MinuteDisplay)
            {
                minutes = Math.Max(0, Math.Min(59, minutes + increment));
                activeTimeUnit.Opacity = 0.7;
            }
            else if (activeTimeUnit == SecondDisplay)
            {
                seconds = Math.Max(0, Math.Min(59, seconds + increment));
                activeTimeUnit.Opacity = 0.7;
            }

            remainingTime = new TimeSpan(hours, minutes, seconds);
            UpdateAllDisplays();
            lastMousePosition = currentPosition;
        }

        private void TimeUnit_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!isDragging) return;
            isDragging = false;
            if (activeTimeUnit != null)
            {
                activeTimeUnit.Opacity = 1.0;
                activeTimeUnit.ReleaseMouseCapture();
                activeTimeUnit = null;
            }
        }

        public void StopAlarm()
        {
            if (alarmSound.Source != null)
            {
                alarmPlayCount = 0;
                alarmSound.Stop();
            }
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            if (alarmPlayCount > 0)
            {
                StopAlarm();
                return;
            }

            if (isRunning) return;

            switch (e.Key)
            {
                case Key.Up:
                    remainingTime = remainingTime.Add(TimeSpan.FromMinutes(1));
                    UpdateAllDisplays();
                    break;
                case Key.Down:
                    if (remainingTime.TotalMinutes > 0)
                    {
                        remainingTime = remainingTime.Subtract(TimeSpan.FromMinutes(1));
                        UpdateAllDisplays();
                    }
                    break;
            }
        }

        private void TimeUnit_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (alarmPlayCount > 0)
            {
                StopAlarm();
                return;
            }

            if (isRunning) return;

            var textBlock = sender as TextBlock;
            if (textBlock == null) return;

            var hours = remainingTime.Hours;
            var minutes = remainingTime.Minutes;
            var seconds = remainingTime.Seconds;
            var increment = e.Delta > 0 ? 1 : -1;

            if (textBlock == HourDisplay)
            {
                hours = Math.Max(0, Math.Min(23, hours + increment));
            }
            else if (textBlock == MinuteDisplay)
            {
                minutes = Math.Max(0, Math.Min(59, minutes + increment));
            }
            else if (textBlock == SecondDisplay)
            {
                seconds = Math.Max(0, Math.Min(59, seconds + increment));
            }

            remainingTime = new TimeSpan(hours, minutes, seconds);
            UpdateAllDisplays();
        }
        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (alarmPlayCount > 0)
            {
                StopAlarm();
                return;
            }
        }

        private void SetQuickTime(int minutes)
        {
            if (isRunning) return;
            remainingTime = TimeSpan.FromMinutes(minutes);
            UpdateAllDisplays();
        }
    }
}