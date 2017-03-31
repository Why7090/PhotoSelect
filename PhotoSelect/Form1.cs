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

namespace PhotoSelect
{
    public partial class ImageSelect : Form
    {
        public FileInfo[] imagePath;
        public Dictionary<string, Image> images = new Dictionary<string, Image>();
        public List<int> bookmarks = new List<int>();

        public ImageSelect ()
        {
            InitializeComponent();
        }

        private void BrowseFolder (object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            imagePath = SearchImage(folderBrowserDialog1.SelectedPath);
            images.Clear();
            listView1.Items.Clear();
            int length = imageList1.Images.Count;
            for (int i = 0; i < length; i++)
            {
                var img = imageList1.Images[0];
                imageList1.Images.RemoveAt(0);
                img.Dispose();
            }
            verticalProgressBar1.Maximum = imagePath.Length;
            backgroundWorker1.RunWorkerAsync();
        }

        private FileInfo[] SearchImage (string path)
        {
            DirectoryInfo folder = new DirectoryInfo(path);
            var filter = new string[] { ".jpg", ".jpeg", ".png", ".gif", ".tiff", ".bmp", ".ico" };
            var files = folder.GetFiles("*.*", System.IO.SearchOption.TopDirectoryOnly)
                .Where(f => filter.Contains(f.Extension.ToLower())).ToArray();
            return files;
        }

        private void OnImageSelected (object sender, EventArgs e)
        {
            imageBox1.SizeMode = ImageBoxSizeMode.Stretch;
            imageBox1.Image = images[listView1.SelectedItems[0].ImageKey];
            imageBox1.SizeMode = ImageBoxSizeMode.Normal;
            if (bookmarks.Contains(listView1.SelectedIndices[0]))
            {
                button1.Image = Properties.Resources.Star_Filled_32px;
            }
            else
            {
                button1.Image = Properties.Resources.Star_32px;
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
                    button1.Image = Properties.Resources.Star_32px;
                }
                else
                {
                    bookmarks.Add(index);
                    item.BackColor = Color.Yellow;
                    button1.Image = Properties.Resources.Star_Filled_32px;
                }
            }
        }

        private void Save (object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            var path = folderBrowserDialog1.SelectedPath;
            foreach (int index in bookmarks)
            {
                var file = imagePath[index];
                file.CopyTo(Path.Combine(path, file.Name), false);
            }
            MessageBox.Show("Images saved to " + path);
        }

        private bool SelectImage (int relativeIndex)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                int index = listView1.SelectedIndices[0] + relativeIndex;
                if (index < listView1.Items.Count && index >= 0)
                {
                    listView1.Focus();
                    listView1.Items[index].Selected = true;
                    listView1.Items[index].Focused = true;
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
            MessageBox.Show(
                "\"/\" : \n  Bookmark selected image\n\n" +
                "Arrow left / up : \n  Select previous image\n\n" +
                "Arrow right / down : \n  Select next image", "Shortcuts",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void DeleteImage (object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                var item = listView1.SelectedItems[0];
                if (MessageBox.Show("Move \"" + item.Text + "\" to recycle bin?", "Warning",
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
                {
                    string key = item.ImageKey;
                    int index = listView1.SelectedIndices[0];
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
                    catch
                    {
                        MessageBox.Show("Operation failed", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    var lst = imagePath.ToList();
                    lst.RemoveAt(index);
                    imagePath = lst.ToArray();
                }
            }
        }

        private void backgroundWorker1_DoWork (object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            //int length = imagePath.Length;
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
            verticalProgressBar1.Value = e.ProgressPercentage;
        }

        private void backgroundWorker1_RunWorkerCompleted (object sender, RunWorkerCompletedEventArgs e)
        {
            foreach (var p in imagePath)
            {
                string name = p.Name;
                var item = listView1.Items.Add(name, name);
            }
            verticalProgressBar1.Value = 0;
        }
    }
}
