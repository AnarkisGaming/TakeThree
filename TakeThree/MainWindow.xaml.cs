/*
 * TakeThree, as originally included in After the Collapse
 * Copyright (C) 2016-2020 Anarkis Gaming. All rights reserved.
 *  
 * This file and all other files accompanying this distribution are
 * licensed to you under the Microsoft Reciprocal Source License. Please see
 * the LICENSE file for more details.
 * 
 * As a reminder: the software is licensed "as-is." You bear the risk of using
 * it. The contributors give no express warranties, guarantees or conditions.
 * You may have additional consumer rights under your local laws which this
 * license cannot change. To the extent permitted under your local laws, the
 * contributors exclude the implied warranties of merchantability, fitness for
 * a particular purpose and non-infringement.
 */

using Newtonsoft.Json;
using Steamworks;
using Steamworks.Ugc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TakeThree
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<string> ETags = new List<string>() { "Localization/Translation", "Item", "Map", "Trait", "Balance", "Event", "Other" };
        List<string> EVersions = new List<string>() { "0.4.x", "0.5.x", "0.6.x" };
        List<ulong> EPublished = new List<ulong>();
        ModConfiguration config = null;
        string directory = null;
        string preview = null;

        List<string> versions = new List<string>();
        List<string> tags = new List<string>();

        FilePrivacy privacy = FilePrivacy.PUBLIC;

        uint appid = uint.Parse(File.ReadAllText("steam_appid.txt")); // You should replace this with your own AppID.

        public MainWindow()
        {
            SteamClient.Init(appid);

            if (! SteamClient.IsValid)
            {
                System.Windows.MessageBox.Show("Please launch this tool from Steam.", "Steamworks not initialized");
                System.Windows.Application.Current.Shutdown();
                return;
            }

            InitializeComponent();

            ResizeMode = ResizeMode.NoResize;

            ETags.ForEach(i => TagList.Items.Add(i));
            TagList.SelectedIndex = 0;

            EVersions.ForEach(i =>
            {
                System.Windows.Controls.CheckBox cb = ((System.Windows.Controls.CheckBox)FindName("Version_" + i.Replace(".", "_")));

                cb.Click += (sender, e) =>
                {
                    if (cb.IsChecked.GetValueOrDefault(false)) versions.Add(i);
                    else versions.Remove(i);
                };
            });

            CheckEligibility();
            GetPublishedFiles();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            SteamClient.Shutdown();

            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
        }

        private void ChooseLocation_Click(object sender, RoutedEventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                DialogResult res = dialog.ShowDialog();

                if (res == System.Windows.Forms.DialogResult.OK)
                {
                    FileLocation.Text = dialog.SelectedPath;
                }
            }

            CheckEligibility();
        }

        private void FileLocation_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!Directory.Exists(FileLocation.Text))
            {
                SelectedModInfoBox.Content = "Directory selected does not exist";
                return;
            }

            if (!File.Exists(FileLocation.Text + System.IO.Path.DirectorySeparatorChar + "config.json"))
            {
                SelectedModInfoBox.Content = "Mod manifest does not exist";
                return;
            }

            config = null;

            try
            {
                config = JsonConvert.DeserializeObject<ModConfiguration>(File.ReadAllText(FileLocation.Text + System.IO.Path.DirectorySeparatorChar + "config.json"));
            }
            catch (Exception)
            {
                SelectedModInfoBox.Content = "Invalid mod manifest";
                return;
            }

            if (config == null)
            {
                SelectedModInfoBox.Content = "Invalid mod manifest";
                return;
            }

            SelectedModInfoBox.Content = "Selected " + config.name + " by " + config.author;

            directory = FileLocation.Text;

            CheckEligibility();
        }

        private void TagButton_Click(object sender, RoutedEventArgs e)
        {
            if (AddedTagsList.Items.Contains(TagList.SelectedItem)) AddedTagsList.Items.Remove(TagList.SelectedItem);
            else AddedTagsList.Items.Add(TagList.SelectedItem);

            TagList_SelectionChanged(null, null);

            CheckEligibility();
        }

        private void TagList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AddedTagsList.Items.Contains(TagList.SelectedItem)) TagButton.Content = "Remove";
            else TagButton.Content = "Add";
        }

        private void ChoosePreviewImageButton_Click(object sender, RoutedEventArgs e)
        {
            using (OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog())
            {
                dialog.Filter = "Image Files|*.jpeg;*.jpg;*.png";
                DialogResult res = dialog.ShowDialog();

                if (res == System.Windows.Forms.DialogResult.OK && File.Exists(dialog.FileName))
                {
                    PreviewImage.Source = new BitmapImage(new Uri(dialog.FileName));
                    preview = dialog.FileName;
                }
            }

            CheckEligibility();
        }

        private bool CheckEligibility()
        {
            bool isOK = config != null && AddedTagsList.Items.Count > 0 && versions.Count > 0 && PreviewImage.Source != null && AcceptCheckbox.IsChecked.GetValueOrDefault(false);

            if (!isOK)
            {
                SubmitButton.Content = "Please fill out all of the fields";
                SubmitButton.IsEnabled = false;
            }
            else
            {
                SubmitButton.Content = "Submit to Workshop";
                SubmitButton.IsEnabled = true;
            }

            return isOK;
        }

        private void AcceptCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            CheckEligibility();
        }

        private void SetProgress(string text)
        {
            SubmitButton.Dispatcher.Invoke(() => SubmitButton.Content = text);
        }

        private void Reset()
        {
            SubmitButton.Dispatcher.Invoke(() => MainWindow1.IsEnabled = true);
        }

        private async void GetPublishedFiles()
        {
            EPublished.Clear();
            EPublished.Add(0);

            UpdateItem.Dispatcher.Invoke(() => {
                UpdateItem.Items.Clear();
                UpdateItem.IsEnabled = false;
                UpdateItem.Items.Add("Create new mod");
                UpdateItem.SelectedIndex = 0;
            });

            int i = 1;

            while (true)
            {
                ResultPage? pg = await Query.ItemsReadyToUse.WhereUserPublished().SortByCreationDate().GetPageAsync(i);
                if (!pg.HasValue || pg.Value.Entries.Count() == 0) break;

                foreach(Item item in pg.Value.Entries)
                {
                    EPublished.Add(item.Id.Value);
                    UpdateItem.Dispatcher.Invoke(() => UpdateItem.Items.Add(item.Title + " (" + item.Id + ")"));
                }

                i++;
            }

            UpdateItem.Dispatcher.Invoke(() => UpdateItem.IsEnabled = true);
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckEligibility())
            {
                System.Windows.Forms.MessageBox.Show("Please fill out all of the fields and try again.", "Error");
                return;
            }

            MainWindow1.IsEnabled = false;

            if (Privacy_FriendsOnly.IsChecked.Value) privacy = FilePrivacy.FRIENDSONLY;
            else if(Privacy_Private.IsChecked.Value) privacy = FilePrivacy.PRIVATE;
            else privacy = FilePrivacy.PUBLIC;

            UploadItem(EPublished[UpdateItem.SelectedIndex]);
        }

        private async void UploadItem(ulong file)
        {
            SetProgress("Publishing");

            Editor ed;

            if (file == 0) ed = Editor.NewCommunityFile;
            else ed = new Editor(file);

            ed = ed.ForAppId(appid)
              .WithTitle(config.name)
              .WithDescription(config.description);

            switch (privacy)
            {
                case FilePrivacy.PUBLIC:
                    ed = ed.WithPublicVisibility();
                    break;
                case FilePrivacy.FRIENDSONLY:
                    ed = ed.WithFriendsOnlyVisibility();
                    break;
                case FilePrivacy.PRIVATE:
                    ed = ed.WithPrivateVisibility();
                    break;
            }

            foreach (string version in versions)
            {
                ed = ed.WithTag(version);
            }

            foreach (string tag in AddedTagsList.Items)
            {
                ed = ed.WithTag(tag);
            }

            ed = ed.WithContent(directory)
              .WithPreviewFile(preview)
              .WithChangeLog("Uploaded with Take Three");

            PublishResult res = await ed.SubmitAsync();

            if (res.NeedsWorkshopAgreement)
            {
                System.Windows.MessageBox.Show("Your file will be hidden until you accept the Workshop Legal Agreement at https://steamcommunity.com/sharedfiles/workshoplegalagreement.", "Steam Workshop Legal Agreement");
            }

            if (res.Success)
            {
                SetProgress("Published");

                MainWindow1.Dispatcher.Invoke(() => {
                    MainWindow1.IsEnabled = true;
                    System.Diagnostics.Process.Start("steam://url/CommunityFilePage/" + res.FileId);
                });
            }
            else
            {
                System.Windows.MessageBox.Show("Publication was unsuccessful: " + res.Result.ToString());

                MainWindow1.Dispatcher.Invoke(() => MainWindow1.IsEnabled = true);
            }

            GetPublishedFiles();

            new Thread(() => {
                Thread.Sleep(2000);
                SetProgress("Submit to Workshop");
            }).Start();
        }
    }
}
