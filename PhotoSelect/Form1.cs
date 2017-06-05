using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Cyotek.Windows.Forms;
using Microsoft.VisualBasic.FileIO;
using PhotoSelect.Properties;

namespace PhotoSelect
{
    public partial class PhotoSelect : Form
    {
        public FileInfo[] imagePath;
        public Dictionary<string, Image> images = new Dictionary<string, Image>();
        public List<int> bookmarks = new List<int>();
        public List<FileInfo> bookmarkFiles = new List<FileInfo>();

        public PhotoSelect ()
        {
            InitializeComponent();
        }

        private void BrowseFolder (object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            foreach (int index in bookmarks)
            {
                var file = imagePath[index];
                if (!bookmarkFiles.Contains(file))
                {
                    bookmarkFiles.Add(file);
                }
            }
            imagePath = SearchImage(folderBrowserDialog1.SelectedPath);
            images.Clear();
            bookmarks.Clear();
            listView1.Items.Clear();
            int length = imageList1.Images.Count;
            for (int i = 0; i < length; i++)
            {
                var img = imageList1.Images[0];
                imageList1.Images.RemoveAt(0);
                img.Dispose();
            }
            progressBar1.Visible = true;
            progressBar1.Maximum = imagePath.Length;
            backgroundWorker1.RunWorkerAsync();
        }

        private void BrowseBookmarks(object sender, EventArgs e)
        {
            foreach (int index in bookmarks)
            {
                bookmarkFiles.Add(imagePath[index]);
            }
            imagePath = bookmarkFiles.ToArray();
            bookmarkFiles.Clear();
            bookmarks.Clear();
            for (int i = 0; i < imagePath.Length; i++)
            {
                bookmarks.Add(i);
            }
            images.Clear();
            listView1.Items.Clear();
            int length = imageList1.Images.Count;
            for (int i = 0; i < length; i++)
            {
                var img = imageList1.Images[0];
                imageList1.Images.RemoveAt(0);
                img.Dispose();
            }
            progressBar1.Visible = true;
            progressBar1.Maximum = imagePath.Length;
            backgroundWorker1.RunWorkerAsync();
        }

        private FileInfo[] SearchImage (string path)
        {
            DirectoryInfo folder = new DirectoryInfo(path);
            var filter = new string[] { ".jpg", ".jpeg", ".png", ".gif", ".tiff", ".bmp", ".ico", ".tif", ".emf", ".cur", ".wmf" };
            var files = folder.GetFiles("*.*", System.IO.SearchOption.TopDirectoryOnly)
                .Where(f => filter.Contains(f.Extension.ToLower())).ToArray();
            files = files.OrderByDescending(f => f.LastWriteTime).ToArray();
            return files;
        }

        private void OnImageSelected (object sender, EventArgs e)
        {
            imageBox1.SizeMode = ImageBoxSizeMode.Stretch;
            imageBox1.Image = images[listView1.SelectedItems[0].ImageKey];
            imageBox1.SizeMode = ImageBoxSizeMode.Normal;
            if (bookmarks.Contains(listView1.SelectedIndices[0]))
            {
                button1.Image = Resources.Star_Filled_32px;
            }
            else
            {
                button1.Image = Resources.Star_32px;
            }
        }

        private void ToggleBookmark (object sender, EventArgs e)
        {
            listView1.Focus();
            if (listView1.SelectedItems.Count > 0)
            {
                var item = listView1.SelectedItems[0];
                int index = item.Index;
                if (bookmarks.Contains(index))
                {
                    bookmarks.Remove(index);
                    item.BackColor = Color.Transparent;
                    button1.Image = Resources.Star_32px;
                }
                else
                {
                    bookmarks.Add(index);
                    item.BackColor = Color.Yellow;
                    button1.Image = Resources.Star_Filled_32px;
                }
            }
        }

        private void Save (object sender, EventArgs e)
        {
            foreach (int index in bookmarks)
            {
                var file = imagePath[index];
                if (!bookmarkFiles.Contains(file))
                {
                    bookmarkFiles.Add(file);
                }
            }
            if (folderBrowserDialog1.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            var path = folderBrowserDialog1.SelectedPath;
            var pending = bookmarkFiles.ToList();
            try
            {
                foreach (var file in pending)
                {
                    file.CopyTo(Path.Combine(path, file.Name), false);
                    bookmarkFiles.Remove(file);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Operation failed :\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            MessageBox.Show(Strings.Saved + path);
        }

        private bool SelectImage (int relativeIndex)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                int index = listView1.SelectedIndices[0] + relativeIndex;
                if (index < listView1.Items.Count && index >= 0)
                {
                    listView1.Focus();
                    var item = listView1.Items[index];
                    item.Selected = true;
                    item.Focused = true;
                    item.EnsureVisible();
                    OnImageSelected(null, null);
                    return true;
                }
            }
            return false;
        }

        protected override bool ProcessCmdKey (ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Left || keyData == Keys.Up)
            {
                SelectImage(-1);
                return true;
            }
            if (keyData == Keys.Right || keyData == Keys.Down)
            {
                SelectImage(1);
                return true;
            }
            if (keyData == Keys.OemQuestion)
            {
                ToggleBookmark(null, null);
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void ShowHelp (object sender, EventArgs e)
        {
            string content = Strings.Shortcuts;
            string title = Strings.ShortcutsTitle;
            MessageBox.Show(content, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void DeleteImage (object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                var item = listView1.SelectedItems[0];
                string content = Strings.Delete;
                string title = Strings.Warning;
                if (MessageBox.Show(String.Format(content, item.Text), title,
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
                {
                    string key = item.ImageKey;
                    int index = listView1.SelectedIndices[0];
                    SelectImage(1);
                    imageList1.Images[key].Dispose();
                    imageList1.Images.RemoveByKey(key);
                    images[key].Dispose();
                    images.Remove(key);
                    listView1.Items.Remove(item);
                    try
                    {
                        FileSystem.DeleteFile(imagePath[index].FullName,
                            UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Operation failed :\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    var lst = imagePath.ToList();
                    lst.RemoveAt(index);
                    imagePath = lst.ToArray();
                }
            }
        }

        private void backgroundWorker1_DoWork (object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            int processed = 0;

            foreach (var p in imagePath)
            {
                var image = Image.FromFile(p.FullName);
                var name = p.Name;
                images.Add(name, image);
                imageList1.Images.Add(name, image);
                processed += 1;
                worker.ReportProgress(processed);
            }
        }

        private void backgroundWorker1_ProgressChanged (object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            var bookmarkPath = new List<string>(bookmarkFiles.Count);
            foreach (var file in bookmarkFiles)
            {
                bookmarkPath.Add(file.FullName);
            }
            foreach (var p in imagePath)
            {
                string name = p.Name;
                var item = listView1.Items.Add(name, name);
                if (bookmarkPath.Contains(p.FullName))
                {
                    bookmarks.Add(item.Index);
                    item.BackColor = Color.Yellow;
                }
                else if (bookmarks.Contains(item.Index))
                {
                    item.BackColor = Color.Yellow;
                }
            }
            progressBar1.Value = 0;
            progressBar1.Visible = false;
        }
    }
}
