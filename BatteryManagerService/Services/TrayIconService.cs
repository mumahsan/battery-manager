using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BatteryManagerService.Services
{
    /// <summary>
    /// Interface for managing system tray icon.
    /// </summary>
    public interface ITrayIconService
    {
        /// <summary>
        /// Updates the tray icon with current battery percentage.
        /// </summary>
        void UpdateBatteryLevel(int percentage, bool isCharging);

        /// <summary>
        /// Shows the tray icon.
        /// </summary>
        void Show();

        /// <summary>
        /// Hides and disposes the tray icon.
        /// </summary>
        void Hide();
    }

    /// <summary>
    /// Hidden form that hosts the NotifyIcon to ensure proper message pump association.
    /// </summary>
    internal class TrayIconForm : Form
    {
        public NotifyIcon NotifyIcon { get; }

        public TrayIconForm()
        {
            // Create hidden form - never show it
            ShowInTaskbar = false;
            WindowState = FormWindowState.Minimized;
            FormBorderStyle = FormBorderStyle.None;
            Opacity = 0;
            Width = 0;
            Height = 0;

            // Create the NotifyIcon as a component of this form
            NotifyIcon = new NotifyIcon(new System.ComponentModel.Container())
            {
                Icon = SystemIcons.Application,
                Text = "Battery Manager",
                Visible = true
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                NotifyIcon?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Service that displays battery percentage in the system tray.
    /// </summary>
    public class TrayIconService : ITrayIconService, IDisposable
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);
        
        private readonly ILogger<TrayIconService> _logger;
        private TrayIconForm? _form;
        private NotifyIcon? _notifyIcon;
        private ContextMenuStrip _contextMenu;
        private ToolStripMenuItem _exitMenuItem;
        private readonly IHostApplicationLifetime _lifetime;
        private bool _isInitialized = false;

        public TrayIconService(ILogger<TrayIconService> logger, IHostApplicationLifetime lifetime)
        {
            _logger = logger;
            _lifetime = lifetime;

            // Create context menu with larger font
            _contextMenu = new ContextMenuStrip();
            _contextMenu.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            
            var titleItem = new ToolStripMenuItem("Battery Manager") { Enabled = false };
            _contextMenu.Items.Add(titleItem);
            _contextMenu.Items.Add(new ToolStripSeparator());
            
            _exitMenuItem = new ToolStripMenuItem("Exit");
            _exitMenuItem.Click += OnExitClicked;
            _contextMenu.Items.Add(_exitMenuItem);

            _logger.LogInformation("Tray icon service created (will initialize on UI thread)");
        }

        private void EnsureInitialized()
        {
            if (_isInitialized) return;

            // This must be called from UI thread
            _form = new TrayIconForm();
            _notifyIcon = _form.NotifyIcon;
            _notifyIcon.ContextMenuStrip = _contextMenu;
            _notifyIcon.Text = "Battery Manager - Press Ctrl+Shift+X to exit";
            
            // Add double-click handler
            _notifyIcon.DoubleClick += (s, e) =>
            {
                if (MessageBox.Show("Exit Battery Manager?", "Confirm Exit", 
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    OnExitClicked(s, e);
                }
            };

            _isInitialized = true;
            _logger.LogInformation("Tray icon initialized on UI thread");
        }

        /// <summary>
        /// Updates the tray icon with current battery percentage and charging status.
        /// </summary>
        public void UpdateBatteryLevel(int percentage, bool isCharging)
        {
            // Use Control.Invoke to ensure we're on UI thread
            if (_form != null && _form.InvokeRequired)
            {
                _form.Invoke(new Action(() => UpdateBatteryLevelCore(percentage, isCharging)));
            }
            else
            {
                UpdateBatteryLevelCore(percentage, isCharging);
            }
        }

        private void UpdateBatteryLevelCore(int percentage, bool isCharging)
        {
            try
            {
                // Ensure we're initialized on UI thread
                EnsureInitialized();
                
                if (_notifyIcon == null) return;

                // Update tooltip text
                var status = isCharging ? "Charging" : "Discharging";
                _notifyIcon.Text = $"Battery: {percentage}% ({status})";

                // Create custom icon with battery percentage text
                var newIcon = CreateBatteryIcon(percentage, isCharging);
                _notifyIcon.Icon = newIcon;

                _logger.LogDebug("Tray icon updated: {Percentage}%, {Status}", percentage, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tray icon");
            }
        }

        private Icon CreateBatteryIcon(int percentage, bool isCharging)
        {
            // Create a 64x64 bitmap for the icon
            var bitmap = new Bitmap(64, 64);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAlias;

                // Choose color based on battery level
                Color textColor;
                if (percentage <= 20)
                    textColor = Color.Red;
                else if (percentage >= 80)
                    textColor = Color.LimeGreen;
                else
                    textColor = Color.White;

                // Draw percentage text (larger font for better readability)
                var text = percentage.ToString();
                using (var font = new Font("Segoe UI", 28, FontStyle.Bold))
                using (var brush = new SolidBrush(textColor))
                {
                    var format = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    
                    g.DrawString(text, font, brush, new RectangleF(0, 0, 64, 64), format);
                }

                // Draw small charging indicator if charging
                if (isCharging)
                {
                    using (var pen = new Pen(Color.Yellow, 2))
                    {
                        // Draw lightning bolt symbol in top-right corner
                        g.DrawLine(pen, 50, 8, 44, 16);
                        g.DrawLine(pen, 44, 16, 48, 16);
                        g.DrawLine(pen, 48, 16, 42, 24);
                    }
                }
            }

            // Convert bitmap to icon
            IntPtr hIcon = bitmap.GetHicon();
            Icon icon = Icon.FromHandle(hIcon);
            
            return icon;
        }

        /// <summary>
        /// Shows the tray icon.
        /// </summary>
        public void Show()
        {
            if (_form != null && _form.InvokeRequired)
            {
                _form.Invoke(new Action(ShowCore));
            }
            else
            {
                ShowCore();
            }
        }

        private void ShowCore()
        {
            EnsureInitialized();
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = true;
                _logger.LogInformation("Tray icon shown");
            }
        }

        /// <summary>
        /// Hides the tray icon.
        /// </summary>
        public void Hide()
        {
            if (_form != null && _form.InvokeRequired)
            {
                _form.Invoke(new Action(HideCore));
            }
            else
            {
                HideCore();
            }
        }

        private void HideCore()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _logger.LogInformation("Tray icon hidden");
            }
        }

        /// <summary>
        /// Handles exit menu item click.
        /// </summary>
        private void OnExitClicked(object? sender, EventArgs e)
        {
            _logger.LogInformation("Exit requested from tray icon");
            Environment.Exit(0);
        }

        public void Dispose()
        {
            try
            {
                if (_form != null)
                {
                    _form.Dispose();
                    _form = null;
                }

                _notifyIcon = null;

                _exitMenuItem?.Dispose();
                _contextMenu?.Dispose();
                
                _logger.LogInformation("Tray icon service disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing tray icon service");
            }
        }
    }
}
