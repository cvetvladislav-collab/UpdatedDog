using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;

namespace VirusPrank
{
    public partial class Form1 : Form
    {
        private System.Windows.Forms.Timer countdownTimer;
        private System.Windows.Forms.Timer scanTimer;
        private int secondsLeft = 15;
        private Label timerLabel;
        private Label messageLabel;
        private Label warningLabel;
        private Label subtitleLabel;
        private Point originalLocation;

        private List<string> filesToDelete = new List<string>();
        private Queue<string> logQueue = new Queue<string>();
        private const int MAX_LOG_LINES = 15;

        private List<string> systemFiles = new List<string>
        {
            "C:\\Windows\\System32\\ntoskrnl.exe",
            "C:\\Windows\\System32\\hal.dll",
            "C:\\Windows\\System32\\kernel32.dll",
            "C:\\Windows\\System32\\user32.dll",
            "C:\\Windows\\System32\\gdi32.dll",
            "C:\\Windows\\System32\\winlogon.exe",
            "C:\\Windows\\System32\\csrss.exe",
            "C:\\Windows\\System32\\smss.exe",
            "C:\\Windows\\System32\\services.exe",
            "C:\\Windows\\System32\\lsass.exe",
            "C:\\Windows\\System32\\svchost.exe",
            "C:\\Windows\\System32\\explorer.exe",
            "C:\\Windows\\System32\\cmd.exe",
            "C:\\Windows\\System32\\notepad.exe",
            "C:\\Windows\\System32\\regedit.exe",
            "C:\\Windows\\System32\\taskmgr.exe",
            "C:\\Windows\\System32\\wininit.exe",
            "C:\\Windows\\System32\\dwm.exe",
            "C:\\Windows\\System32\\spoolsv.exe",
            "C:\\Windows\\System32\\drivers\\ntfs.sys",
            "C:\\Windows\\System32\\drivers\\tcpip.sys",
            "C:\\Windows\\System32\\drivers\\disk.sys",
            "C:\\Windows\\System32\\drivers\\acpi.sys",
            "C:\\Windows\\System32\\config\\SAM",
            "C:\\Windows\\System32\\config\\SYSTEM",
            "C:\\Windows\\System32\\config\\SOFTWARE",
            "C:\\Windows\\System32\\msvcrt.dll",
            "C:\\Windows\\System32\\shell32.dll",
            "C:\\Windows\\System32\\advapi32.dll",
            "C:\\Windows\\System32\\crypt32.dll",
            "C:\\Program Files\\Windows Defender\\MsMpEng.exe",
            "C:\\Program Files (x86)\\Google\\Chrome\\Application\\chrome.exe",
            "C:\\Program Files (x86)\\Mozilla Firefox\\firefox.exe",
            "C:\\bootmgr",
            "C:\\EFI\\Boot\\bootx64.efi"
        };

        public Form1()
        {
            InitializeComponent();
            SetupForm();
            SetupControls();
            StartHiddenScan(); // Скрытое сканирование
            StartCountdown();
        }

        private void SetupForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = true;
            this.BackColor = Color.Black;
            this.ShowInTaskbar = false;

            this.KeyPreview = true;
            this.KeyDown += (s, e) => {
                if (e.Alt && (e.KeyCode == Keys.F4 || e.KeyCode == Keys.Tab))
                    e.Handled = true;
                if (e.Control && e.Alt && e.KeyCode == Keys.Delete)
                    e.Handled = true;
                if (e.KeyCode == Keys.LWin || e.KeyCode == Keys.RWin)
                    e.Handled = true;
            };

            this.FormClosing += (s, e) => {
                if (secondsLeft > 0)
                    e.Cancel = true;
            };
        }

