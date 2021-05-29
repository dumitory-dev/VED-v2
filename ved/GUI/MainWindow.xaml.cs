﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using GUI.VEDHelper;
using GUI.VEDHelper.VedUnsafeNativeMethods;
using GUI.View;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace GUI
{

    public partial class MainWindow : MetroWindow
    {
        private readonly ApplicationSettings _settings = new();
        private readonly VedManager _manager;
        private readonly VirtualDiskView _disksView = new();
        public MainWindow()
        {

            try
            {

                InitializeComponent();
                _manager = new VedManager();
                LoadSettings();
                LoadMountedDisk();

                this.Closing += (_, _) => this._settings.SaveDisks(new List<VirtualDisk>(this._disksView.DiskCollection));
                this.DataContext = _disksView;
                this.FlyoutCreateFile.ClosingFinished += FlyoutCreateFile_ClosingFinished;

            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }

        }

        private void FlyoutCreateFile_ClosingFinished(object sender, RoutedEventArgs e)
        {
            this.TbPassword.Password = string.Empty;
            this.TbPathFile.Text = string.Empty;
            this.TbFileSize.Text = string.Empty;
            this.rbAesCrypt.IsChecked = null;
        }

        private void LoadSettings()
        {
            var disks = _settings.LoadDisks();

            if (disks.Count == 0)
                return;

            foreach (var disk in disks)
                this._disksView.DiskCollection.Add(disk);

        }

        private void LoadMountedDisk()
        {
            var items = JsonConvert.DeserializeObject<List<VirtualDisk>>(this._manager.GetMountedDisksJson());

            if (items == null) return;

            foreach (var item in from item in items
                                 let virtualDiskView = this._disksView.DiskCollection.FirstOrDefault(disk => disk.Path == item.Path)
                                 where virtualDiskView == null
                                 select item)
            {
                item.IsMounted = true;
                this._disksView.DiskCollection.Add(item);
            }

        }
        private async void ButtonUnMount_OnClick(object sender, RoutedEventArgs e)
        {

            try
            {
                var nowIndex = this.lvUsers.SelectedIndex;

                if (nowIndex == -1)
                    return;

                this._manager.UnMount('P');
                this._disksView.DiskCollection[nowIndex].IsMounted = false;
                this._disksView.DiskCollection[nowIndex].Letter = ' ';

                this.lvUsers.Items.Refresh();
            }
            catch (Exception exception)
            {
                await this.ShowMessageAsync("Error", exception.Message);
            }

        }


        private async void ButtonMount_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var nowIndex = this.lvUsers.SelectedIndex;

                if (nowIndex == -1)
                    return;

                var result = await this.ShowInputAsync("INFO", "Please, type your password");

                _manager.Mount(this._disksView.DiskCollection[nowIndex].Path, result, 'P');
                this._disksView.DiskCollection[nowIndex].IsMounted = true;
                this._disksView.DiskCollection[nowIndex].Letter = 'P';
                this.lvUsers.ItemsSource = new List<object>();
                this.lvUsers.ItemsSource = this._disksView.DiskCollection;

            }
            catch (Exception exception)
            {
                await this.ShowMessageAsync("Error", exception.Message);
            }

        }


        private void ButtonOpenCreateFile_OnClick(object sender, RoutedEventArgs e)
        {
            this.FlyoutCreateFile.IsOpen = true;
        }

        private async void ButtonOpenFileManager_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new SaveFileDialog { DefaultExt = ".img" };

                dlg.DefaultExt = ".img";
                dlg.Filter = "(.img)|*.img";

                var result = dlg.ShowDialog();

                if (result == true)
                    this.TbPathFile.Text = dlg.FileName;

            }
            catch (Exception error)
            {
                await this.ShowMessageAsync("Error", error.Message);
            }

        }
        

        private async void ButtonCreateFile_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (this.TbPassword.Password == string.Empty || this.TbPathFile.Text == string.Empty || this.TbFileSize.Text == string.Empty)
                {
                    await this.ShowMessageAsync("Error", "Invalid input data!");
                    return;
                }

                var fileSize = ulong.Parse(this.TbFileSize.Text);
                var cryptMode = this.rbAesCrypt.IsFocused ? VedManager.CryptMode.Aes : VedManager.CryptMode.Rc4;
                this._manager.CreateDiskFile(this.TbPathFile.Text, fileSize * 1024 * 1024, this.TbPassword.Password, cryptMode);

                await this.ShowMessageAsync("Info", "Success!");

                this._disksView.DiskCollection.Add(new VirtualDisk { IsMounted = false, Path = TbPathFile.Text, Size = TbFileSize.Text });
                this.FlyoutCreateFile.IsOpen = false;


            }
            catch (Exception error)
            {
                await this.ShowMessageAsync("Error", error.Message);
            }

        }

        private async void MenuItemOpenFile_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new OpenFileDialog { DefaultExt = ".img" };

                dlg.DefaultExt = ".img";
                dlg.Filter = "(.img)|*.img";

                var result = dlg.ShowDialog();

                if (result != true)
                    return;


                var fileInfo = new FileInfo(dlg.FileName);
                var newDisk = new VirtualDisk {Path = dlg.FileName, Size = (fileInfo.Length / 1024 / 1024).ToString()};


                if(this._disksView.DiskCollection.All(disk => newDisk.Path != disk.Path))
                    this._disksView.DiskCollection.Add(newDisk);

            }
            catch (Exception error)
            {
                await this.ShowMessageAsync("Error", error.Message);
            }
        }
    }
}