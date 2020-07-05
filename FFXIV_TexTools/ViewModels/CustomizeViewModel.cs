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
using FFXIV_TexTools.Properties;
using FFXIV_TexTools.Resources;
using FolderSelect;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using xivModdingFramework.General.Enums;

namespace FFXIV_TexTools.ViewModels
{
    public class CustomizeViewModel : INotifyPropertyChanged
    {
        private readonly string _skinDefault = "#FFFFFFFF";
        private readonly string _brownDefault = "#FF603913";
        private readonly string _bgColorDefault = "#FF777777";
        private string _defaultAuthor = Settings.Default.Default_Author;


        public CustomizeViewModel()
        {
            SkinTypes = new List<string>
            {
                {XivStringRaces.Hyur_M},
                {XivStringRaces.Hyur_H},
                {XivStringRaces.Aura_R},
                {XivStringRaces.Aura_X}
            };

            TargetImporters = new List<string>
            {
                {XivStrings.OpenCollada},
                {XivStrings.AutodeskCollada}
            };

            DefaultRaces = new List<string>
            {
                XivRace.Hyur_Midlander_Male.GetDisplayName(),
                XivRace.Hyur_Midlander_Female.GetDisplayName(),
                XivRace.Hyur_Highlander_Male.GetDisplayName(),
                XivRace.Hyur_Highlander_Female.GetDisplayName(),
                XivRace.Elezen_Male.GetDisplayName(),
                XivRace.Elezen_Female.GetDisplayName(),
                XivRace.Miqote_Male.GetDisplayName(),
                XivRace.Miqote_Female.GetDisplayName(),
                XivRace.Roegadyn_Male.GetDisplayName(),
                XivRace.Roegadyn_Female.GetDisplayName(),
                XivRace.Lalafell_Male.GetDisplayName(),
                XivRace.Lalafell_Female.GetDisplayName(),
                XivRace.AuRa_Male.GetDisplayName(),
                XivRace.AuRa_Female.GetDisplayName(),
                XivRace.Viera.GetDisplayName(),
                XivRace.Hrothgar.GetDisplayName()
            };

        }

        #region Public Properties

        /// <summary>
        /// The FFXIV directory
        /// </summary>
        public string FFXIV_Directory
        {
            get => Path.GetFullPath(Settings.Default.FFXIV_Directory);
            set => NotifyPropertyChanged(nameof(FFXIV_Directory));
        }

        /// <summary>
        /// The save directory
        /// </summary>
        public string Save_Directory
        {
            get => Path.GetFullPath(Settings.Default.Save_Directory);
            set => NotifyPropertyChanged(nameof(Save_Directory));
        }

        /// <summary>
        /// The backups directory
        /// </summary>
        public string Backups_Directory
        {
            get => Path.GetFullPath(Settings.Default.Backup_Directory);
            set => NotifyPropertyChanged(nameof(Backups_Directory));
        }

        /// <summary>
        /// The modpack directory
        /// </summary>
        public string ModPack_Directory
        {
            get => Path.GetFullPath(Settings.Default.ModPack_Directory);
            set => NotifyPropertyChanged(nameof(ModPack_Directory));
        }

        /// <summary>
        /// The default author
        /// </summary>
        public string DefaultAuthor
        {
            get => _defaultAuthor;
            set
            {
                _defaultAuthor = value;
                NotifyPropertyChanged(nameof(DefaultAuthor));
            }
        }

        /// <summary>
        /// The selected skin color
        /// </summary>
        public Color Selected_SkinColor
        {
            get => (Color)ColorConverter.ConvertFromString(Settings.Default.Skin_Color);
            set
            {
                SetSkinColor(value);
                NotifyPropertyChanged(nameof(Selected_SkinColor));
            }
        }

        /// <summary>
        /// The selected hair color
        /// </summary>
        public Color Selected_HairColor
        {
            get => (Color)ColorConverter.ConvertFromString(Settings.Default.Hair_Color);
            set
            {
                SetHairColor(value);
                NotifyPropertyChanged(nameof(Selected_HairColor));
            }
        }

        /// <summary>
        /// The selected iris color
        /// </summary>
        public Color Selected_IrisColor
        {
            get => (Color)ColorConverter.ConvertFromString(Settings.Default.Iris_Color);
            set
            {
                SetIrisColor(value);
                NotifyPropertyChanged(nameof(Selected_IrisColor));
            }
        }

