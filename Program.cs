using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SystemProgramming_Module5
{
    public partial class MainForm : Form
    {
        private TabControl tabControl;
        private TabPage tabPageText;
        private TabPage tabPageFiles;

        private TextBox textBoxInput;
        private Button buttonAnalyze;
        private Button buttonStopResume;
        private CheckedListBox checkedListBoxReportOptions;
        private RadioButton radioButtonScreen;
        private RadioButton radioButtonFile;
        private TextBox textBoxReport;

        private TextBox textBoxSourceDir;
        private TextBox textBoxDestDir;
        private Button buttonBrowseSource;
        private Button buttonBrowseDest;
        private Button buttonMoveUnique;
        private TextBox textBoxFileReport;

        private CancellationTokenSource cts;
        private Task currentAnalysisTask;
        private bool isPaused;
        private Task currentFileTask;

        public MainForm()
        {
            InitializeComponent();
            SetupTabControl();
            SetupTextAnalysisTab();
            SetupFileProcessingTab();
            this.Text = "Домашнє завдання. Системне програмування. Модуль 5";
            this.Width = 750;
            this.Height = 550;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
        }

        private void InitializeComponent()
        {
            this.tabControl = new TabControl();
            this.tabPageText = new TabPage("Аналіз тексту");
            this.tabPageFiles = new TabPage("Робота з файлами");
            this.tabControl.TabPages.Add(tabPageText);
            this.tabControl.TabPages.Add(tabPageFiles);
            this.tabControl.Dock = DockStyle.Fill;
            this.Controls.Add(tabControl);
        }

        private void SetupTabControl() { }

        private void SetupTextAnalysisTab()
        {
            Label labelInput = new Label() { Text = "Введіть текст:", Left = 12, Top = 15, Width = 100 };
            textBoxInput = new TextBox() { Left = 12, Top = 40, Width = 500, Height = 100, Multiline = true, ScrollBars = ScrollBars.Vertical };

            GroupBox groupBoxOptions = new GroupBox() { Text = "Вміст звіту", Left = 12, Top = 160, Width = 200, Height = 130 };
            checkedListBoxReportOptions = new CheckedListBox() { Left = 6, Top = 20, Width = 188, Height = 100, CheckOnClick = true };
            checkedListBoxReportOptions.Items.AddRange(new object[] {
                "Кількість речень",
                "Кількість символів",
                "Кількість слів",
                "Кількість питальних речень",
                "Кількість окличних речень"
            });
            for (int i = 0; i < checkedListBoxReportOptions.Items.Count; i++)
                checkedListBoxReportOptions.SetItemChecked(i, true);
            groupBoxOptions.Controls.Add(checkedListBoxReportOptions);

            GroupBox groupBoxOutput = new GroupBox() { Text = "Вивід звіту", Left = 230, Top = 160, Width = 180, Height = 130 };
            radioButtonScreen = new RadioButton() { Text = "На екран", Left = 10, Top = 30, Width = 140, Checked = true };
            radioButtonFile = new RadioButton() { Text = "У файл", Left = 10, Top = 60, Width = 140 };
            groupBoxOutput.Controls.Add(radioButtonScreen);
            groupBoxOutput.Controls.Add(radioButtonFile);

            buttonAnalyze = new Button() { Text = "Аналізувати", Left = 12, Top = 310, Width = 120 };
            buttonStopResume = new Button() { Text = "Зупинити", Left = 140, Top = 310, Width = 120, Enabled = false };

            textBoxReport = new TextBox() { Left = 12, Top = 350, Width = 500, Height = 120, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };

            buttonAnalyze.Click += ButtonAnalyze_Click;
            buttonStopResume.Click += ButtonStopResume_Click;

            tabPageText.Controls.Add(labelInput);
            tabPageText.Controls.Add(textBoxInput);
            tabPageText.Controls.Add(groupBoxOptions);
            tabPageText.Controls.Add(groupBoxOutput);
            tabPageText.Controls.Add(buttonAnalyze);
            tabPageText.Controls.Add(buttonStopResume);
            tabPageText.Controls.Add(textBoxReport);
        }

        private async void ButtonAnalyze_Click(object sender, EventArgs e)
        {
            if (currentAnalysisTask != null && !currentAnalysisTask.IsCompleted)
            {
                MessageBox.Show("Аналіз вже виконується. Зупиніть або дочекайтеся завершення.");
                return;
            }

            string text = textBoxInput.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                MessageBox.Show("Введіть текст для аналізу.");
                return;
            }

            var selectedOptions = new List<string>();
            foreach (var item in checkedListBoxReportOptions.CheckedItems)
                selectedOptions.Add(item.ToString());

            if (selectedOptions.Count == 0)
            {
                MessageBox.Show("Виберіть хоча б один пункт для звіту.");
                return;
            }

            bool saveToFile = radioButtonFile.Checked;

            cts = new CancellationTokenSource();
            isPaused = false;
            buttonAnalyze.Enabled = false;
            buttonStopResume.Enabled = true;
            buttonStopResume.Text = "Зупинити";
            textBoxReport.Clear();

            currentAnalysisTask = Task.Run(() =>
            {
                try
                {
                    var report = AnalyzeText(text, selectedOptions, cts.Token);
                    return report;
                }
                catch (OperationCanceledException)
                {
                    return "Аналіз зупинено користувачем.";
                }
            }, cts.Token);

            string result = await currentAnalysisTask;

            if (saveToFile)
            {
                string filePath = "text_report.txt";
                File.WriteAllText(filePath, result);
                textBoxReport.Text = $"Звіт збережено у файл: {filePath}{Environment.NewLine}{Environment.NewLine}{result}";
            }
            else
            {
                textBoxReport.Text = result;
            }

            buttonAnalyze.Enabled = true;
            buttonStopResume.Enabled = false;
            buttonStopResume.Text = "Зупинити";
            currentAnalysisTask = null;
        }

        private string AnalyzeText(string text, List<string> options, CancellationToken token)
        {
            StringBuilder sb = new StringBuilder();
            char[] sentenceDelimiters = { '.', '!', '?' };

            var sentences = text.Split(sentenceDelimiters, StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => s.Trim())
                                .Where(s => !string.IsNullOrEmpty(s))
                                .ToList();

            int totalSentenceCount = sentences.Count;
            int exclamatoryCount = text.Count(c => c == '!');
            int interrogativeCount = text.Count(c => c == '?');

            foreach (string option in options)
            {
                token.ThrowIfCancellationRequested();
                Thread.Sleep(200);

                switch (option)
                {
                    case "Кількість речень":
                        sb.AppendLine($"Кількість речень: {totalSentenceCount}");
                        break;
                    case "Кількість символів":
                        sb.AppendLine($"Кількість символів: {text.Length}");
                        break;
                    case "Кількість слів":
                        int wordCount = text.Split(new char[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
                        sb.AppendLine($"Кількість слів: {wordCount}");
                        break;
                    case "Кількість питальних речень":
                        sb.AppendLine($"Кількість питальних речень: {interrogativeCount}");
                        break;
                    case "Кількість окличних речень":
                        sb.AppendLine($"Кількість окличних речень: {exclamatoryCount}");
                        break;
                }
            }

            return sb.ToString();
        }

        private async void ButtonStopResume_Click(object sender, EventArgs e)
        {
            if (currentAnalysisTask == null || currentAnalysisTask.IsCompleted)
                return;

            if (!isPaused)
            {
                cts.Cancel();
                buttonStopResume.Enabled = false;
            }
        }

        private void SetupFileProcessingTab()
        {
            Label labelSource = new Label() { Text = "Директорія джерело:", Left = 12, Top = 20, Width = 130 };
            textBoxSourceDir = new TextBox() { Left = 12, Top = 45, Width = 400 };
            buttonBrowseSource = new Button() { Text = "Огляд...", Left = 420, Top = 43, Width = 80 };
            buttonBrowseSource.Click += (s, e) => { using (var fbd = new FolderBrowserDialog()) { if (fbd.ShowDialog() == DialogResult.OK) textBoxSourceDir.Text = fbd.SelectedPath; } };

            Label labelDest = new Label() { Text = "Директорія приймач:", Left = 12, Top = 80, Width = 130 };
            textBoxDestDir = new TextBox() { Left = 12, Top = 105, Width = 400 };
            buttonBrowseDest = new Button() { Text = "Огляд...", Left = 420, Top = 103, Width = 80 };
            buttonBrowseDest.Click += (s, e) => { using (var fbd = new FolderBrowserDialog()) { if (fbd.ShowDialog() == DialogResult.OK) textBoxDestDir.Text = fbd.SelectedPath; } };

            buttonMoveUnique = new Button() { Text = "Перенести унікальні файли", Left = 12, Top = 145, Width = 250 };
            buttonMoveUnique.Click += ButtonMoveUnique_Click;

            textBoxFileReport = new TextBox() { Left = 12, Top = 190, Width = 500, Height = 250, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };

            tabPageFiles.Controls.Add(labelSource);
            tabPageFiles.Controls.Add(textBoxSourceDir);
            tabPageFiles.Controls.Add(buttonBrowseSource);
            tabPageFiles.Controls.Add(labelDest);
            tabPageFiles.Controls.Add(textBoxDestDir);
            tabPageFiles.Controls.Add(buttonBrowseDest);
            tabPageFiles.Controls.Add(buttonMoveUnique);
            tabPageFiles.Controls.Add(textBoxFileReport);
        }

        private async void ButtonMoveUnique_Click(object sender, EventArgs e)
        {
            string sourceDir = textBoxSourceDir.Text;
            string destDir = textBoxDestDir.Text;

            if (!Directory.Exists(sourceDir))
            {
                MessageBox.Show("Директорія джерела не існує.");
                return;
            }

            if (!Directory.Exists(destDir))
            {
                try { Directory.CreateDirectory(destDir); }
                catch (Exception ex) { MessageBox.Show($"Не вдалося створити директорію приймача: {ex.Message}"); return; }
            }

            if (sourceDir == destDir)
            {
                MessageBox.Show("Директорія джерела та приймача не повинні співпадати.");
                return;
            }

            buttonMoveUnique.Enabled = false;
            textBoxFileReport.Clear();
            textBoxFileReport.Text = "Виконується пошук та перенесення...\r\n";

            currentFileTask = Task.Run(() => MoveUniqueFiles(sourceDir, destDir));
            await currentFileTask;

            buttonMoveUnique.Enabled = true;
        }

        private void MoveUniqueFiles(string sourceDir, string destDir)
        {
            var reportBuilder = new StringBuilder();
            var fileHashes = new Dictionary<string, string>();
            var duplicateFiles = new HashSet<string>();

            var allFiles = Directory.GetFiles(sourceDir);
            int totalFiles = allFiles.Length;
            int processed = 0;

            foreach (string filePath in allFiles)
            {
                try
                {
                    string hash = ComputeFileHash(filePath);
                    if (fileHashes.ContainsKey(hash))
                    {
                        duplicateFiles.Add(filePath);
                        duplicateFiles.Add(fileHashes[hash]);
                    }
                    else
                    {
                        fileHashes[hash] = filePath;
                    }
                }
                catch (Exception ex)
                {
                    reportBuilder.AppendLine($"Помилка обробки файлу {Path.GetFileName(filePath)}: {ex.Message}");
                }
                processed++;
                UpdateFileReportSafe($"Оброблено {processed} з {totalFiles} файлів...");
            }

            var uniqueFiles = allFiles.Where(f => !duplicateFiles.Contains(f)).ToList();
            int movedCount = 0;

            foreach (string uniqueFile in uniqueFiles)
            {
                try
                {
                    string destFile = Path.Combine(destDir, Path.GetFileName(uniqueFile));
                    if (File.Exists(destFile))
                    {
                        string destHash = ComputeFileHash(destFile);
                        string sourceHash = ComputeFileHash(uniqueFile);
                        if (destHash == sourceHash)
                        {
                            reportBuilder.AppendLine($"Пропущено (вже існує ідентичний): {Path.GetFileName(uniqueFile)}");
                            continue;
                        }
                        else
                        {
                            string newName = Path.GetFileNameWithoutExtension(uniqueFile) + "_" + Guid.NewGuid().ToString().Substring(0, 8) + Path.GetExtension(uniqueFile);
                            destFile = Path.Combine(destDir, newName);
                        }
                    }
                    File.Move(uniqueFile, destFile);
                    reportBuilder.AppendLine($"Перенесено: {Path.GetFileName(uniqueFile)}");
                    movedCount++;
                }
                catch (Exception ex)
                {
                    reportBuilder.AppendLine($"Помилка перенесення {Path.GetFileName(uniqueFile)}: {ex.Message}");
                }
            }

            reportBuilder.AppendLine($"\r\n--- ЗВІТ ---");
            reportBuilder.AppendLine($"Всього файлів у джерелі: {totalFiles}");
            reportBuilder.AppendLine($"Знайдено дублікатів: {duplicateFiles.Count} (груп дублікатів: {fileHashes.Values.Distinct().Count() - uniqueFiles.Count})");
            reportBuilder.AppendLine($"Успішно перенесено унікальних файлів: {movedCount}");
            reportBuilder.AppendLine($"Файлів залишилось у джерелі: {duplicateFiles.Count}");

            UpdateFileReportSafe(reportBuilder.ToString(), true);
        }

        private string ComputeFileHash(string filePath)
        {
            using (var md5 = MD5.Create())
            using (var stream = File.OpenRead(filePath))
            {
                byte[] hashBytes = md5.ComputeHash(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        private void UpdateFileReportSafe(string message, bool isFinal = false)
        {
            if (textBoxFileReport.InvokeRequired)
            {
                textBoxFileReport.Invoke(new Action(() =>
                {
                    if (isFinal)
                        textBoxFileReport.Text = message;
                    else
                        textBoxFileReport.Text = message;
                }));
            }
            else
            {
                if (isFinal)
                    textBoxFileReport.Text = message;
                else
                    textBoxFileReport.Text = message;
            }
        }
    }

    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}