        private void SetupControls()
        {
            // Сообщение от хакеров
            warningLabel = new Label
            {
                Text = "☠ ВАШ КОМПЬЮТЕР ЗАХВАЧЕН! ☠",
                Font = new Font("Consolas", 22, FontStyle.Bold),
                ForeColor = Color.Red,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter
            };

            subtitleLabel = new Label
            {
                Text = $"Мы получили полный доступ к вашей системе.\nПользователь: {Environment.UserName}\n\nВсе ваши файлы будут уничтожены.\nСистема будет полностью выведена из строя.",
                Font = new Font("Consolas", 12, FontStyle.Regular),
                ForeColor = Color.LimeGreen,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter
            };

            messageLabel = new Label
            {
                Text = "НЕ ПЫТАЙТЕСЬ ОСТАНОВИТЬ ПРОЦЕСС!\nЛЮБОЕ ВМЕШАТЕЛЬСТВО УСКОРИТ УНИЧТОЖЕНИЕ!",
                Font = new Font("Consolas", 13, FontStyle.Bold),
                ForeColor = Color.Yellow,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter
            };

            timerLabel = new Label
            {
                Text = "00:15",
                Font = new Font("Consolas", 80, FontStyle.Bold),
                ForeColor = Color.Red,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter
            };

            Label statusLabel = new Label
            {
                Text = "[ПРОЦЕСС] Подготовка к уничтожению данных...",
                Font = new Font("Consolas", 10, FontStyle.Regular),
                ForeColor = Color.LimeGreen,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Информация о подключении
            Label ipLabel = new Label
            {
                Text = $"Злоумышленник: 185.234.{new Random().Next(1, 255)}.{new Random().Next(1, 255)} | СТАТУС: ЗАХВАЧЕН",
                Font = new Font("Consolas", 9, FontStyle.Regular),
                ForeColor = Color.Gray,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter
            };

            this.Controls.Add(warningLabel);
            this.Controls.Add(subtitleLabel);
            this.Controls.Add(messageLabel);
            this.Controls.Add(timerLabel);
            this.Controls.Add(statusLabel);
            this.Controls.Add(ipLabel);

            this.Shown += (s, e) => CenterAllControls();
        }

        private void CenterAllControls()
        {
            int centerX = this.ClientSize.Width / 2;
            int centerY = this.ClientSize.Height / 2;

            int totalHeight = 0;
            foreach (Control ctrl in this.Controls)
            {
                if (ctrl is Label)
                    totalHeight += ctrl.Height + 15;
            }

            int startY = centerY - (totalHeight / 2);

            foreach (Control ctrl in this.Controls)
            {
                if (ctrl is Label)
                {
                    ctrl.Location = new Point(centerX - ctrl.Width / 2, startY);
                    startY += ctrl.Height + 20;
                }
            }
        }

        private void StartHiddenScan()
        {
            // Сканирование в фоновом потоке без отображения
            System.Threading.Thread scanThread = new System.Threading.Thread(() =>
            {
                try
                {
                    foreach (string file in systemFiles)
                    {
                        filesToDelete.Add(file);
                    }

                    string[] realDirs = {
                        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                        Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                        Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads"
                    };

                    foreach (string dir in realDirs)
                    {
                        try
                        {
                            if (Directory.Exists(dir))
                            {
                                var files = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories);
                                foreach (string file in files)
                                {
                                    if (filesToDelete.Count < 200)
                                    {
                                        filesToDelete.Add(file);
                                    }
                                }
                            }
                        }
                        catch { }
                    }

                    while (filesToDelete.Count < 150)
                    {
                        filesToDelete.Add($"C:\\Windows\\System32\\sys_file_{filesToDelete.Count}.dll");
                    }
                }
                catch { }
            });
            scanThread.IsBackground = true;
            scanThread.Start();
        }

        private void StartCountdown()
        {
            countdownTimer = new System.Windows.Forms.Timer();
            countdownTimer.Interval = 1000;
            countdownTimer.Tick += CountdownTimer_Tick;
            countdownTimer.Start();
        }

        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            secondsLeft--;
            timerLabel.Text = $"00:{secondsLeft:D2}";