        /// <summary>
        /// The selected etc. color
        /// </summary>
        public Color Selected_EtcColor
        {
            get => (Color)ColorConverter.ConvertFromString(Settings.Default.Etc_Color);
            set
            {
                SetEtcColor(value);
                NotifyPropertyChanged(nameof(Selected_EtcColor));
            }
        }

        /// <summary>
        /// The selected background color for the 3D viewport
        /// </summary>
        public Color Selected_BgColor
        {
            get => (Color)ColorConverter.ConvertFromString(Settings.Default.BG_Color);
            set
            {
                SetBgColor(value);
                NotifyPropertyChanged(nameof(Selected_BgColor));
            }
        }

        /// <summary>
        /// The list of skin types
        /// </summary>
        public List<string> SkinTypes { get; }

        /// <summary>
        /// The selected skin type
        /// </summary>
        public string Selected_SkinType
        {
            get => Settings.Default.Default_Race;
            set
            {
                if (Selected_SkinType != value)
                {
                    SetSkin(value);
                }

            }
        }

        /// <summary>
        /// The list of default races
        /// </summary>
        public List<string> DefaultRaces { get; }

        /// <summary>
        /// The selected default race
        /// </summary>
        public string SelectedDefaultRace
        {
            get
            {
                try
                {
                    return Settings.Default.Default_Race_Selection;
                }
                catch
                {
                    return XivRace.Hyur_Midlander_Male.GetDisplayName();
                }
            }
            set
            {
                if (SelectedDefaultRace != value)
                {
                    SetDefaultRace(value);
                }
            }
        }

        /// <summary>
        /// The list of target importers
        /// </summary>
        public List<string> TargetImporters { get; set; }

        /// <summary>
        /// The selected importer
        /// </summary>
        public string SelectedImporter
        {
            get => Settings.Default.DAE_Plugin_Target;
            set
            {
                if (SelectedImporter != value)
                {
                    SetImporter(value);
                }
            }
        }

        public bool ExportTextureAsDDS
        {
            get => Settings.Default.ExportTexDDS;
            set
            {
                Settings.Default.ExportTexDDS = value;
                Settings.Default.Save();
                NotifyPropertyChanged(nameof(ExportTextureAsDDS));
                NotifyPropertyChanged(nameof(ExportTexDisplay));
                EnsureValidExportFormat();
            }
        }

        public bool ExportTextureAsBMP
        {
            get => Settings.Default.ExportTexBMP;
            set
            {
                Settings.Default.ExportTexBMP = value;
                Settings.Default.Save();
                NotifyPropertyChanged(nameof(ExportTextureAsBMP));
                NotifyPropertyChanged(nameof(ExportTexDisplay));
                EnsureValidExportFormat();
            }
        }

        public bool ExportTextureAsPNG
        {
            get => Settings.Default.ExportTexPNG;
            set
            {
                Settings.Default.ExportTexPNG = value;
                Settings.Default.Save();
                NotifyPropertyChanged(nameof(ExportTextureAsPNG));
                NotifyPropertyChanged(nameof(ExportTexDisplay));
                EnsureValidExportFormat();
            }
        }

        private void EnsureValidExportFormat()
        {
            if (!Settings.Default.ExportTexPNG &&
                !Settings.Default.ExportTexBMP &&
                !Settings.Default.ExportTexDDS)
            {
                this.ExportTextureAsDDS = true;
            }
        }

        public string ExportTexDisplay
        {
            get
            {
                string val = string.Empty;

                if (this.ExportTextureAsDDS)
                    val += "DDS";

                if (this.ExportTextureAsBMP)
                    val += string.IsNullOrEmpty(val) ? "BMP" : ", BMP";

                 if (this.ExportTextureAsPNG)
                    val += string.IsNullOrEmpty(val) ? "PNG" : ", PNG";

                return val;
            }
        }

        #endregion

        #region Commands

        // Button commands
        public ICommand FFXIV_SelectDir => new RelayCommand(FFXIVSelectDir);
        public ICommand Save_SelectDir => new RelayCommand(SaveSelectDir);
        public ICommand Backup_SelectDir => new RelayCommand(BackupSelectDir);
        public ICommand ModPack_SelectDir => new RelayCommand(ModPackSelectDir);
        public ICommand Customize_Reset => new RelayCommand(ResetToDefault);
        public ICommand CloseCustomize => new RelayCommand(CustomizeClose);

