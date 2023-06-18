using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Security.AccessControl;
using System.Windows.Forms;

namespace GameDownloadSystem
{
    public partial class MainForm : Form
    {
        private const string GamesFolderPath = "Games";
        private ListView gamesListView;
        private string selectedFolder;
        private ImageList gameIconList;
        private Panel buttonPanel;

        public MainForm()
        {
            InitializeComponent();
            InitializeGamesListView();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            LoadGames();
        }

        private void LoadGames()
        {
            if (!Directory.Exists(GamesFolderPath))
                Directory.CreateDirectory(GamesFolderPath);

            string[] gameFolders = Directory.GetDirectories(GamesFolderPath);

            gamesListView.Items.Clear();
            gameIconList.Images.Clear();

            foreach (string folderPath in gameFolders)
            {
                string folderName = Path.GetFileName(folderPath);
                string manifestPath = Path.Combine(folderPath, "manifest.json");
                string iconPath = Path.Combine(folderPath, "icon.png");

                if (!File.Exists(manifestPath) || !File.Exists(iconPath))
                    continue;

                string manifestContent = File.ReadAllText(manifestPath);

                // Load and set the game icon
                Image gameIcon = Image.FromFile(iconPath);
                gameIconList.Images.Add(gameIcon);

                ListViewItem listViewItem = new ListViewItem();
                listViewItem.Text = folderName;
                listViewItem.ImageIndex = gameIconList.Images.Count - 1;

                gamesListView.Items.Add(listViewItem);
            }

            gamesListView.LargeImageList = gameIconList;
        }