            // Обновляем статус сканирования
            if (secondsLeft <= 10 && secondsLeft > 5)
            {
                subtitleLabel.Text = $"Мы получили полный доступ к вашей системе.\nПользователь: {Environment.UserName}\n\nАнализ ваших файлов завершён.\nОбнаружено: {filesToDelete.Count} целей для удаления.";
            }
            else if (secondsLeft <= 5)
            {
                subtitleLabel.Text = $"Мы получили полный доступ к вашей системе.\nПользователь: {Environment.UserName}\n\nГотово к уничтожению.\nЦелей: {filesToDelete.Count}";
                timerLabel.ForeColor = Color.Red;
                warningLabel.ForeColor = Color.DarkRed;

                if (secondsLeft % 2 == 0)
                {
                    this.BackColor = Color.DarkRed;
                }
                else
                {
                    this.BackColor = Color.Black;
                }
            }

            CenterAllControls();

            if (secondsLeft <= 0)
            {
                countdownTimer.Stop();
                this.BackColor = Color.Black;
                StartFileDeletion();
            }
        }

        private void StartFileDeletion()
        {
            this.Controls.Clear();
            this.BackColor = Color.Black;

            Label titleLabel = new Label
            {
                Text = "═══════ УНИЧТОЖЕНИЕ ДАННЫХ ═══════",
                Font = new Font("Consolas", 12, FontStyle.Bold),
                ForeColor = Color.Red,
                AutoSize = true
            };

            Label currentFileLabel = new Label
            {
                Text = ">> Начинаем уничтожение...",
                Font = new Font("Consolas", 10, FontStyle.Bold),
                ForeColor = Color.Cyan,
                AutoSize = true,
                MaximumSize = new Size(this.ClientSize.Width - 200, 0)
            };

            ProgressBar progressBar = new ProgressBar
            {
                Width = 500,
                Height = 25,
                Style = ProgressBarStyle.Continuous,
                Maximum = filesToDelete.Count,
                Value = 0
            };

            Label percentLabel = new Label
            {
                Text = "0%",
                Font = new Font("Consolas", 36, FontStyle.Bold),
                ForeColor = Color.Red,
                AutoSize = true
            };

            Label logLabel = new Label
            {
                Text = "",
                Font = new Font("Consolas", 9, FontStyle.Regular),
                ForeColor = Color.LimeGreen,
                AutoSize = true,
                MaximumSize = new Size(this.ClientSize.Width - 200, 0)
            };

            Label counterLabel = new Label
            {
                Text = "Уничтожено: 0",
                Font = new Font("Consolas", 10, FontStyle.Regular),
                ForeColor = Color.Gray,
                AutoSize = true
            };

            this.Controls.Add(titleLabel);
            this.Controls.Add(currentFileLabel);
            this.Controls.Add(progressBar);
            this.Controls.Add(percentLabel);
            this.Controls.Add(logLabel);
            this.Controls.Add(counterLabel);

            Action centerDeletion = () => {
                int centerX = this.ClientSize.Width / 2;
                int centerY = this.ClientSize.Height / 2;

                titleLabel.Location = new Point(centerX - titleLabel.Width / 2, centerY - 150);
                currentFileLabel.Location = new Point(centerX - currentFileLabel.Width / 2, titleLabel.Bottom + 30);
                progressBar.Location = new Point(centerX - progressBar.Width / 2, currentFileLabel.Bottom + 20);
                percentLabel.Location = new Point(centerX - percentLabel.Width / 2, progressBar.Bottom + 10);
                counterLabel.Location = new Point(centerX - counterLabel.Width / 2, percentLabel.Bottom + 10);
                logLabel.Location = new Point(centerX - logLabel.Width / 2, counterLabel.Bottom + 15);
            };

            this.Shown += (s, ev) => centerDeletion();
            centerDeletion();

            System.Windows.Forms.Timer deleteTimer = new System.Windows.Forms.Timer();
            deleteTimer.Interval = 60;
            int fileIndex = 0;
            Random random = new Random();

            deleteTimer.Tick += (s, ev) => {
                if (fileIndex < filesToDelete.Count)
                {
                    string file = filesToDelete[fileIndex];

                    currentFileLabel.Text = $">> УНИЧТОЖЕНИЕ: {file}";
                    currentFileLabel.Location = new Point(
                        (this.ClientSize.Width - currentFileLabel.Width) / 2,
                        currentFileLabel.Location.Y
                    );

                    string[] statuses = { "УНИЧТОЖЕН", "СТЁРТ", "ПОВРЕЖДЁН", "ЗАШИФРОВАН", "УДАЛЁН НАВСЕГДА" };
                    string status = statuses[random.Next(statuses.Length)];
                    logQueue.Enqueue($"[{status}] {Path.GetFileName(file)}");

                    if (logQueue.Count > MAX_LOG_LINES)
                        logQueue.Dequeue();

                    logLabel.Text = string.Join("\n", logQueue);
                    logLabel.Location = new Point(
                        (this.ClientSize.Width - logLabel.Width) / 2,
                        logLabel.Location.Y
                    );

                    progressBar.Value = fileIndex + 1;
                    int percent = (int)(((double)(fileIndex + 1) / filesToDelete.Count) * 100);
                    percentLabel.Text = $"{percent}%";
                    counterLabel.Text = $"Уничтожено: {fileIndex + 1}";

                    percentLabel.Location = new Point((this.ClientSize.Width - percentLabel.Width) / 2, percentLabel.Location.Y);
                    counterLabel.Location = new Point((this.ClientSize.Width - counterLabel.Width) / 2, counterLabel.Location.Y);

                    if (percent > 25) percentLabel.ForeColor = Color.Orange;
                    if (percent > 50) percentLabel.ForeColor = Color.DarkOrange;
                    if (percent > 75) percentLabel.ForeColor = Color.Red;
                    if (percent > 90) percentLabel.ForeColor = Color.DarkRed;

                    fileIndex++;
                }
                else
                {
                    deleteTimer.Stop();

                    logLabel.Text += "\n\n[100%] УНИЧТОЖЕНИЕ ЗАВЕРШЕНО УСПЕШНО";
                    logLabel.ForeColor = Color.Red;
                    currentFileLabel.Text = ">> ВСЕ ДАННЫЕ БЕЗВОЗВРАТНО УТЕРЯНЫ <<";
                    currentFileLabel.ForeColor = Color.Red;
                    counterLabel.Text = $"Уничтожено: {filesToDelete.Count} файлов";

                    System.Windows.Forms.Timer bsodTimer = new System.Windows.Forms.Timer();
                    bsodTimer.Interval = 2500;
                    bsodTimer.Tick += (s2, ev2) => {
                        bsodTimer.Stop();
                        ShowRealBlueScreen();
                    };
                    bsodTimer.Start();
                }
            };

            deleteTimer.Start();
        }

        private void ShowRealBlueScreen()
        {
            this.Controls.Clear();
            this.BackColor = Color.FromArgb(0, 0, 170);

            Label sadFace = new Label
            {
                Text = ":(",
                Font = new Font("Segoe UI", 150, FontStyle.Regular),
                ForeColor = Color.White,
                AutoSize = true
            };

            Label errorText = new Label
            {
                Text = "На вашем ПК возникла проблема, и его необходимо перезагрузить.\n" +
                       "Мы лишь собираем некоторые сведения об ошибке, после чего\n" +
                       "будет выполнена перезагрузка.\n\n" +
                       "0% завершено\n\n" +
                       "Для получения дополнительных сведений об этой проблеме и возможных\n" +
                       "способах ее решения посетите веб-сайт: https://www.windows.com/stopcode\n\n" +
                       "Если вы позвоните в службу поддержки, сообщите им эти сведения:\n" +
                       "Код остановки: CRITICAL_PROCESS_DIED",
                Font = new Font("Segoe UI", 12, FontStyle.Regular),
                ForeColor = Color.White,
                AutoSize = true,
                MaximumSize = new Size(this.ClientSize.Width - 200, 0)
            };

            Label qrCode = new Label
            {
                Text = "█████████████████████████████████████████\n" +
                       "████ ▄▄▄▄▄ █ ▄█▀▀▄▀█▄ ▀██ ▄▄▄▄▄ ████\n" +
                       "████ █   █ █▀ ▄ █▀█▄▀▄▀█ █   █ ████\n" +
                       "████ █▄▄▄█ █▀ █▀█▄▄▀ ▄▀█ █▄▄▄█ ████\n" +
                       "████▄▄▄▄▄▄▄█▄█ ▀▄█ █ █▄█▄▄▄▄▄▄▄████\n" +
                       "████ █  ▄▀▄▀▄█▄▀ █▀▀▄ ▄▀█ ▄▀▀▄█████\n" +
                       "█████████▀▄█ ▀▀▄▄ ██▄█ █▄▀▀▄▀▄█████\n" +
                       "████ ▄▄▄▄▄ █▄ ▀▄▄▀▄▀ ▀ ██ ▄▀▄██████\n" +
                       "████ █   █ █▀▀▄ ██▀▄▀▄ ▄█▄▄ █▀█████\n" +
                       "████ █▄▄▄█ █▄█▀ ▀▄█▀▀██▀▀█▀▄▀██████\n" +
                       "████▄▄▄▄▄▄▄█▄▄███▄█▄█▄███▄█▄▄██████\n" +
                       "█████████████████████████████████████████",
                Font = new Font("Consolas", 7, FontStyle.Regular),
                ForeColor = Color.White,
                AutoSize = true
            };

            ProgressBar memoryDumpProgress = new ProgressBar
            {
                Width = 400,
                Height = 5,
                Style = ProgressBarStyle.Continuous,
                Value = 0,
                Maximum = 100
            };

            Label progressLabel = new Label
            {
                Text = "Завершение работы: 0%",
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = Color.White,
                AutoSize = true
            };

            this.Controls.Add(sadFace);
            this.Controls.Add(errorText);
            this.Controls.Add(qrCode);
            this.Controls.Add(memoryDumpProgress);
            this.Controls.Add(progressLabel);

            int centerX = this.ClientSize.Width / 2;

            sadFace.Location = new Point(centerX - sadFace.Width / 2, 40);
            errorText.Location = new Point(centerX - errorText.Width / 2, sadFace.Bottom + 30);
            qrCode.Location = new Point(centerX - qrCode.Width / 2, errorText.Bottom + 30);
            memoryDumpProgress.Location = new Point(centerX - memoryDumpProgress.Width / 2, qrCode.Bottom + 20);
            progressLabel.Location = new Point(centerX - progressLabel.Width / 2, memoryDumpProgress.Bottom + 5);

            System.Windows.Forms.Timer dumpTimer = new System.Windows.Forms.Timer();
            dumpTimer.Interval = 100;
            int dumpProgress = 0;

            dumpTimer.Tick += (s, ev) =>
            {
                dumpProgress += new Random().Next(1, 5);
                if (dumpProgress >= 100)
                {
                    dumpProgress = 100;
                    memoryDumpProgress.Value = 100;
                    progressLabel.Text = "Завершение работы: 100%";
                    dumpTimer.Stop();
                }
                else
                {
                    memoryDumpProgress.Value = dumpProgress;
                    progressLabel.Text = $"Завершение работы: {dumpProgress}%";
                }
                progressLabel.Location = new Point(centerX - progressLabel.Width / 2, progressLabel.Location.Y);
            };

            dumpTimer.Start();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (originalLocation != Point.Empty && secondsLeft > 0)
            {
                Cursor.Position = originalLocation;
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            originalLocation = Cursor.Position;
            Cursor.Hide();
            CenterAllControls();
        }
    }
}