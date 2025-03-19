using System.Windows;
using System.Windows.Input;
using MessageBox = System.Windows.MessageBox;

namespace CountdownGo
{
    public partial class PreviewWindow : Window
    {
        private MainWindow? _mainWindow;
        private bool _isRunning;
        public bool IsClosing { get; private set; }

        public PreviewWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            
            // 从设置中加载窗口位置
            var settings = Properties.Settings.Default;
            if (settings.PreviewWindowLeft != 0 || settings.PreviewWindowTop != 0)
            {
                Left = settings.PreviewWindowLeft;
                Top = settings.PreviewWindowTop;
            }
            else
            {
                // 首次运行时设置窗口位置到屏幕右下角
                var screenWidth = SystemParameters.PrimaryScreenWidth;
                var screenHeight = SystemParameters.PrimaryScreenHeight;
                Left = screenWidth - Width - 15;
                Top = screenHeight - Height - 60;
                
                // 保存初始位置
                settings.PreviewWindowLeft = Left;
                settings.PreviewWindowTop = Top;
                settings.Save();                
            }

            // 添加双击事件处理
            MouseDoubleClick += PreviewWindow_MouseDoubleClick;
            UpdatePlayPauseMenuItemStatus();
        }

        private void PreviewWindow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Close();
            _mainWindow?.RestoreWindow();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            IsClosing = true;
            try
            {
                base.OnClosing(e);
                SaveWindowPosition();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存窗口位置失败：{ex.Message}", "CountdownGo");
            }
        }

        public void UpdateTime(string time)
        {
            TimeDisplay.Text = time;
        }

        public void UpdateDisplay(TimeSpan remainingTime)
        {
            TimeDisplay.Text = $"{remainingTime.Hours:00}:{remainingTime.Minutes:00}:{remainingTime.Seconds:00}";
        }

        private void SaveWindowPosition()
        {
            Properties.Settings.Default.PreviewWindowLeft = Left;
            Properties.Settings.Default.PreviewWindowTop = Top;
            Properties.Settings.Default.Save();
        }

        private void DragWindow(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
                SaveWindowPosition();
            }
        }

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            try
            {
                // 调用主窗口的StopAlarm方法来停止铃声
                _mainWindow?.StopAlarm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"停止闹铃失败：{ex.Message}", "CountdownGo");
            }
        }

        private void PlayPauseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_mainWindow == null) return;
                
                if (_isRunning)
                {
                    _mainWindow.PauseTimer();
                }
                else
                {
                    _mainWindow.StartTimer();
                }
                UpdatePlayPauseMenuItemStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"计时器操作失败：{ex.Message}", "CountdownGo");
            }
        }


        public void UpdatePlayPauseMenuItemStatus()
        {
            if (_mainWindow == null) return;
            _isRunning = _mainWindow.IsTimerRunning();
            PlayPauseMenuItem.Header = _isRunning ? "暂停" : "开始";
        }
    }
}