        private void CustomizeClose(object obj)
        {
            if (!Settings.Default.Default_Author.Equals(DefaultAuthor))
            {
                Settings.Default.Default_Author = DefaultAuthor;
                Settings.Default.Save();
            }
        }

        /// <summary>
        /// The select ffxiv directory command
        /// </summary>
        private void FFXIVSelectDir(object obj)
        {
            var folderSelect = new FolderSelectDialog
            {
                Title = UIMessages.SelectffxivFolderTitle
            };

            if (folderSelect.ShowDialog())
            {
                Settings.Default.FFXIV_Directory = folderSelect.FileName;
                Settings.Default.Save();
            }

            //TODO: Logic to restart application once a new game directory is chosen

            FFXIV_Directory = Settings.Default.FFXIV_Directory;
        }

        /// <summary>
        /// The select save directory command
        /// </summary>
        private void SaveSelectDir(object obj)
        {
            var oldSaveLocation = Save_Directory;
            var folderSelect = new FolderSelectDialog
            {
                Title = UIMessages.NewSaveLocationTitle,
                InitialDirectory = oldSaveLocation
            };

            if (folderSelect.ShowDialog())
            {
                if (FlexibleMessageBox.Show(UIMessages.MoveDataMessage,
                        UIMessages.MoveDataTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    try
                    {
                        Directory.Move(oldSaveLocation, folderSelect.FileName);
                    }
                    catch
                    {
                        var newLoc = folderSelect.FileName;

                        foreach (var dirPath in Directory.GetDirectories(oldSaveLocation, "*",
                            SearchOption.AllDirectories))
                        {
                            Directory.CreateDirectory(dirPath.Replace(oldSaveLocation, newLoc));
                        }

                        foreach (var newPath in Directory.GetFiles(oldSaveLocation, "*.*",
                            SearchOption.AllDirectories))
                        {
                            File.Copy(newPath, newPath.Replace(oldSaveLocation, newLoc), true);
                        }

                        DeleteDirectory(oldSaveLocation);
                    }
                }

                Settings.Default.Save_Directory = folderSelect.FileName;
                Settings.Default.Save();

                FlexibleMessageBox.Show(string.Format(UIMessages.SavedLocationChangedMessage, folderSelect.FileName), UIMessages.NewDirectoryTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);

                Save_Directory = Settings.Default.Backup_Directory;
            }
        }

        /// <summary>
        /// The select backup directory command
        /// </summary>
        private void BackupSelectDir(object obj)
        {
            var oldIndexBackupLocation = Backups_Directory;
            var folderSelect = new FolderSelectDialog
            {
                Title = UIMessages.NewBackupLocationTitle,
                InitialDirectory = oldIndexBackupLocation
            };

            if (folderSelect.ShowDialog())
            {
                if (FlexibleMessageBox.Show(UIMessages.MoveDataMessage, 
                        UIMessages.MoveDataTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    try
                    {
                        Directory.Move(oldIndexBackupLocation, folderSelect.FileName);
                    }
                    catch
                    {
                        var newLoc = folderSelect.FileName;

                        foreach (var dirPath in Directory.GetDirectories(oldIndexBackupLocation, "*", SearchOption.AllDirectories))
                        {
                            Directory.CreateDirectory(dirPath.Replace(oldIndexBackupLocation, newLoc));
                        }

                        foreach (var newPath in Directory.GetFiles(oldIndexBackupLocation, "*.*", SearchOption.AllDirectories))
                        {
                            File.Copy(newPath, newPath.Replace(oldIndexBackupLocation, newLoc), true);
                        }

                        DeleteDirectory(oldIndexBackupLocation);
                    }
                }

                Settings.Default.Backup_Directory = folderSelect.FileName;
                Settings.Default.Save();

                FlexibleMessageBox.Show(string.Format(UIMessages.IndexBackupLocationChangedMessage, folderSelect.FileName), UIMessages.NewDirectoryTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);

                Backups_Directory = Settings.Default.Backup_Directory;
            }
        }

        /// <summary>
        /// The select mod pack directory command
        /// </summary>
        private void ModPackSelectDir(object obj)
        {
            var oldModPackLocation = ModPack_Directory;
            var folderSelect = new FolderSelectDialog
            {
                Title = UIMessages.newModpacksLocationTitle,
                InitialDirectory = oldModPackLocation
            };

            if (folderSelect.ShowDialog())
            {
                if (FlexibleMessageBox.Show(UIMessages.MoveDataMessage, 
                        UIMessages.MoveDataTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    try
                    {
                        Directory.Move(oldModPackLocation, folderSelect.FileName);
                    }
                    catch
                    {
                        var newLoc = folderSelect.FileName;

                        foreach (var dirPath in Directory.GetDirectories(oldModPackLocation, "*", SearchOption.AllDirectories))
                        {
                            Directory.CreateDirectory(dirPath.Replace(oldModPackLocation, newLoc));
                        }

                        foreach (var newPath in Directory.GetFiles(oldModPackLocation, "*.*", SearchOption.AllDirectories))
                        {
                            File.Copy(newPath, newPath.Replace(oldModPackLocation, newLoc), true);
                        }

                        DeleteDirectory(oldModPackLocation);
                    }
                }

                Settings.Default.ModPack_Directory = folderSelect.FileName;
                Settings.Default.Save();

                FlexibleMessageBox.Show(string.Format(UIMessages.ModPacksLocationChangedMessage, folderSelect.FileName), UIMessages.NewDirectoryTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);

                ModPack_Directory = Settings.Default.ModPack_Directory;
            }
        }

        /// <summary>
        /// The reset to default command
        /// </summary>
        private void ResetToDefault(object obj)
        {
            Selected_SkinColor = (Color)ColorConverter.ConvertFromString(_skinDefault);
            Selected_HairColor = (Color)ColorConverter.ConvertFromString(_brownDefault);
            Selected_IrisColor = (Color)ColorConverter.ConvertFromString(_brownDefault);
            Selected_EtcColor = (Color)ColorConverter.ConvertFromString(_brownDefault);
            Selected_BgColor = (Color)ColorConverter.ConvertFromString(_bgColorDefault);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Deletes a directory
        /// </summary>
        /// <param name="target_dir">The target directory to delete</param>
        private static void DeleteDirectory(string target_dir)
        {
            var files = Directory.GetFiles(target_dir);
            var dirs = Directory.GetDirectories(target_dir);

            foreach (var file in files)
            {
                File.Delete(file);
            }

            foreach (var dir in dirs)
            {
                DeleteDirectory(dir);
            }

            Directory.Delete(target_dir, false);
        }

        private void SetDefaultRace(string selectedRace)
        {
            Settings.Default.Default_Race_Selection = selectedRace;
            Settings.Default.Save();
        }

        /// <summary>
        /// Saves the skin to the settings
        /// </summary>
        /// <param name="selectedSkin">The selected skin</param>
        private void SetSkin(string selectedSkin)
        {
            Settings.Default.Default_Race = selectedSkin;
            Settings.Default.Save();
        }

        /// <summary>
        /// Saves the skin color to the settings
        /// </summary>
        /// <param name="selectedColor">The selected skin color</param>
        private void SetSkinColor(Color selectedColor)
        {
            Settings.Default.Skin_Color = selectedColor.ToString();
            Settings.Default.Save();
        }

        /// <summary>
        /// Saves the hair color to the settings
        /// </summary>
        /// <param name="selectedColor">The selected hair color</param>
        private void SetHairColor(Color selectedColor)
        {
            Settings.Default.Hair_Color = selectedColor.ToString();
            Settings.Default.Save();
        }

        /// <summary>
        /// Saves the iris color to the settings
        /// </summary>
        /// <param name="selectedColor">The selected iris color</param>
        private void SetIrisColor(Color selectedColor)
        {
            Settings.Default.Iris_Color = selectedColor.ToString();
            Settings.Default.Save();
        }

        /// <summary>
        /// Saves the etc. color to the settings
        /// </summary>
        /// <param name="selectedColor">The selected etc. color</param>
        private void SetEtcColor(Color selectedColor)
        {
            Settings.Default.Etc_Color = selectedColor.ToString();
            Settings.Default.Save();
        }

        /// <summary>
        /// Saves the 3D viewport background color to the settings
        /// </summary>
        /// <param name="selectedColor">The selected background color</param>
        private void SetBgColor(Color selectedColor)
        {
            Settings.Default.BG_Color = selectedColor.ToString();
            Settings.Default.Save();
        }

        /// <summary>
        /// Saves the importer to the settings
        /// </summary>
        /// <param name="importer">The importer</param>
        private void SetImporter(string importer)
        {
            Settings.Default.DAE_Plugin_Target = importer;
            Settings.Default.Save();
        }
        #endregion


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
