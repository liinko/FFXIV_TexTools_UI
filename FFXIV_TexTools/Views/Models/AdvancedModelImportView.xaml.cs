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

using FFXIV_TexTools.ViewModels;
using xivModdingFramework.General.Enums;
using xivModdingFramework.Items.Interfaces;
using xivModdingFramework.Models.DataContainers;

namespace FFXIV_TexTools.Views.Models
{
    /// <summary>
    /// Interaction logic for AdvancedModelImportView.xaml
    /// </summary>
    public partial class AdvancedModelImportView
    {
        private AdvancedImportViewModel _viewModel;
        private bool _fromWizard;
        private IItemModel _itemModel;

        public AdvancedModelImportView(XivMdl xivMdl,XivMdl modMdl, IItemModel itemModel, XivRace selectedRace, bool fromWizard)
        {
            InitializeComponent();

            _itemModel = itemModel;
            _fromWizard = fromWizard;
            _viewModel = new AdvancedImportViewModel(xivMdl,modMdl, itemModel, selectedRace, this, fromWizard);
            this.DataContext = _viewModel;

            if (fromWizard)
            {
                Title = FFXIV_TexTools.Resources.UIStrings.Advanced_Model_Options;
                ImportButton.Content = FFXIV_TexTools.Resources.UIStrings.Add;
            }
        }

        public byte[] RawModelData { get; set; }

        /// <summary>
        /// Event Handler for Cancel Button Click
        /// </summary>
        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// Event Handler for Import Button Click
        /// </summary>
        private async void ImportButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            await _viewModel.ImportAsync();

            DialogResult = true;

            if (_fromWizard)
            {
                RawModelData = _viewModel.RawModelData;
            }

            Close();
        }

        /// <summary>
        /// Event Handler for ForceUV1Quadrant Checkbox Click
        /// </summary>
        private void ForceUV1Quadrant_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_itemModel.PrimaryCategory.Equals(FFXIV_TexTools.Resources.XivStrings.Gear))
            {
                Properties.Settings.Default.ForceUV1Quadrant = _viewModel.ForceUV1QuadrantChecked;
                Properties.Settings.Default.Save();
            }
        }

        /// <summary>
        /// Event Handler for CloneUV1toUV2 Checkbox Click
        /// </summary>
        private void CloneUV1toUV2_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_itemModel.SecondaryCategory.Equals(FFXIV_TexTools.Resources.XivStrings.Hair))
            {
                Properties.Settings.Default.CloneUV1toUV2 = _viewModel.CloneUV1toUV2Checked;
                Properties.Settings.Default.Save();
            }
        }
    }
}
