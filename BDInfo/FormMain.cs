//============================================================================
// BDInfo - Blu-ray Video and Audio Analysis Tool
// Copyright © 2010 Cinema Squid
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//=============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace BDInfo
{
    public partial class FormMain : Form
    {
        private BDROM BDROM = null;
        private int CustomPlaylistCount = 0;
        ScanBDROMResult ScanResult = new ScanBDROMResult();

        #region UI Handlers

        private ListViewColumnSorter PlaylistColumnSorter;

        public static Control FindFocusedControl(Control control)
        {
            var container = control as IContainerControl;
            while (container != null)
            {
                control = container.ActiveControl;
                container = control as IContainerControl;
            }
            return control;
        }

        public FormMain(string[] args)
        {
            InitializeComponent();

            PlaylistColumnSorter = new ListViewColumnSorter();
            listViewPlaylistFiles.ListViewItemSorter = PlaylistColumnSorter;
            if (args.Length > 0)
            {
                string path = args[0];
                textBoxSource.Text = path;
                InitBDROM(path);
            }
            else
            {
                textBoxSource.Text = BDInfoSettings.LastPath;
            }
            this.Icon = BDInfo.Properties.Resources.Bluray_disc;

            Text += String.Format(" v{0}", Application.ProductVersion);

            Size = BDInfo.Properties.Settings.Default.WindowSize;
            Location = BDInfo.Properties.Settings.Default.WindowLocation;
            WindowState = BDInfo.Properties.Settings.Default.WindowState;
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            ResetColumnWidths();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.C))
            {
                Control focusedControl = FindFocusedControl(this);

                Clipboard.Clear();

                if (focusedControl == listViewPlaylistFiles && listViewPlaylistFiles.SelectedItems.Count > 0)
                {
                    ListViewItem playlistItem = listViewPlaylistFiles.SelectedItems[0];
                    if (playlistItem != null)
                    {
                        TSPlaylistFile playlist = null;
                        string playlistFileName = playlistItem.Text;
                        if (BDROM.PlaylistFiles.ContainsKey(playlistFileName))
                        {
                            playlist = BDROM.PlaylistFiles[playlistFileName];
                        }
                        if (playlist != null)
                          Clipboard.SetText(playlist.GetFilePath());
                    }
                }
                if (focusedControl == listViewStreamFiles && listViewStreamFiles.SelectedItems.Count > 0)
                {
                    ListViewItem streamFileItem = listViewStreamFiles.SelectedItems[0];
                    if (streamFileItem != null)
                    {
                        TSStreamFile streamFile = null;
                        string streamFileName = streamFileItem.Text;
                        if (BDROM.StreamFiles.ContainsKey(streamFileName))
                        {
                            streamFile = BDROM.StreamFiles[streamFileName];
                        }
                        if (streamFile != null)
                            Clipboard.SetText(streamFile.GetFilePath());
                    }
                }
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void textBoxSource_TextChanged(object sender, EventArgs e)
        {
            if (textBoxSource.Text.Length > 0)
            {
                buttonRescan.Enabled = true;
            }
            else
            {
                buttonRescan.Enabled = false;
            }
        }

        private void FormMain_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.All;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void FormMain_DragDrop(object sender, DragEventArgs e)
        {
            string[] sources = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            if (sources.Length > 0)
            {
                string path = sources[0];
                textBoxSource.Text = path;
                InitBDROM(path);
            }
        }

        private void buttonBrowse_Click(
            object sender,
            EventArgs e)
        {
            string path = null;
            try
            {
                CommonOpenFileDialog openDialog = new CommonOpenFileDialog();
                if (((Button) sender).Name == "buttonBrowse")
                {
                    openDialog.IsFolderPicker = true;
                    openDialog.Title = "Select a BluRay BDMV Folder:";
                }
                else
                {
                    openDialog.IsFolderPicker = false;
                    openDialog.Title = "Select a BluRay .ISO file:";
                    openDialog.Filters.Add(new CommonFileDialogFilter("ISO-Image", ".iso"));
                }


                if (!string.IsNullOrEmpty(textBoxSource.Text))
                {
                    openDialog.InitialDirectory = textBoxSource.Text;
                }
                if (openDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    path = openDialog.FileName;
                    textBoxSource.Text = path;
                    InitBDROM(path);
                }
            }
            catch (Exception ex)
            {
                string msg = string.Format(CultureInfo.InvariantCulture,
                    "Error opening path {0}: {1}{2}",
                    path,
                    ex.Message,
                    Environment.NewLine);

                MessageBox.Show(msg, "BDInfo Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void buttonRescan_Click(object sender, EventArgs e)
        {
            string path = textBoxSource.Text;
            try
            {
                InitBDROM(path);
            }
            catch (Exception ex)
            {
                string msg = string.Format(CultureInfo.InvariantCulture,
                    "Error opening path {0}: {1}{2}",
                    path,
                    ex.Message,
                    Environment.NewLine);

                MessageBox.Show(msg, "BDInfo Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void buttonSettings_Click(
            object sender,
            EventArgs e)
        {
            FormSettings settings = new FormSettings();
            settings.ShowDialog();
        }

        private void buttonSelectAll_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listViewPlaylistFiles.Items)
            {
                item.Checked = true;
            }
        }

        private void buttonUnselectAll_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listViewPlaylistFiles.Items)
            {
                item.Checked = false;
            }
        }

        private void buttonCustomPlaylist_Click(
            object sender,
            EventArgs e)
        {
            string name = string.Format(CultureInfo.InvariantCulture,
                "USER.{0}", (++CustomPlaylistCount).ToString("D3", CultureInfo.InvariantCulture));

            FormPlaylist form = new FormPlaylist(name, BDROM, OnCustomPlaylistAdded);
            form.LoadPlaylists();
            form.Show();
        }

        public void OnCustomPlaylistAdded()
        {
            LoadPlaylists();
        }

        private void buttonScan_Click(
            object sender,
            EventArgs e)
        {
            ScanBDROM();
        }

        private void buttonViewReport_Click(
            object sender,
            EventArgs e)
        {
            GenerateReport();
        }

        private void listViewPlaylistFiles_SelectedIndexChanged(
            object sender,
            EventArgs e)
        {
            LoadPlaylist();
        }

        private void listViewPlaylistFiles_ColumnClick(
            object sender,
            ColumnClickEventArgs e)
        {
            if (e.Column == PlaylistColumnSorter.SortColumn)
            {
                if (PlaylistColumnSorter.Order == SortOrder.Ascending)
                {
                    PlaylistColumnSorter.Order = SortOrder.Descending;
                }
                else
                {
                    PlaylistColumnSorter.Order = SortOrder.Ascending;
                }
            }
            else
            {
                PlaylistColumnSorter.SortColumn = e.Column;
                PlaylistColumnSorter.Order = SortOrder.Ascending;
            }
            listViewPlaylistFiles.Sort();
        }

        private void ResetColumnWidths()
        {
            listViewPlaylistFiles.Columns[0].Width =
                (int)(listViewPlaylistFiles.ClientSize.Width * 0.30);
            listViewPlaylistFiles.Columns[1].Width =
                (int)(listViewPlaylistFiles.ClientSize.Width * 0.07);
            listViewPlaylistFiles.Columns[2].Width =
                (int)(listViewPlaylistFiles.ClientSize.Width * 0.21);
            listViewPlaylistFiles.Columns[3].Width =
                (int)(listViewPlaylistFiles.ClientSize.Width * 0.21);
            listViewPlaylistFiles.Columns[4].Width =
                (int)(listViewPlaylistFiles.ClientSize.Width * 0.21);

            listViewStreamFiles.Columns[0].Width =
                (int)(listViewStreamFiles.ClientSize.Width * 0.23);
            listViewStreamFiles.Columns[1].Width =
                (int)(listViewStreamFiles.ClientSize.Width * 0.08);
            listViewStreamFiles.Columns[2].Width =
                (int)(listViewStreamFiles.ClientSize.Width * 0.23);
            listViewStreamFiles.Columns[3].Width =
                (int)(listViewStreamFiles.ClientSize.Width * 0.23);
            listViewStreamFiles.Columns[4].Width =
                (int)(listViewStreamFiles.ClientSize.Width * 0.23);

            listViewStreams.Columns[0].Width =
                (int)(listViewStreams.ClientSize.Width * 0.22);
            listViewStreams.Columns[1].Width =
                (int)(listViewStreams.ClientSize.Width * 0.10);
            listViewStreams.Columns[2].Width =
                (int)(listViewStreams.ClientSize.Width * 0.10);
            listViewStreams.Columns[3].Width =
                (int)(listViewStreams.ClientSize.Width * 0.58);
        }

        private void FormMain_FormClosing(
            object sender,
            FormClosingEventArgs e)
        {
            BDInfoSettings.LastPath = textBoxSource.Text;
            BDInfo.Properties.Settings.Default.WindowState = WindowState;

            if (WindowState == FormWindowState.Normal)
            {
                BDInfo.Properties.Settings.Default.WindowSize = Size;
                BDInfo.Properties.Settings.Default.WindowLocation = Location;
            }
            else
            {
                BDInfo.Properties.Settings.Default.WindowSize = RestoreBounds.Size;
                BDInfo.Properties.Settings.Default.WindowLocation = RestoreBounds.Location;
            }


            BDInfoSettings.SaveSettings();

            if (InitBDROMWorker != null &&
                InitBDROMWorker.IsBusy)
            {
                InitBDROMWorker.CancelAsync();
            }
            if (ScanBDROMWorker != null &&
                ScanBDROMWorker.IsBusy)
            {
                ScanBDROMWorker.CancelAsync();
            }
            if (ReportWorker != null &&
                ReportWorker.IsBusy)
            {
                ReportWorker.CancelAsync();
            }
        }

        #endregion

        #region BDROM Initialization Worker

        private BackgroundWorker InitBDROMWorker = null;

        private void InitBDROM(
            string path)
        {
            ShowNotification("Please wait while we scan the disc...");

            CustomPlaylistCount = 0;
            buttonBrowse.Enabled = false;
            buttonIsoBrowse.Enabled = false;
            buttonRescan.Enabled = false;
            buttonSelectAll.Enabled = false;
            buttonUnselectAll.Enabled = false;
            buttonCustomPlaylist.Enabled = false;
            buttonScan.Enabled = false;
            buttonViewReport.Enabled = false;
            textBoxDetails.Enabled = false;
            listViewPlaylistFiles.Enabled = false;
            listViewStreamFiles.Enabled = false;
            listViewStreams.Enabled = false;
            textBoxDetails.Clear();
            listViewPlaylistFiles.Items.Clear();
            listViewStreamFiles.Items.Clear();
            listViewStreams.Items.Clear();

            if (BDROM != null && BDROM.IsImage && BDROM.CdReader != null)
                BDROM.CloseDiscImage();

            InitBDROMWorker = new BackgroundWorker();
            InitBDROMWorker.WorkerReportsProgress = true;
            InitBDROMWorker.WorkerSupportsCancellation = true;
            InitBDROMWorker.DoWork += InitBDROMWork;
            InitBDROMWorker.ProgressChanged += InitBDROMProgress;
            InitBDROMWorker.RunWorkerCompleted += InitBDROMCompleted;
            InitBDROMWorker.RunWorkerAsync(path);
        }

        public void InitBDROMWork(
            object sender,
            DoWorkEventArgs e)
        {
            try
            {
                BDROM = new BDROM((string)e.Argument);
                BDROM.StreamClipFileScanError += new BDROM.OnStreamClipFileScanError(BDROM_StreamClipFileScanError);
                BDROM.StreamFileScanError += new BDROM.OnStreamFileScanError(BDROM_StreamFileScanError);
                BDROM.PlaylistFileScanError += new BDROM.OnPlaylistFileScanError(BDROM_PlaylistFileScanError);
                BDROM.Scan();
                e.Result = null;
            }
            catch (Exception ex)
            {
                e.Result = ex;
            }
        }

        protected bool BDROM_PlaylistFileScanError(TSPlaylistFile playlistFile, Exception ex)
        {
            DialogResult result = MessageBox.Show(string.Format(CultureInfo.InvariantCulture,
                "An error occurred while scanning the playlist file {0}.\n\nThe disc may be copy-protected or damaged.\n\nDo you want to continue scanning the playlist files?", playlistFile.Name),
                "BDInfo Scan Error", MessageBoxButtons.YesNo);

            if (result == DialogResult.Yes) return true;
            else return false;
        }

        protected bool BDROM_StreamFileScanError(TSStreamFile streamFile, Exception ex)
        {
            DialogResult result = MessageBox.Show(string.Format(CultureInfo.InvariantCulture,
                "An error occurred while scanning the stream file {0}.\n\nThe disc may be copy-protected or damaged.\n\nDo you want to continue scanning the stream files?", streamFile.Name),
                "BDInfo Scan Error", MessageBoxButtons.YesNo);

            if (result == DialogResult.Yes) return true;
            else return false;
        }

        protected bool BDROM_StreamClipFileScanError(TSStreamClipFile streamClipFile, Exception ex)
        {
            DialogResult result = MessageBox.Show(string.Format(CultureInfo.InvariantCulture,
                "An error occurred while scanning the stream clip file {0}.\n\nThe disc may be copy-protected or damaged.\n\nDo you want to continue scanning the stream clip files?", streamClipFile.Name),
                "BDInfo Scan Error", MessageBoxButtons.YesNo);

            if (result == DialogResult.Yes) return true;
            else return false;
        }

        private void InitBDROMProgress(
            object sender,
            ProgressChangedEventArgs e)
        {
        }

        private void InitBDROMCompleted(
            object sender,
            RunWorkerCompletedEventArgs e)
        {
            HideNotification();

            if (e.Result != null)
            {
                string msg = string.Format(CultureInfo.InvariantCulture,
                                            "{0}", ((Exception)e.Result).Message);

                MessageBox.Show(msg, "BDInfo Error",
                                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                buttonBrowse.Enabled = true;
                buttonIsoBrowse.Enabled = true;
                buttonRescan.Enabled = true;
                return;
            }

            buttonBrowse.Enabled = true;
            buttonIsoBrowse.Enabled = true;
            buttonRescan.Enabled = true;
            buttonScan.Enabled = true;
            buttonSelectAll.Enabled = true;
            buttonUnselectAll.Enabled = true;
            buttonCustomPlaylist.Enabled = true;
            buttonViewReport.Enabled = true;
            textBoxDetails.Enabled = true;
            listViewPlaylistFiles.Enabled = true;
            listViewStreamFiles.Enabled = true;
            listViewStreams.Enabled = true;
            progressBarScan.Value = 0;
            labelProgress.Text = "";
            labelTimeElapsed.Text = "00:00:00";
            labelTimeRemaining.Text = "00:00:00";

            if (!string.IsNullOrEmpty(BDROM.DiscTitle))
            {
                textBoxDetails.Text += string.Format(CultureInfo.InvariantCulture,
                                                    "Disc Title: {0}{1}",
                                                    BDROM.DiscTitle,
                                                    Environment.NewLine);
            }

            if (!BDROM.IsImage)
            {
                textBoxSource.Text = BDROM.DirectoryRoot.FullName;
                textBoxDetails.Text += string.Format(CultureInfo.InvariantCulture,
                                                    "Detected BDMV Folder: {0} (Disc Label: {1}){2}",
                                                    BDROM.DirectoryBDMV.FullName,
                                                    BDROM.VolumeLabel,
                                                    Environment.NewLine);
            }
            else
            {
                textBoxDetails.Text += string.Format(CultureInfo.InvariantCulture,
                                                    "Detected BDMV Folder: {0} (Disc Label: {1}){3}ISO Image: {2}{3}",
                                                    BDROM.DiscDirectoryBDMV.FullName,
                                                    BDROM.VolumeLabel,
                                                    textBoxSource.Text,
                                                    Environment.NewLine);
            }

            List<string> features = new List<string>();
            if (BDROM.IsUHD)
            {
                features.Add("Ultra HD");
            }
            if (BDROM.Is50Hz)
            {
                features.Add("50Hz Content");
            }
            if (BDROM.IsBDPlus)
            {
                features.Add("BD+ Copy Protection");
            }
            if (BDROM.IsBDJava)
            {
                features.Add("BD-Java");
            }
            if (BDROM.Is3D)
            {
                features.Add("Blu-ray 3D");
            }
            if (BDROM.IsDBOX)
            {
                features.Add("D-BOX Motion Code");
            }
            if (BDROM.IsPSP)
            {
                features.Add("PSP Digital Copy");
            }
            if (features.Count > 0)
            {
                textBoxDetails.Text += "Detected Features: " + string.Join(", ", features.ToArray()) + Environment.NewLine;
            }

            textBoxDetails.Text += string.Format(CultureInfo.InvariantCulture,
                                                "Disc Size: {0:N0} bytes ({1}){2}",
                                                BDROM.Size,
                                                ToolBox.FormatFileSize(BDROM.Size),
                                                Environment.NewLine);

            LoadPlaylists();
        }

        #endregion

        #region File/Stream Lists

        List<TSPlaylistFile> selectedPlaylists = new List<TSPlaylistFile>();

        public void GenerateReportCLI(String savePath)
        {
            if (ScanResult.ScanException != null)
            {
                System.Console.WriteLine(string.Format("{0}", ScanResult.ScanException.Message));
            }
            else
            {
                if (ScanResult.FileExceptions.Count > 0)
                {
                    System.Console.WriteLine("Scan completed with errors (see report).");
                }
                else
                {
                    System.Console.WriteLine("Scan completed successfully.");
                }

                System.Console.WriteLine("Please wait while we generate the report...");
                try                                                                                                                            {
                    FormReport report = (FormReport)FormatterServices.GetUninitializedObject(typeof(FormReport));
                    report.Generate(BDROM, selectedPlaylists, ScanResult, savePath);
                }
                catch (Exception ex)
                {                                                                                                                                  System.Console.WriteLine(string.Format("{0}", (ex.Message)));
                }                                                                                                                          }
        }

        /* XXX: returns -1 on 'q' input */
        private static int getIntIndex(int min, int max)
        {
            String response;
            int resp = -1;
            do
            {
                while (Console.KeyAvailable)
                    Console.ReadKey();

                Console.Write("Select (q when finished): ");
                response = Console.ReadLine();
                if (response == "q")
                    return -1;

                try
                {
                    resp = int.Parse(response);
                }
                catch (Exception)
                {
                    Console.WriteLine("Invalid Input!");
                }

                if (resp > max || resp < min)
                {
                    System.Console.WriteLine("Invalid Selection!");
                }
            } while (resp > max || resp < min);

            System.Console.WriteLine();

            return resp;
        }

        public void LoadPlaylists(List<String> inputPlaylists)
        {
            selectedPlaylists = new List<TSPlaylistFile>();
            foreach (String playlistName in inputPlaylists)
            {
                String Name = playlistName.ToUpper();
                if (BDROM.PlaylistFiles.ContainsKey(Name))
                {
                    if (!selectedPlaylists.Contains(BDROM.PlaylistFiles[Name]))
                    {
                        selectedPlaylists.Add(BDROM.PlaylistFiles[Name]);
                    }
                }
            }

            // throw error if no playlist is found
            if (selectedPlaylists.Count == 0)
            {
                throw new Exception("No matching playlists found on BD");
            }
        }


        public void LoadPlaylists(bool wholeDisc = false)
        {
#if false
            listViewPlaylistFiles.Items.Clear();
            listViewStreamFiles.Items.Clear();
            listViewStreams.Items.Clear();
#endif

            selectedPlaylists = new List<TSPlaylistFile>();

            if (BDROM == null) return;

            bool hasHiddenTracks = false;

            //Dictionary<string, int> playlistGroup = new Dictionary<string, int>();
            List<List<TSPlaylistFile>> groups = new List<List<TSPlaylistFile>>();

            TSPlaylistFile[] sortedPlaylistFiles = new TSPlaylistFile[BDROM.PlaylistFiles.Count];
            BDROM.PlaylistFiles.Values.CopyTo(sortedPlaylistFiles, 0);
            Array.Sort(sortedPlaylistFiles, ComparePlaylistFiles);

            foreach (TSPlaylistFile playlist1
                in sortedPlaylistFiles)
            {
                if (!playlist1.IsValid) continue;

                int matchingGroupIndex = 0;
                for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
                {
                    List<TSPlaylistFile> group = groups[groupIndex];
                    foreach (TSPlaylistFile playlist2 in group)
                    {
                        if (!playlist2.IsValid) continue;

                        foreach (TSStreamClip clip1 in playlist1.StreamClips)
                        {
                            foreach (TSStreamClip clip2 in playlist2.StreamClips)
                            {
                                if (clip1.Name == clip2.Name)
                                {
                                    matchingGroupIndex = groupIndex + 1;
                                    break;
                                }
                            }
                            if (matchingGroupIndex > 0) break;
                        }
                        if (matchingGroupIndex > 0) break;
                    }
                    if (matchingGroupIndex > 0) break;
                }
                if (matchingGroupIndex > 0)
                {
                    groups[matchingGroupIndex - 1].Add(playlist1);
                }
                else
                {
                    groups.Add(new List<TSPlaylistFile> { playlist1 });
                    //matchingGroupIndex = groups.Count;
                }
                //playlistGroup[playlist1.Name] = matchingGroupIndex;
            }

            System.Console.WriteLine(String.Format("{0,-4}{1,-7}{2,-15}{3,-10}{4,-16}{5,-16}\n", "#", "Group", "Playlist File", "Length", "Estimated Bytes", "Measured Bytes"));
            int playlistIdx = 1;
            Dictionary<int,TSPlaylistFile> playlistDict = new Dictionary<int, TSPlaylistFile>();

            for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
            {
                List<TSPlaylistFile> group = groups[groupIndex];
                group.Sort(ComparePlaylistFiles);

                foreach (TSPlaylistFile playlist in group)
                    //in BDROM.PlaylistFiles.Values)
                {
                    if (!playlist.IsValid) continue;

                    playlistDict[playlistIdx] = playlist;
                    if (wholeDisc)
                        selectedPlaylists.Add(playlist);

                    if (playlist.HasHiddenTracks)
                    {
                        hasHiddenTracks = true;
                    }
#if false
                    ListViewItem.ListViewSubItem playlistIndex =
                        new ListViewItem.ListViewSubItem();
                    playlistIndex.Text = (groupIndex + 1).ToString(CultureInfo.InvariantCulture);
                    playlistIndex.Tag = groupIndex;

                    ListViewItem.ListViewSubItem playlistName =
                        new ListViewItem.ListViewSubItem();
                    playlistName.Text = playlist.Name;
                    playlistName.Tag = playlist.Name;

                    if (playlist.Chapters != null && playlist.Chapters.Count > 1 && BDInfoSettings.DisplayChapterCount)
                        playlistName.Text += string.Format(CultureInfo.InvariantCulture,
                            " [{0:D2} Chapters]",
                            playlist.Chapters.Count);

                    TimeSpan playlistLengthSpan =
                        new TimeSpan((long)(playlist.TotalLength * 10000000));
                    ListViewItem.ListViewSubItem playlistLength =
                        new ListViewItem.ListViewSubItem();
                    playlistLength.Text = string.Format(CultureInfo.InvariantCulture,
                        "{0:D2}:{1:D2}:{2:D2}",
                        playlistLengthSpan.Hours,
                        playlistLengthSpan.Minutes,
                        playlistLengthSpan.Seconds);
                    playlistLength.Tag = playlist.TotalLength;

                    ListViewItem.ListViewSubItem playlistSize =
                        new ListViewItem.ListViewSubItem();
                    if (BDInfoSettings.EnableSSIF &&
                        playlist.InterleavedFileSize > 0)
                    {
                        playlistSize.Text = ToolBox.FormatFileSize(playlist.InterleavedFileSize);
                        playlistSize.Tag = playlist.InterleavedFileSize;
                    }
                    else if (playlist.FileSize > 0)
                    {
                        playlistSize.Text = ToolBox.FormatFileSize(playlist.FileSize);
                        playlistSize.Tag = playlist.FileSize;
                    }
                    else
                    {
                        playlistSize.Text = "-";
                        playlistSize.Tag = playlist.FileSize;
                    }

                    ListViewItem.ListViewSubItem playlistSize2 =
                        new ListViewItem.ListViewSubItem();
                    if (playlist.TotalAngleSize > 0)
                    {
                        playlistSize2.Text = ToolBox.FormatFileSize(playlist.TotalAngleSize);
                    }
                    else
                    {
                        playlistSize2.Text = "-";
                    }
                    playlistSize2.Tag = playlist.TotalAngleSize;

                    ListViewItem.ListViewSubItem[] playlistSubItems =
                        new ListViewItem.ListViewSubItem[]
                        {
                            playlistName,
                            playlistIndex,
                            playlistLength,
                            playlistSize,
                            playlistSize2
                        };

                    ListViewItem playlistItem =
                        new ListViewItem(playlistSubItems, 0);
                    listViewPlaylistFiles.Items.Add(playlistItem);
#else
                    String groupString = (groupIndex + 1).ToString();

                    TimeSpan playlistLengthSpan = new TimeSpan((long)(playlist.TotalLength * 10000000));
                    String length = string.Format(
                        "{0:D2}:{1:D2}:{2:D2}",
                        playlistLengthSpan.Hours,
                        playlistLengthSpan.Minutes,
                        playlistLengthSpan.Seconds);

                    String fileSize;
                    if (BDInfoSettings.EnableSSIF &&
                        playlist.InterleavedFileSize > 0)
                    {
                        fileSize = playlist.InterleavedFileSize.ToString("N0");
                    }
                    else if (playlist.FileSize > 0)
                    {
                        fileSize = playlist.FileSize.ToString("N0");
                    }
                    else
                    {
                        fileSize = "-";
                    }

                    String fileSize2;
                    if (playlist.TotalAngleSize > 0)
                    {
                        fileSize2 = (playlist.TotalAngleSize).ToString("N0");
                    }
                    else
                    {
                        fileSize2 = "-";
                    }

                    System.Console.WriteLine(String.Format("{0,-4:G}{1,-7}{2,-15}{3,-10}{4,-16}{5,-16}", playlistIdx.ToString(), groupString, playlist.Name, length, fileSize, fileSize2));
                    playlistIdx++;
#endif
                }
            }

            if (hasHiddenTracks)
            {
                System.Console.WriteLine("(*) Some playlists on this disc have hidden tracks. These tracks are marked with an asterisk.");
            }
            if (wholeDisc)
                return;

            for (int selectedIdx; (selectedIdx = getIntIndex(1, playlistIdx - 1)) > 0; ) {
                selectedPlaylists.Add(playlistDict[selectedIdx]);
                System.Console.WriteLine(String.Format("Added {0}", selectedIdx));
            }
            if (selectedPlaylists.Count == 0) {
                System.Console.WriteLine("No playlists selected. Exiting.");
                System.Environment.Exit(0);
            }

#if false
            if (listViewPlaylistFiles.Items.Count > 0)
            {
                listViewPlaylistFiles.Items[0].Selected = true;
            }
            ResetColumnWidths();
#endif
        }

        private void LoadPlaylist()
        {
            listViewStreamFiles.Items.Clear();
            listViewStreams.Items.Clear();

            if (BDROM == null) return;
            if (listViewPlaylistFiles.SelectedItems.Count == 0) return;

            ListViewItem playlistItem = listViewPlaylistFiles.SelectedItems[0];
            if (playlistItem == null) return;

            TSPlaylistFile playlist = null;
            string playlistFileName = (string)playlistItem.SubItems[0].Tag;
            if (BDROM.PlaylistFiles.ContainsKey(playlistFileName))
            {
                playlist = BDROM.PlaylistFiles[playlistFileName];
            }
            if (playlist == null) return;

            int clipCount = 0;
            foreach (TSStreamClip clip in playlist.StreamClips)
            {
                if (clip.AngleIndex == 0)
                {
                    ++clipCount;
                }

                ListViewItem.ListViewSubItem clipIndex =
                    new ListViewItem.ListViewSubItem
                    {
                        Text = clipCount.ToString(CultureInfo.InvariantCulture),
                        Tag = clipCount
                    };

                ListViewItem.ListViewSubItem clipName =
                    new ListViewItem.ListViewSubItem
                    {
                        Text = clip.DisplayName,
                        Tag = clip.Name
                    };
                if (clip.AngleIndex > 0)
                {
                    clipName.Text += string.Format(CultureInfo.InvariantCulture,
                        " ({0})", clip.AngleIndex);
                }

                TimeSpan clipLengthSpan =
                    new TimeSpan((long)(clip.Length * 10000000));

                ListViewItem.ListViewSubItem clipLength =
                    new ListViewItem.ListViewSubItem
                    {
                        Text = string.Format(CultureInfo.InvariantCulture,
                            "{0:D2}:{1:D2}:{2:D2}",
                            clipLengthSpan.Hours,
                            clipLengthSpan.Minutes,
                            clipLengthSpan.Seconds),
                        Tag = clip.Length
                    };

                ListViewItem.ListViewSubItem clipSize =
                    new ListViewItem.ListViewSubItem();
                if (BDInfoSettings.EnableSSIF &&
                    clip.InterleavedFileSize > 0)
                {
                    clipSize.Text = ToolBox.FormatFileSize(clip.InterleavedFileSize);
                    clipSize.Tag = clip.InterleavedFileSize;
                }
                else if (clip.FileSize > 0)
                {
                    clipSize.Text = ToolBox.FormatFileSize(clip.FileSize);
                    clipSize.Tag = clip.FileSize;
                }
                else
                {
                    clipSize.Text = "-";
                    clipSize.Tag = clip.FileSize;
                }

                ListViewItem.ListViewSubItem clipSize2 =
                    new ListViewItem.ListViewSubItem();
                if (clip.PacketSize > 0)
                {
                    clipSize2.Text = ToolBox.FormatFileSize(clip.PacketSize);
                }
                else
                {
                    clipSize2.Text = "-";
                }
                clipSize2.Tag = clip.PacketSize;

                ListViewItem.ListViewSubItem[] streamFileSubItems =
                    new ListViewItem.ListViewSubItem[]
                    {
                        clipName,
                        clipIndex,
                        clipLength,
                        clipSize,
                        clipSize2
                    };

                ListViewItem streamFileItem =
                    new ListViewItem(streamFileSubItems, 0);
                listViewStreamFiles.Items.Add(streamFileItem);
            }

            foreach (TSStream stream in playlist.SortedStreams)
            {
                ListViewItem.ListViewSubItem codec =
                    new ListViewItem.ListViewSubItem();
                codec.Text = stream.CodecName;
                if (stream.AngleIndex > 0)
                {
                    codec.Text += string.Format(CultureInfo.InvariantCulture,
                        " ({0})", stream.AngleIndex);
                }
                codec.Tag = stream.CodecName;

                if (stream.IsHidden)
                {
                    codec.Text = "* " + codec.Text;
                }

                ListViewItem.ListViewSubItem language =
                    new ListViewItem.ListViewSubItem
                    {
                        Text = stream.LanguageName,
                        Tag = stream.LanguageName
                    };

                ListViewItem.ListViewSubItem bitrate =
                    new ListViewItem.ListViewSubItem();

                if (stream.AngleIndex > 0)
                {
                    if (stream.ActiveBitRate > 0)
                    {
                        bitrate.Text = string.Format(CultureInfo.InvariantCulture,
                            "{0} kbps", Math.Round((double)stream.ActiveBitRate / 1000));
                    }
                    else
                    {
                        bitrate.Text = "-";
                    }
                    bitrate.Tag = stream.ActiveBitRate;
                }
                else
                {
                    if (stream.BitRate > 0)
                    {
                        bitrate.Text = string.Format(CultureInfo.InvariantCulture,
                            "{0} kbps", Math.Round((double)stream.BitRate / 1000));
                    }
                    else
                    {
                        bitrate.Text = "-";
                    }
                    bitrate.Tag = stream.BitRate;
                }

                ListViewItem.ListViewSubItem description =
                    new ListViewItem.ListViewSubItem();
                description.Text = stream.Description;
                description.Tag = stream.Description;

                ListViewItem.ListViewSubItem[] streamSubItems =
                    new ListViewItem.ListViewSubItem[]
                    {
                        codec,
                        language,
                        bitrate,
                        description
                    };

                ListViewItem streamItem =
                    new ListViewItem(streamSubItems, 0);
                streamItem.Tag = stream.PID;
                listViewStreams.Items.Add(streamItem);
            }

            ResetColumnWidths();
        }

        private void UpdatePlaylistBitrates()
        {
            foreach (ListViewItem item in listViewPlaylistFiles.Items)
            {
                string playlistName = (string)item.SubItems[0].Tag;
                if (BDROM.PlaylistFiles.ContainsKey(playlistName))
                {
                    TSPlaylistFile playlist =
                        BDROM.PlaylistFiles[playlistName];
                    item.SubItems[4].Text = ToolBox.FormatFileSize(playlist.TotalAngleSize);
                    item.SubItems[4].Tag = playlist.TotalAngleSize;
                }
            }

            if (listViewPlaylistFiles.SelectedItems.Count == 0)
            {
                return;
            }

            ListViewItem selectedPlaylistItem =
                listViewPlaylistFiles.SelectedItems[0];
            if (selectedPlaylistItem == null)
            {
                return;
            }

            string selectedPlaylistName = (string)selectedPlaylistItem.SubItems[0].Tag;
            TSPlaylistFile selectedPlaylist = null;
            if (BDROM.PlaylistFiles.ContainsKey(selectedPlaylistName))
            {
                selectedPlaylist = BDROM.PlaylistFiles[selectedPlaylistName];
            }
            if (selectedPlaylist == null)
            {
                return;
            }

            for (int i = 0; i < listViewStreamFiles.Items.Count; i++)
            {
                ListViewItem item = listViewStreamFiles.Items[i];
                if (selectedPlaylist.StreamClips.Count > i &&
                    selectedPlaylist.StreamClips[i].Name == (string)item.SubItems[0].Tag)
                {
                    item.SubItems[4].Text = ToolBox.FormatFileSize(selectedPlaylist.StreamClips[i].PacketSize);
                    item.Tag = selectedPlaylist.StreamClips[i].PacketSize;

                }
            }

            for (int i = 0; i < listViewStreams.Items.Count; i++)
            {
                ListViewItem item = listViewStreams.Items[i];
                if (i < selectedPlaylist.SortedStreams.Count &&
                    selectedPlaylist.SortedStreams[i].PID == (ushort)item.Tag)
                {
                    TSStream stream = selectedPlaylist.SortedStreams[i];
                    int kbps = 0;
                    if (stream.AngleIndex > 0)
                    {
                        kbps = (int)Math.Round((double)stream.ActiveBitRate / 1000);
                    }
                    else
                    {
                        kbps = (int)Math.Round((double)stream.BitRate / 1000);
                    }
                    item.SubItems[2].Text = string.Format(CultureInfo.InvariantCulture,
                        "{0} kbps", kbps);
                    item.SubItems[3].Text =
                        stream.Description;
                }
            }
        }

        #endregion

        #region Scan BDROM

        private BackgroundWorker ScanBDROMWorker = null;

        private class ScanBDROMState
        {
            public long TotalBytes = 0;
            public long FinishedBytes = 0;
            public DateTime TimeStarted = DateTime.Now;
            public TSStreamFile StreamFile = null;
            public Dictionary<string, List<TSPlaylistFile>> PlaylistMap =
                new Dictionary<string, List<TSPlaylistFile>>();
            public Exception Exception = null;
        }

        private void ScanBDROM()
        {
            if (ScanBDROMWorker != null &&
                ScanBDROMWorker.IsBusy)
            {
                ScanBDROMWorker.CancelAsync();
                return;
            }

            buttonScan.Text = "Cancel Scan";
            progressBarScan.Value = 0;
            progressBarScan.Minimum = 0;
            progressBarScan.Maximum = 100;
            labelProgress.Text = "Scanning disc...";
            labelTimeElapsed.Text = "00:00:00";
            labelTimeRemaining.Text = "00:00:00";
            buttonBrowse.Enabled = false;
            buttonIsoBrowse.Enabled = false;
            buttonRescan.Enabled = false;

            List<TSStreamFile> streamFiles = new List<TSStreamFile>();
            if (listViewPlaylistFiles.CheckedItems == null ||
                listViewPlaylistFiles.CheckedItems.Count == 0)
            {
                foreach (TSStreamFile streamFile
                    in BDROM.StreamFiles.Values)
                {
                    streamFiles.Add(streamFile);
                }
            }
            else
            {
                foreach (ListViewItem item
                    in listViewPlaylistFiles.CheckedItems)
                {
                    string playlistName = (string)item.SubItems[0].Tag;
                    if (BDROM.PlaylistFiles.ContainsKey(playlistName))
                    {
                        TSPlaylistFile playlist =
                            BDROM.PlaylistFiles[playlistName];

                        foreach (TSStreamClip clip
                            in playlist.StreamClips)
                        {
                            if (!streamFiles.Contains(clip.StreamFile))
                            {
                                streamFiles.Add(clip.StreamFile);
                            }
                        }
                    }
                }
            }

            ScanBDROMWorker = new BackgroundWorker();
            ScanBDROMWorker.WorkerReportsProgress = true;
            ScanBDROMWorker.WorkerSupportsCancellation = true;
            ScanBDROMWorker.DoWork += ScanBDROMWork;
//            ScanBDROMWorker.ProgressChanged += ScanBDROMProgress;
            ScanBDROMWorker.RunWorkerCompleted += ScanBDROMCompleted;
            ScanBDROMWorker.RunWorkerAsync(streamFiles);
        }

        public void ScanBDROMWork(
            object sender,
            DoWorkEventArgs e)
        {
            ScanResult = new ScanBDROMResult {ScanException = new Exception("Scan is still running.")};

            List<TSStreamFile> streamFiles = new List<TSStreamFile>();
            List<string> streamNames;
            System.Console.WriteLine("Preparing to analyze the following:");
            // Adapted from ScanBDROM()
            foreach (TSPlaylistFile playlist in selectedPlaylists)
            {
                System.Console.Write("{0} --> ", playlist.Name);
                streamNames = new List<string>();
                foreach (TSStreamClip clip in playlist.StreamClips)
                {
                    if (!streamFiles.Contains(clip.StreamFile))
                    {
                        streamNames.Add(clip.StreamFile.Name);
                        streamFiles.Add(clip.StreamFile);
                    }
                }
                Console.WriteLine(String.Join(" + ", streamNames));
            }

            System.Threading.Timer timer = null;
            try
            {
#if false
                List<TSStreamFile> streamFiles =
                    (List<TSStreamFile>)e.Argument;
#endif

                ScanBDROMState scanState = new ScanBDROMState();
                foreach (TSStreamFile streamFile in streamFiles)
                {
                    if (BDInfoSettings.EnableSSIF &&
                        streamFile.InterleavedFile != null)
                    {
                        if (streamFile.InterleavedFile.FileInfo != null)
                            scanState.TotalBytes += streamFile.InterleavedFile.FileInfo.Length;
                        else
                            scanState.TotalBytes += streamFile.InterleavedFile.DFileInfo.Length;
                    }
                    else
                    {
                        if (streamFile.FileInfo != null)
                            scanState.TotalBytes += streamFile.FileInfo.Length;
                        else
                            scanState.TotalBytes += streamFile.DFileInfo.Length;
                    }

                    if (!scanState.PlaylistMap.ContainsKey(streamFile.Name))
                    {
                        scanState.PlaylistMap[streamFile.Name] = new List<TSPlaylistFile>();
                    }

                    foreach (TSPlaylistFile playlist
                        in BDROM.PlaylistFiles.Values)
                    {
                        playlist.ClearBitrates();

                        foreach (TSStreamClip clip in playlist.StreamClips)
                        {
                            if (clip.Name == streamFile.Name)
                            {
                                if (!scanState.PlaylistMap[streamFile.Name].Contains(playlist))
                                {
                                    scanState.PlaylistMap[streamFile.Name].Add(playlist);
                                }
                            }
                        }
                    }
                }

                timer = new System.Threading.Timer(
                    ScanBDROMProgress, scanState, 1000, 1000);
		System.Console.WriteLine("\n{0,16}{1,-15}{2,-13}{3}","", "File", "Elapsed", "Remaining");

                foreach (TSStreamFile streamFile in streamFiles)
                {
                    scanState.StreamFile = streamFile;

                    Thread thread = new Thread(ScanBDROMThread);
                    thread.Start(scanState);
                    while (thread.IsAlive)
                    {
#if false
                        if (ScanBDROMWorker.CancellationPending)
                        {
                            ScanResult.ScanException = new Exception("Scan was cancelled.");
                            thread.Abort();
                            return;
                        }
#endif
                        Thread.Sleep(250);
                    }
                    if (streamFile.FileInfo != null)
                        scanState.FinishedBytes += streamFile.FileInfo.Length;
                    else
                        scanState.FinishedBytes += streamFile.DFileInfo.Length;
                    if (scanState.Exception != null)
                    {
                        ScanResult.FileExceptions[streamFile.Name] = scanState.Exception;
                    }
                }
                ScanResult.ScanException = null;
            }
            catch (Exception ex)
            {
                ScanResult.ScanException = ex;
            }
            finally
            {
                System.Console.WriteLine();
                timer?.Dispose();
            }
        }

        private void ScanBDROMThread(
            object parameter)
        {
            ScanBDROMState scanState = (ScanBDROMState)parameter;
            try
            {
                TSStreamFile streamFile = scanState.StreamFile;
                List<TSPlaylistFile> playlists = scanState.PlaylistMap[streamFile.Name];
                streamFile.Scan(playlists, true);
            }
            catch (Exception ex)
            {
                scanState.Exception = ex;
            }
        }

        private void ScanBDROMEvent(
            object state)
        {
            try
            {
                if (ScanBDROMWorker.IsBusy &&
                    !ScanBDROMWorker.CancellationPending)
                {
                    ScanBDROMWorker.ReportProgress(0, state);
                }
            }
            catch { }
        }

        private void ScanBDROMProgress(
            object state)
        {
            ScanBDROMState scanState = (ScanBDROMState)state;

            try
            {
#if false
                if (scanState.StreamFile != null)
                {
                    labelProgress.Text = string.Format(CultureInfo.InvariantCulture,
                        "Scanning {0}...\r\n",
                        scanState.StreamFile.DisplayName);
                }
#endif

                long finishedBytes = scanState.FinishedBytes;
                if (scanState.StreamFile != null)
                {
                    finishedBytes += scanState.StreamFile.Size;
                }

                double progress = ((double)finishedBytes / scanState.TotalBytes);
                int progressValue = (int)Math.Round(progress * 100);
                if (progressValue < 0) progressValue = 0;
                if (progressValue > 100) progressValue = 100;
//                progressBarScan.Value = progressValue;

                TimeSpan elapsedTime = DateTime.Now.Subtract(scanState.TimeStarted);
                TimeSpan remainingTime;
                if (progress > 0 && progress < 1)
                {
                    remainingTime = new TimeSpan(
                        (long)((double)elapsedTime.Ticks / progress) - elapsedTime.Ticks);
                }
                else
                {
                    remainingTime = new TimeSpan(0);
                }

                string elapsedTimeString = string.Format(CultureInfo.InvariantCulture,
                    "{0:D2}:{1:D2}:{2:D2}",
                    elapsedTime.Hours,
                    elapsedTime.Minutes,
                    elapsedTime.Seconds);

                string remainingTimeString = string.Format(CultureInfo.InvariantCulture,
                    "{0:D2}:{1:D2}:{2:D2}",
                    remainingTime.Hours,
                    remainingTime.Minutes,
                    remainingTime.Seconds);

                if (scanState.StreamFile != null)
                {
                    System.Console.Write("Scanning {0,3:d}% - {1,10} {2,12}  |  {3}\r", progressValue, scanState.StreamFile.DisplayName, elapsedTimeString, remainingTimeString);
                }
                else
                {
                    System.Console.Write("Scanning {0,3}% - \t{1,10}  |  {2}...\r", progressValue, elapsedTimeString, remainingTimeString);

                }

//                UpdatePlaylistBitrates();
            }
            catch { }
        }

        private void ScanBDROMCompleted(
            object sender,
            RunWorkerCompletedEventArgs e)
        {
            buttonScan.Enabled = false;

            UpdatePlaylistBitrates();

            labelProgress.Text = "Scan complete.";
            progressBarScan.Value = 100;
            labelTimeRemaining.Text = "00:00:00";

            if (ScanResult.ScanException != null)
            {
                string msg = string.Format(CultureInfo.InvariantCulture,
                    "{0}", ScanResult.ScanException.Message);

                MessageBox.Show(msg, "BDInfo Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                if (BDInfoSettings.AutosaveReport)
                {
                    GenerateReport();
                }
                else if (ScanResult.FileExceptions.Count > 0)
                {
                    MessageBox.Show(
                        "Scan completed with errors (see report).", "BDInfo Scan",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    MessageBox.Show(
                        "Scan completed successfully.", "BDInfo Scan",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            buttonBrowse.Enabled = true;
            buttonIsoBrowse.Enabled = true;
            buttonRescan.Enabled = true;
            buttonScan.Enabled = true;
            buttonScan.Text = "Scan Bitrates";
        }

        #endregion

        #region Report Generation

        private BackgroundWorker ReportWorker = null;

        private void GenerateReport()
        {
            ShowNotification("Please wait while we generate the report...");
            buttonViewReport.Enabled = false;

            List<TSPlaylistFile> playlists = new List<TSPlaylistFile>();
            if (listViewPlaylistFiles.CheckedItems == null ||
                listViewPlaylistFiles.CheckedItems.Count == 0)
            {
                foreach (ListViewItem item
                    in listViewPlaylistFiles.Items)
                {
                    if (BDROM.PlaylistFiles.ContainsKey(item.SubItems[0].Tag.ToString()))
                    {
                        playlists.Add(BDROM.PlaylistFiles[item.SubItems[0].Tag.ToString()]);
                    }
                }
            }
            else
            {
                foreach (ListViewItem item
                    in listViewPlaylistFiles.CheckedItems)
                {
                    if (BDROM.PlaylistFiles.ContainsKey(item.SubItems[0].Tag.ToString()))
                    {
                        playlists.Add(BDROM.PlaylistFiles[item.SubItems[0].Tag.ToString()]);
                    }
                }
            }

            ReportWorker = new BackgroundWorker();
            ReportWorker.WorkerReportsProgress = true;
            ReportWorker.WorkerSupportsCancellation = true;
            ReportWorker.DoWork += GenerateReportWork;
            ReportWorker.ProgressChanged += GenerateReportProgress;
            ReportWorker.RunWorkerCompleted += GenerateReportCompleted;
            ReportWorker.RunWorkerAsync(playlists);
        }

        private void GenerateReportWork(
            object sender,
            DoWorkEventArgs e)
        {
            try
            {
                List<TSPlaylistFile> playlists = (List<TSPlaylistFile>)e.Argument;
                FormReport report = new FormReport();
                report.Generate(BDROM, playlists, ScanResult);
                e.Result = report;
            }
            catch (Exception ex)
            {
                e.Result = ex;
            }
        }

        private void GenerateReportProgress(
            object sender,
            ProgressChangedEventArgs e)
        {
        }

        private void GenerateReportCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            HideNotification();
            if (e.Result != null)
            {
                if (e.Result.GetType().Name == "FormReport")
                {
                    ((Form)e.Result).Show();
                }
                else if (e.Result.GetType().Name == "Exception")
                {
                    string msg = string.Format(
                        "{0}", ((Exception)e.Result).Message);

                    MessageBox.Show(msg, "BDInfo Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            buttonViewReport.Enabled = true;
        }

        #endregion

        #region Notification Display

        private Form FormNotification = null;

        private void ShowNotification(
            string text)
        {
#if false
            HideNotification();

            Label label = new Label
            {
                AutoSize = true,
                Font = new Font(Font.SystemFontName, 12),
                Text = text
            };

            FormNotification = new Form
            {
                ControlBox = false,
                ShowInTaskbar = false,
                ShowIcon = false,
                FormBorderStyle = FormBorderStyle.Fixed3D
            };
            FormNotification.Controls.Add(label);
            FormNotification.Size = new Size(label.Width + 10, 18);
            FormNotification.Show(this);
            FormNotification.Location = new Point(
                this.Location.X + this.Width / 2 - FormNotification.Width / 2,
                this.Location.Y + this.Height / 2 - FormNotification.Height / 2);
#else
            System.Console.WriteLine(text);
#endif
        }

        private void HideNotification()
        {
#if false
            if (FormNotification != null &&
                !FormNotification.IsDisposed)
            {
                FormNotification.Close();
                FormNotification = null;
            }
#endif
        }

        private void UpdateNotification()
        {
            if (FormNotification != null &&
                !FormNotification.IsDisposed &&
                FormNotification.Visible)
            {
                FormNotification.Location = new Point(
                    this.Location.X + this.Width / 2 - FormNotification.Width / 2,
                    this.Location.Y + this.Height / 2 - FormNotification.Height / 2);
            }
        }

        private void FormMain_Resize(object sender, EventArgs e)
        {
            ResetColumnWidths();
            UpdateNotification();
        }

        private void FormMain_LocationChanged(object sender, EventArgs e)
        {
            UpdateNotification();
        }

        #endregion

        public static int ComparePlaylistFiles(
            TSPlaylistFile x,
            TSPlaylistFile y)
        {
            if (x == null && y == null)
            {
                return 0;
            }
            else if (x == null && y != null)
            {
                return 1;
            }
            else if (x != null && y == null)
            {
                return -1;
            }
            else
            {
                if (x.TotalLength > y.TotalLength)
                {
                    return -1;
                }
                else if (y.TotalLength > x.TotalLength)
                {
                    return 1;
                }
                else
                {
                    return x.Name.CompareTo(y.Name);
                }
            }
        }
    }

    public class ListViewColumnSorter : IComparer
    {
        private int ColumnToSort;
        private SortOrder OrderOfSort;
        private CaseInsensitiveComparer ObjectCompare;

        public ListViewColumnSorter()
        {
            ColumnToSort = 0;
            OrderOfSort = SortOrder.None;
            ObjectCompare = new CaseInsensitiveComparer();
        }

        public int Compare(
            object x,
            object y)
        {
            ListViewItem listviewX = (ListViewItem)x;
            ListViewItem listviewY = (ListViewItem)y;

            int compareResult = ObjectCompare.Compare(
                listviewX.SubItems[ColumnToSort].Tag,
                listviewY.SubItems[ColumnToSort].Tag);

            if (OrderOfSort == SortOrder.Ascending)
            {
                return compareResult;
            }
            else if (OrderOfSort == SortOrder.Descending)
            {
                return (-compareResult);
            }
            else
            {
                return 0;
            }
        }

        public int SortColumn
        {
            set
            {
                ColumnToSort = value;
            }
            get
            {
                return ColumnToSort;
            }
        }

        public SortOrder Order
        {
            set
            {
                OrderOfSort = value;
            }
            get
            {
                return OrderOfSort;
            }
        }
    }

    public class ScanBDROMResult
    {
        public Exception ScanException = new Exception("Scan has not been run.");
        public Dictionary<string, Exception> FileExceptions = new Dictionary<string, Exception>();
    }

    public static class MessageBox {
        public static DialogResult Show(string msg,
                                        string title = null,
                                        MessageBoxButtons buttons = 0,
                                        MessageBoxIcon icon = 0,
                                        MessageBoxDefaultButton defButton = 0,
                                        MessageBoxOptions options = 0,
                                        string helpFilePath = null,
                                        string keyword = null)
        {
            System.Console.WriteLine(msg);
            return DialogResult.No;
        }

    }
}