        private void CreateButton_Click(object sender, EventArgs e)
        {
            CreateGameForm createGameForm = new CreateGameForm();
            DialogResult result = createGameForm.ShowDialog();

            if (result == DialogResult.OK)
            {
                string gameName = createGameForm.GameName;
                string gameFolderPath = Path.Combine(createGameForm.SelectedFolder, gameName);

                if (Directory.Exists(gameFolderPath))
                {
                    MessageBox.Show("The game folder already exists.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                Directory.CreateDirectory(gameFolderPath);

                string manifestPath = Path.Combine(gameFolderPath, "manifest.json");
                string manifestContent = $"{{\"name\": \"{gameName}\", \"description\": \"Game description here\"}}";
                File.WriteAllText(manifestPath, manifestContent);

                string iconPath = Path.Combine(gameFolderPath, "icon.png");

                string instructionsPath = Path.Combine(gameFolderPath, "instructions.txt");
                string instructionsContent = "Instructions for playing the game.";
                File.WriteAllText(instructionsPath, instructionsContent);

                string downloadsFolderPath = Path.Combine(gameFolderPath, "Downloads");
                Directory.CreateDirectory(downloadsFolderPath);

                LoadGames();
            }
        }

        private string lastUsedFolderFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "LastUsedFolder.txt");

        private void LoadButton_Click(object sender, EventArgs e)
        {
            using (var fileDialog = new OpenFileDialog())
            {
                fileDialog.Filter = "DERO Files (*.dero)|*.dero";
                fileDialog.Title = "Select a .dero file";
                DialogResult result = fileDialog.ShowDialog();

                if (result == DialogResult.OK)
                {
                    string selectedPath = fileDialog.FileName;

                    if (File.Exists(selectedPath))
                    {
                        string fileExtension = Path.GetExtension(selectedPath);

                        if (fileExtension.Equals(".dero", StringComparison.OrdinalIgnoreCase))
                        {
                            string zipFilePath = Path.ChangeExtension(selectedPath, ".zip");

                            File.Move(selectedPath, zipFilePath);

                            string extractPath = Path.Combine(Path.GetDirectoryName(zipFilePath), "Extracted");
                            ZipFile.ExtractToDirectory(zipFilePath, extractPath);

                            Thread.Sleep(2000);

                            if (Directory.Exists(extractPath))
                            {
                                string[] subDirectories = Directory.GetDirectories(extractPath);
                                if (subDirectories.Length > 0)
                                {
                                    bool foundMatchingFolder = false;
                                    foreach (string subDirectory in subDirectories)
                                    {
                                        string subDirectoryName = Path.GetFileName(subDirectory);

                                        if (subDirectoryName.Equals(Path.GetFileNameWithoutExtension(selectedPath), StringComparison.OrdinalIgnoreCase))
                                        {
                                            foundMatchingFolder = true;

                                            string subDownloadFolderPath = Path.Combine(subDirectory, "Download");

                                            Thread.Sleep(2000);

                                            if (Directory.Exists(subDownloadFolderPath))
                                            {
                                                string documentsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                                                string fullSubDownloadFolderPath = Path.Combine(extractPath, subDownloadFolderPath);
                                                if (Directory.Exists(fullSubDownloadFolderPath))
                                                {
                                                    string[] subDownloadExeFiles = Directory.GetFiles(fullSubDownloadFolderPath, "*.exe");
                                                    if (subDownloadExeFiles.Length > 0)
                                                    {
                                                        string exeFilePath = subDownloadExeFiles[0];

                                                        string tempFolderPath = Path.Combine(documentsFolderPath, "TempDownloadFolder");
                                                        Directory.Move(fullSubDownloadFolderPath, tempFolderPath);

                                                        if (Directory.Exists(subDownloadFolderPath))
                                                            Directory.Delete(subDownloadFolderPath, true);

                                                        Directory.Move(tempFolderPath, subDownloadFolderPath);

                                                        ProcessStartInfo startInfo = new ProcessStartInfo
                                                        {
                                                            FileName = "cmd.exe",
                                                            Arguments = $"/C \"{exeFilePath}\"",
                                                            WorkingDirectory = subDownloadFolderPath,
                                                            Verb = "runas"
                                                        };

                                                        try
                                                        {
                                                            Process.Start(startInfo);
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            MessageBox.Show($"An error occurred: {ex.Message}");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        MessageBox.Show("No executable files found inside the 'Download' folder.");
                                                    }
                                                }
                                                else
                                                {
                                                    MessageBox.Show("The 'Download' folder does not exist inside the extracted folder.");
                                                }
                                            }
                                            else
                                            {
                                                MessageBox.Show("The 'Download' folder does not exist inside the subdirectory.");
                                            }

                                            break;
                                        }
                                    }

                                    if (!foundMatchingFolder)
                                    {
                                        MessageBox.Show("No matching folder found inside the extracted contents.");
                                    }
                                }
                                else
                                {
                                    MessageBox.Show("No subdirectories found inside the extracted folder.");
                                }
                            }
                            else
                            {
                                MessageBox.Show("The extracted folder does not exist.");
                            }
                        }
                        else
                        {
                            MessageBox.Show("Selected file is not a .dero file.");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Invalid selection.");
                    }
                }
            }

            using (var folderDialog = new FolderBrowserDialog())
            {
                DialogResult result = folderDialog.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(folderDialog.SelectedPath))
                {
                    selectedFolder = folderDialog.SelectedPath;

                    string folderName = Path.GetFileName(selectedFolder);
                    string manifestPath = Path.Combine(selectedFolder, "manifest.json");
                    string iconPath = Path.Combine(selectedFolder, "icon.png");

                    if (File.Exists(manifestPath) && File.Exists(iconPath))
                    {
                        GameItem gameItem = new GameItem();
                        gameItem.Name = folderName;
                        gamesListView.Items.Add(gameItem.Name);

                        string downloadFolderPath = Path.Combine(selectedFolder, "Download");
                        Directory.CreateDirectory(downloadFolderPath);

                        string[] exeFiles = Directory.GetFiles(downloadFolderPath, "*.exe");
                        if (exeFiles.Length > 0)
                        {
                            string exeFilePath = exeFiles[0];
                            RunExecutable(exeFilePath, downloadFolderPath);

                            string extractPath = Path.Combine(downloadFolderPath, "Extracted");
                            if (Directory.Exists(extractPath))
                            {
                                if (!extractPath.Equals(selectedFolder, StringComparison.OrdinalIgnoreCase))
                                {
                                    Directory.Delete(extractPath, true);
                                }
                            }

                            string renamedFilePath = Path.ChangeExtension(exeFilePath, ".dero");
                            if (File.Exists(renamedFilePath))
                            {
                                File.Move(renamedFilePath, exeFilePath);
                            }
                        }
                        else
                        {
                            string[] deroFiles = Directory.GetFiles(downloadFolderPath, "*.dero");
                            if (deroFiles.Length > 0)
                            {
                                string deroFilePath = deroFiles[0];
                                string zipFilePath = Path.ChangeExtension(deroFilePath, ".zip");

                                File.Move(deroFilePath, zipFilePath);

                                string extractPath = Path.Combine(downloadFolderPath, "Extracted");
                                ZipFile.ExtractToDirectory(zipFilePath, extractPath);

                                Thread.Sleep(2000);

                                if (Directory.Exists(extractPath))
                                {
                                    string[] subDirectories = Directory.GetDirectories(extractPath);
                                    if (subDirectories.Length > 0)
                                    {
                                        bool foundMatchingFolder = false;
                                        foreach (string subDirectory in subDirectories)
                                        {
                                            string subDirectoryName = Path.GetFileName(subDirectory);

                                            if (subDirectoryName.Equals(folderName, StringComparison.OrdinalIgnoreCase))
                                            {
                                                foundMatchingFolder = true;

                                                string subDownloadFolderPath = Path.Combine(subDirectory, "Download");

                                                Thread.Sleep(2000);

                                                if (Directory.Exists(subDownloadFolderPath))
                                                {
                                                    string documentsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                                                    string fullSubDownloadFolderPath = Path.Combine(downloadFolderPath, subDownloadFolderPath);
                                                    if (Directory.Exists(fullSubDownloadFolderPath))
                                                    {
                                                        string[] subDownloadExeFiles = Directory.GetFiles(fullSubDownloadFolderPath, "*.exe");
                                                        if (subDownloadExeFiles.Length > 0)
                                                        {
                                                            string exeFilePath = subDownloadExeFiles[0];

                                                            string tempFolderPath = Path.Combine(documentsFolderPath, "TempDownloadFolder");
                                                            Directory.Move(fullSubDownloadFolderPath, tempFolderPath);

                                                            if (Directory.Exists(downloadFolderPath))
                                                                Directory.Delete(downloadFolderPath, true);

                                                            Directory.Move(tempFolderPath, downloadFolderPath);

                                                            ProcessStartInfo startInfo = new ProcessStartInfo
                                                            {
                                                                FileName = "cmd.exe",
                                                                Arguments = $"/C \"{exeFilePath}\"",
                                                                WorkingDirectory = downloadFolderPath,
                                                                Verb = "runas"
                                                            };
                                                            Process.Start(startInfo);

                                                            Thread.Sleep(2000);

                                                            string renamedFilePath = zipFilePath.Replace(".zip", ".dero");
                                                            File.Move(zipFilePath, renamedFilePath);

                                                            break;
                                                        }
                                                        else
                                                        {
                                                            MessageBox.Show("No executable file (.exe) found in the 'Download' folder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                                            break;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    MessageBox.Show("The 'Download' folder does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                                    break;
                                                }
                                            }
                                        }

                                        if (!foundMatchingFolder)
                                        {
                                            MessageBox.Show("No folder with the same name as the chosen folder found inside the extracted folder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        }
                                    }
                                    else
                                    {
                                        MessageBox.Show("No subdirectories found inside the extracted folder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    }

                                    Thread.Sleep(2000);

                                    if (!extractPath.Equals(selectedFolder, StringComparison.OrdinalIgnoreCase))
                                    {
                                        Directory.Delete(extractPath, true);
                                    }
                                }
                                else
                                {
                                    MessageBox.Show("The extracted folder does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            }
                            else
                            {
                                string[] zipFiles = Directory.GetFiles(downloadFolderPath, "*.zip");
                                string zipFilePath = "";

                                if (zipFiles.Length > 0)
                                {
                                    zipFilePath = zipFiles[0];

                                    string extractPath = Path.Combine(downloadFolderPath, "Extracted");
                                    ZipFile.ExtractToDirectory(zipFilePath, extractPath);

                                    Thread.Sleep(2000);

                                    if (Directory.Exists(extractPath))
                                    {
                                        string[] subDirectories = Directory.GetDirectories(extractPath);
                                        if (subDirectories.Length > 0)
                                        {
                                            bool foundMatchingFolder = false;
                                            foreach (string subDirectory in subDirectories)
                                            {
                                                string subDirectoryName = Path.GetFileName(subDirectory);

                                                if (subDirectoryName.Equals(folderName, StringComparison.OrdinalIgnoreCase))
                                                {
                                                    foundMatchingFolder = true;

                                                    string subDownloadFolderPath = Path.Combine(subDirectory, "Download");

                                                    Thread.Sleep(2000);

                                                    string documentsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                                                    string fullSubDownloadFolderPath = Path.Combine(downloadFolderPath, subDownloadFolderPath);
                                                    if (Directory.Exists(fullSubDownloadFolderPath))
                                                    {
                                                        string[] subDownloadExeFiles = Directory.GetFiles(fullSubDownloadFolderPath, "*.exe");
                                                        if (subDownloadExeFiles.Length > 0)
                                                        {
                                                            string exeFilePath = subDownloadExeFiles[0];

                                                            string tempFolderPath = Path.Combine(documentsFolderPath, "TempDownloadFolder");
                                                            Directory.Move(fullSubDownloadFolderPath, tempFolderPath);

                                                            if (Directory.Exists(downloadFolderPath))
                                                                Directory.Delete(downloadFolderPath, true);

                                                            Directory.Move(tempFolderPath, downloadFolderPath);

                                                            ProcessStartInfo startInfo = new ProcessStartInfo
                                                            {
                                                                FileName = "cmd.exe",
                                                                Arguments = $"/C \"{exeFilePath}\"",
                                                                WorkingDirectory = downloadFolderPath,
                                                                Verb = "runas"
                                                            };
                                                            Process.Start(startInfo);

                                                            Thread.Sleep(2000);

                                                            string renamedFilePath = zipFilePath.Replace(".zip", ".dero");
                                                            File.Move(zipFilePath, renamedFilePath);

                                                            break;
                                                        }
                                                        else
                                                        {
                                                            MessageBox.Show("No executable file (.exe) found in the 'Download' folder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                                            break;
                                                        }
                                                    }
                                                }
                                            }

                                            if (!foundMatchingFolder)
                                            {
                                                MessageBox.Show("No folder with the same name as the chosen folder found inside the extracted folder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            }
                                        }
                                        else
                                        {
                                            MessageBox.Show("No subdirectories found inside the extracted folder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        }

                                        Thread.Sleep(2000);

                                        if (!extractPath.Equals(selectedFolder, StringComparison.OrdinalIgnoreCase))
                                        {
                                            Directory.Delete(extractPath, true);
                                        }
                                    }
                                    else
                                    {
                                        MessageBox.Show("The extracted folder does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    }
                                }
                                else
                                {
                                    MessageBox.Show("No .zip or executable file found in the download folder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            }

                            File.WriteAllText(lastUsedFolderFile, selectedFolder);
                        }
                    }
                }
            }
        }

        private void MoveFolderContents(string sourceFolder, string destinationFolder)
        {
            if (!Directory.Exists(destinationFolder))
            {
                Directory.CreateDirectory(destinationFolder);
            }

            string[] files = Directory.GetFiles(sourceFolder);
            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);
                string destinationPath = Path.Combine(destinationFolder, fileName);
                File.Move(file, destinationPath);
            }

            string[] directories = Directory.GetDirectories(sourceFolder);
            foreach (string directory in directories)
            {
                string directoryName = Path.GetFileName(directory);
                string destinationPath = Path.Combine(destinationFolder, directoryName);
                MoveFolderContents(directory, destinationPath);
                Directory.Delete(directory, true);
            }
        }

        private void RunExecutable(string exeFilePath, string workingDirectory)
        {
            if (File.Exists(exeFilePath))
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C \"{exeFilePath}\"",
                    WorkingDirectory = workingDirectory,
                    Verb = "runas"
                };
                Process.Start(startInfo);
            }
            else
            {
                MessageBox.Show("Executable file not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowErrorMessage(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (File.Exists(lastUsedFolderFile))
            {
                string[] lastUsedFolders = File.ReadAllLines(lastUsedFolderFile);

                foreach (string folderPath in lastUsedFolders)
                {
                    lastUsedListView.Items.Add(folderPath);
                }
            }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            float scalingFactorX = (float)this.ClientSize.Width / 320;
            float scalingFactorY = (float)this.ClientSize.Height / 260;

            int listViewWidth = (int)(300 * scalingFactorX);
            int listViewHeight = (int)(200 * scalingFactorY);
            int listViewLeft = 10;
            int listViewTop = 10;

            if (tableLayoutPanel != null)
            {
                int tableLayoutPanelWidth = (int)(300 * scalingFactorX);
                int tableLayoutPanelHeight = (int)(23 * scalingFactorY);
                int tableLayoutPanelLeft = 10;
                int tableLayoutPanelTop = (int)(220 * scalingFactorY);

                tableLayoutPanel.Location = new System.Drawing.Point(tableLayoutPanelLeft, tableLayoutPanelTop);
                tableLayoutPanel.Size = new System.Drawing.Size(tableLayoutPanelWidth, tableLayoutPanelHeight);

                listViewTop = tableLayoutPanelTop - listViewHeight - (int)(10 * scalingFactorY);
            }

            int lastUsedListViewWidth = (int)(300 * scalingFactorX);
            int lastUsedListViewHeight = (int)(100 * scalingFactorY);
            int lastUsedListViewLeft = 10;
            int lastUsedListViewTop = listViewTop - lastUsedListViewHeight - (int)(10 * scalingFactorY);

            gamesListView.Size = new System.Drawing.Size(listViewWidth, listViewHeight);
            gamesListView.Location = new System.Drawing.Point(listViewLeft, listViewTop);

            lastUsedListView.Size = new System.Drawing.Size(lastUsedListViewWidth, lastUsedListViewHeight);
            lastUsedListView.Location = new System.Drawing.Point(lastUsedListViewLeft, lastUsedListViewTop);
        }

        private TableLayoutPanel tableLayoutPanel;
        private ListView lastUsedListView;

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
            this.ClientSize = new System.Drawing.Size(320, 260);
            this.ForeColor = System.Drawing.Color.White;
            this.Name = "Dero GameLoader";
            this.Text = "Game Manager";
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.StartPosition = FormStartPosition.CenterScreen;

            gamesListView = new ListView();
            gamesListView.Location = new System.Drawing.Point(10, 10);
            gamesListView.Size = new System.Drawing.Size(300, 200);
            gamesListView.View = View.LargeIcon;
            gamesListView.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
            gamesListView.ForeColor = System.Drawing.Color.White;
            gamesListView.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            gamesListView.LargeImageList = gameIconList;
            gamesListView.OwnerDraw = true;
            gamesListView.DrawItem += GamesListView_DrawItem;
            Controls.Add(gamesListView);

            gameIconList = new ImageList();
            gameIconList.ImageSize = new Size(64, 89);

            tableLayoutPanel = new TableLayoutPanel();
            tableLayoutPanel.Location = new System.Drawing.Point(10, 220);
            tableLayoutPanel.Size = new System.Drawing.Size(300, 23);
            tableLayoutPanel.ColumnCount = 2;
            tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.Controls.Add(tableLayoutPanel);

            Button loadButton = new Button();
            loadButton.Text = "Load";
            loadButton.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
            loadButton.ForeColor = System.Drawing.Color.White;
            loadButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            loadButton.Click += LoadButton_Click;
            tableLayoutPanel.Controls.Add(loadButton, 0, 0);

            Button createButton = new Button();
            createButton.Text = "Create";
            createButton.BackColor = System.Drawing.Color.LightBlue;
            createButton.ForeColor = System.Drawing.Color.White;
            createButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            createButton.Margin = new Padding(5, 0, 0, 0);

            createButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            createButton.FlatAppearance.BorderSize = 0;
            createButton.Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, createButton.Width, createButton.Height, 5, 5));

            createButton.Click += CreateButton_Click;
            tableLayoutPanel.Controls.Add(createButton, 1, 0);

            lastUsedListView = new ListView();
            lastUsedListView.Location = new System.Drawing.Point(10, 220 + tableLayoutPanel.Height + 10);
            lastUsedListView.Size = new System.Drawing.Size(300, 100);
            lastUsedListView.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
            lastUsedListView.ForeColor = System.Drawing.Color.White;
            lastUsedListView.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            lastUsedListView.OwnerDraw = true;
            lastUsedListView.DrawItem += LastUsedListView_DrawItem;
            lastUsedListView.SelectedIndexChanged += LastUsedListView_SelectedIndexChanged;
            Controls.Add(lastUsedListView);

            this.ResumeLayout(false);
            this.PerformLayout();
            this.Resize += MainForm_Resize;
        }

        private void LastUsedListView_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            e.DrawBackground();
            e.DrawText();
        }

        private void LastUsedListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lastUsedListView.SelectedItems.Count > 0)
            {
                string selectedFolderPath = lastUsedListView.SelectedItems[0].Text;
                autoLoadButton_Click(this, EventArgs.Empty);
            }
        }

        private void autoLoadButton_Click(object sender, EventArgs e)
        {
            string lastUsedFolderFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "LastUsedFolder.txt");

            if (File.Exists(lastUsedFolderFile))
            {
                string lastUsedFolder = File.ReadAllText(lastUsedFolderFile);

                if (Directory.Exists(lastUsedFolder))
                {
                    string downloadFolderPath = Path.Combine(lastUsedFolder, "Download");
                    Directory.CreateDirectory(downloadFolderPath);

                    string[] exeFiles = Directory.GetFiles(downloadFolderPath, "*.exe");
                    if (exeFiles.Length > 0)
                    {
                        string exeFilePath = exeFiles[0];
                        ProcessStartInfo startInfo = new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = $"/C \"{exeFilePath}\"",
                            WorkingDirectory = downloadFolderPath,
                            Verb = "runas"
                        };
                        Process.Start(startInfo);
                    }
                    else
                    {
                        MessageBox.Show("No executable file (.exe) found in the download folder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("The last used folder no longer exists.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("No last used folder found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadFolder(string folderPath)
        {
            string folderName = Path.GetFileName(folderPath);
            string manifestPath = Path.Combine(folderPath, "manifest.json");
            string iconPath = Path.Combine(folderPath, "icon.png");

            if (File.Exists(manifestPath) && File.Exists(iconPath))
            {
                Image gameIcon = Image.FromFile(iconPath);

                ListViewItem listViewItem = new ListViewItem();
                listViewItem.Text = folderName;
                listViewItem.ImageIndex = gameIconList.Images.Count;
                gameIconList.Images.Add(gameIcon);

                gamesListView.Items.Add(listViewItem);
            }
        }

        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

        private void GamesListView_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            foreach (ListViewItem item in gamesListView.Items)
            {
                item.BackColor = gamesListView.BackColor;
                item.ForeColor = gamesListView.ForeColor;
            }

            if (e.IsSelected)
            {
                e.Item.BackColor = SystemColors.Highlight;
                e.Item.ForeColor = SystemColors.HighlightText;

                GameItem selectedGame = (GameItem)e.Item.Tag;
            }
            else
            {
                e.Item.BackColor = gamesListView.BackColor;
                e.Item.ForeColor = gamesListView.ForeColor;
            }
        }

        private void InitializeGamesListView()
        {
            gamesListView.Items.Clear();
            gameIconList.Images.Clear();

            if (!Directory.Exists(GamesFolderPath))
                Directory.CreateDirectory(GamesFolderPath);

            string[] gameFolders = Directory.GetDirectories(GamesFolderPath);

            gamesListView.View = View.LargeIcon;

            gamesListView.LargeImageList = gameIconList;

            gamesListView.ItemSelectionChanged += GamesListView_ItemSelectionChanged;

            foreach (string folderPath in gameFolders)
            {
                string folderName = Path.GetFileName(folderPath);
                string manifestPath = Path.Combine(folderPath, "manifest.json");
                string iconPath = Path.Combine(folderPath, "icon.png");

                if (!File.Exists(manifestPath) || !File.Exists(iconPath))
                    continue;

                string manifestContent = File.ReadAllText(manifestPath);

                Image gameIcon = Image.FromFile(iconPath);
                gameIconList.Images.Add(gameIcon);

                GameItem gameItem = new GameItem();
                gameItem.Name = folderName;

                ListViewItem listViewItem = new ListViewItem();
                listViewItem.Text = folderName;
                listViewItem.ImageIndex = gameIconList.Images.Count - 1;
                listViewItem.Tag = gameItem;

                gamesListView.Items.Add(listViewItem);
            }
        }

        private void GamesListView_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            e.DrawBackground();
            e.DrawText();
            if (e.Item.ImageIndex >= 0)
            {
                int imageX = e.Item.Position.X + 5;
                int imageY = e.Item.Position.Y + 5;
                e.Graphics.DrawImage(gameIconList.Images[e.Item.ImageIndex], imageX, imageY);
            }
        }
    }

    public class CreateGameForm : Form
    {
        public string GameName { get; private set; }
        public string SelectedFolder { get; private set; }

        private TextBox gameNameTextBox;
        private Button browseButton;
        private Label selectedFolderLabel;

        public CreateGameForm()
        {
            InitializeComponent();
        }

        private void CreateButton_Click(object sender, EventArgs e)
        {
            GameName = gameNameTextBox.Text;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                DialogResult result = folderDialog.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(folderDialog.SelectedPath))
                {
                    SelectedFolder = folderDialog.SelectedPath;
                    selectedFolderLabel.Text = $"Selected Folder: {SelectedFolder}";
                }
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
            this.ClientSize = new System.Drawing.Size(300, 150);
            this.ForeColor = System.Drawing.Color.White;
            this.Name = "CreateGameForm";
            this.Text = "Create Game";

            gameNameTextBox = new TextBox();
            gameNameTextBox.Location = new System.Drawing.Point(10, 10);
            gameNameTextBox.Size = new System.Drawing.Size(280, 23);
            gameNameTextBox.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
            gameNameTextBox.ForeColor = System.Drawing.Color.White;
            gameNameTextBox.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Controls.Add(gameNameTextBox);

            // Configure browseButton
            browseButton = new Button();
            browseButton.Location = new System.Drawing.Point(10, 40);
            browseButton.Size = new System.Drawing.Size(100, 23);
            browseButton.Text = "Browse";
            browseButton.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
            browseButton.ForeColor = System.Drawing.Color.White;
            browseButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            browseButton.Click += BrowseButton_Click;
            this.Controls.Add(browseButton);

            // Configure selectedFolderLabel
            selectedFolderLabel = new Label();
            selectedFolderLabel.Location = new System.Drawing.Point(10, 70);
            selectedFolderLabel.Size = new System.Drawing.Size(280, 23);
            selectedFolderLabel.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
            selectedFolderLabel.ForeColor = System.Drawing.Color.White;
            selectedFolderLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Controls.Add(selectedFolderLabel);

            // Configure createButton
            Button createButton = new Button();
            createButton.Location = new System.Drawing.Point(10, 100);
            createButton.Size = new System.Drawing.Size(75, 23);
            createButton.Text = "Create";
            createButton.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
            createButton.ForeColor = System.Drawing.Color.White;
            createButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            createButton.Click += CreateButton_Click;
            this.Controls.Add(createButton);

            this.ResumeLayout(false);
        }
    }

    public class GameListViewItem : ListViewItem
    {
        public int IconIndex { get; set; }
    }

    public class GameItem
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int ImageIndex { get; set; }
        public int IconIndex { get; set; }
    }

    public static class Program
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
