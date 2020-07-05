﻿// FFXIV TexTools
// Copyright © 2019 Rafael Gonzalez - All Rights Reserved
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using FFXIV_TexTools.Helpers;
using FFXIV_TexTools.Models;
using FFXIV_TexTools.Resources;
using FFXIV_TexTools.ViewModels;
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using MahApps.Metro.Controls.Dialogs;
using xivModdingFramework.Mods;
using ListBox = System.Windows.Controls.ListBox;

namespace FFXIV_TexTools.Views
{
    /// <summary>
    /// Interaction logic for ModListView.xaml
    /// </summary>
    public partial class ModListView
    {
        private CancellationTokenSource _cts;
        private TextureViewModel _textureViewModel;
        private ModelViewModel _modelViewModel;

        public ModListView(TextureViewModel textureViewModel, ModelViewModel modelViewModel)
        {
            _textureViewModel = textureViewModel;
            _modelViewModel = modelViewModel;

            InitializeComponent();
        }

        /// <summary>
        /// Event handler for treeview item changed
        /// </summary>
        private async void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var selectedItem = e.NewValue as Category;

            if (e.OldValue != null)
            {
                _cts.Cancel();
                _cts.Dispose();
            }

            try
            {
                _cts = new CancellationTokenSource();

                if (selectedItem?.ParentCategory != null)
                {
                    if (selectedItem.ParentCategory.Name.Equals("ModPacks"))
                    {
                        (DataContext as ModListViewModel).UpdateInfoGrid(selectedItem);
                        modToggleButton.IsEnabled = true;
                        modDeleteButton.IsEnabled = true;
                    }
                    else
                    {
                        await (DataContext as ModListViewModel).UpdateList(selectedItem, _cts);
                    }
                }
                else
                {
                    (DataContext as ModListViewModel).ClearList();
                    modToggleButton.IsEnabled = false;
                    modDeleteButton.IsEnabled = false;
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine($"Loading Canceled\n\n{ex.Message}");
            }

        }

        /// <summary>
        /// Event handler for listbox selection changed
        /// </summary>
        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listbox = sender as ListBox;

            if (listbox.SelectedItem is ModListViewModel.ModListModel selectedModItem)
            {
                (DataContext as ModListViewModel).ModToggleText = selectedModItem.ModItem.enabled ? FFXIV_TexTools.Resources.UIStrings.Disable : FFXIV_TexTools.Resources.UIStrings.Enable;

                // If mod offset and original offset are the same then it's a matadded texture
                if(selectedModItem.ModItem.data.modOffset == selectedModItem.ModItem.data.originalOffset)
                {
                    // Disable toggle button since toggling does nothing with equal offsets
                    modToggleButton.IsEnabled = false;
                }
                else
                {
                    modToggleButton.IsEnabled = true;
                }                
                modDeleteButton.IsEnabled = true;
            }

        }

        /// <summary>
        /// Event handler for mod toggle button changed
        /// </summary>
        private async void modToggleButton_Click(object sender, RoutedEventArgs e)
        {
            var gameDirectory = new DirectoryInfo(Properties.Settings.Default.FFXIV_Directory);
            var modding = new Modding(gameDirectory);

            if ((ModListTreeView.SelectedItem as Category).ParentCategory.Name.Equals("ModPacks"))
            {
                var selectedItem = (ModListTreeView.SelectedItem as Category);

                if ((DataContext as ModListViewModel).ModToggleText == FFXIV_TexTools.Resources.UIStrings.Enable)
                {
                    await modding.ToggleModPackStatus(selectedItem.Name, true);
                    (DataContext as ModListViewModel).ModToggleText = FFXIV_TexTools.Resources.UIStrings.Disable;
                }
                else
                {
                    await modding.ToggleModPackStatus(selectedItem.Name, false);
                    (DataContext as ModListViewModel).ModToggleText = FFXIV_TexTools.Resources.UIStrings.Enable;
                }

                (DataContext as ModListViewModel).UpdateInfoGrid(selectedItem);
            }
            else
            {
                foreach (ModListViewModel.ModListModel selectedModItem in ModItemList.SelectedItems)
                {
                    // If mod offset is equal to original offset there is no point in toggling as is the case for matadd textures
                    if (selectedModItem.ModItem.data.modOffset == selectedModItem.ModItem.data.originalOffset) continue;
                    if (selectedModItem.ModItem.enabled)
                    {
                        await modding.ToggleModStatus(selectedModItem.ModItem.fullPath, false);
                        (DataContext as ModListViewModel).ModToggleText = FFXIV_TexTools.Resources.UIStrings.Enable;
                        selectedModItem.ActiveBorder = Brushes.Red;
                        selectedModItem.Active = Brushes.Gray;
                        selectedModItem.ActiveOpacity = 0.5f;
                        selectedModItem.ModItem.enabled = false;
                    }
                    else
                    {
                        await modding.ToggleModStatus(selectedModItem.ModItem.fullPath, true);
                        (DataContext as ModListViewModel).ModToggleText = FFXIV_TexTools.Resources.UIStrings.Disable;
                        selectedModItem.ActiveBorder = Brushes.Green;
                        selectedModItem.Active = Brushes.Transparent;
                        selectedModItem.ActiveOpacity = 1;
                        selectedModItem.ModItem.enabled = true;
                    }
                }
            }
        }

        /// <summary>
        /// Event handler for mod delete button
        /// </summary>
        private async void modDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var gameDirectory = new DirectoryInfo(Properties.Settings.Default.FFXIV_Directory);
            var modding = new Modding(gameDirectory);

            if ((ModListTreeView.SelectedItem as Category).ParentCategory.Name.Equals("ModPacks"))
            {
                if (FlexibleMessageBox.Show(
                        UIMessages.DeleteModPackMessage, 
                        UIMessages.DeleteModPackTitle,
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == System.Windows.Forms.DialogResult.Yes)
                {
                    var progress = await this.ShowProgressAsync(UIMessages.ModPack_Delete, UIMessages.PleaseStandByMessage);

                    await modding.DeleteModPack((ModListTreeView.SelectedItem as Category).Name);
                    (DataContext as ModListViewModel).RemoveModPack();

                    await progress.CloseAsync();
                }

            }
            else
            {
                var enumerable = ModItemList.SelectedItems as IEnumerable;
                var selectedItems = enumerable.OfType<ModListViewModel.ModListModel>().ToArray();

                foreach (var selectedModItem in selectedItems)
                {
                    await modding.DeleteMod(selectedModItem.ModItem.fullPath);
                    (DataContext as ModListViewModel).RemoveItem(selectedModItem, (Category)ModListTreeView.SelectedItem);
                }
            }
        }

        private void MetroWindow_Closed(object sender, EventArgs e)
        {
            (DataContext as ModListViewModel).Dispose();
            _cts?.Dispose();
            if (_textureViewModel.SelectedPart != null)
            {
                _textureViewModel.SelectedPart = _textureViewModel.SelectedPart;
            }
            if(_modelViewModel.SelectedPart != null)
            {
                _modelViewModel.SelectedPart = _modelViewModel.SelectedPart;
            }         
        }
    }
}
