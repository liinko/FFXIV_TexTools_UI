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
using Newtonsoft.Json;
using SharpDX;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using xivModdingFramework.General.Enums;
using xivModdingFramework.Items.DataContainers;
using xivModdingFramework.Materials.FileTypes;
using xivModdingFramework.Mods.DataContainers;
using xivModdingFramework.Textures.DataContainers;
using xivModdingFramework.Textures.Enums;
using xivModdingFramework.Textures.FileTypes;
using Application = System.Windows.Application;

namespace FFXIV_TexTools.ViewModels
{
    public class ModListViewModel : INotifyPropertyChanged
    {
        private readonly DirectoryInfo _modListDirectory;
        private readonly DirectoryInfo _gameDirectory;
        private string _modToggleText = UIStrings.Enable_Disable;
        private Visibility _listVisibility = Visibility.Visible, _infoGridVisibility = Visibility.Collapsed;
        private string _modPackTitle, _modPackModAuthorLabel, _modPackModCountLabel, _modPackModVersionLabel, _modPackContentList, _progressText;
        private bool _itemFilter, _modPackFilter, _nameSort, _dateSort;
        private int _progressValue;
        private ObservableCollection<Category> _categories;
        private IProgress<(int current, int total)> progress;


        public ModListViewModel()
        {
            _gameDirectory = new DirectoryInfo(Properties.Settings.Default.FFXIV_Directory);
            _modListDirectory = new DirectoryInfo(Path.Combine(_gameDirectory.Parent.Parent.FullName, XivStrings.ModlistFilePath));

            progress = new Progress<(int current, int total)>((result) =>
            {
                ProgressValue = (int)(((float) result.current / (float) result.total) * 100);
                ProgressText = $"{result.current} / {result.total}";
            });

            if (Properties.Settings.Default.ModList_Sorting.Equals("NameSort")) _nameSort = true;
            else _dateSort = true;

            if (Properties.Settings.Default.ModList_Filter.Equals("Item"))
            {
                _itemFilter = true;
                SetFilter("ItemFilter");
            }                
            else
            {
                _modPackFilter = true;
                SetFilter("ModPackFilter");
            }
        }

        /// <summary>
        /// The collection of categories
        /// </summary>
        public ObservableCollection<Category> Categories
        {
            get => _categories;
            set
            {
                _categories = value;
                OnPropertyChanged(nameof(Categories));
            }
        }

        /// <summary>
        /// Gets the categories based on the item filter
        /// </summary>
        private Task GetCategoriesItemFilter()
        {
            Categories = new ObservableCollection<Category>();

            return Task.Run(() =>
            {
                var modList = JsonConvert.DeserializeObject<ModList>(File.ReadAllText(_modListDirectory.FullName));

                if (modList == null) return;

                // Mod Packs
                var category = new Category
                {
                    Name = "ModPacks",
                    Categories = new ObservableCollection<Category>(),
                    CategoryList = new List<string>()
                };

                var categoryItem = new Category
                {
                    Name = UIStrings.Standalone_Non_ModPack,
                    ParentCategory = category
                };

                category.Categories.Add(categoryItem);

                foreach (var modListModPack in modList.ModPacks)
                {
                    categoryItem = new Category
                    {
                        Name = modListModPack.name,
                        ParentCategory = category
                    };

                    category.Categories.Add(categoryItem);
                }

                if(_nameSort)
                {
                    category.Categories = new ObservableCollection<Category>(category.Categories.OrderBy(i => i.Name));
                }
                
                Application.Current.Dispatcher.Invoke(() => Categories.Add(category));

                // Mods
                var mainCategories = new HashSet<string>();

                foreach (var modEntry in modList.Mods)
                {
                    if (!modEntry.name.Equals(string.Empty))
                    {
                        mainCategories.Add(modEntry.category);
                    }
                }

                foreach (var mainCategory in mainCategories)
                {
                    category = new Category
                    {
                        Name = mainCategory,
                        Categories = new ObservableCollection<Category>(),
                        CategoryList = new List<string>()
                    };

                    var modItems =
                        from mod in modList.Mods
                        where mod.category.Equals(mainCategory)
                        select mod;

                    foreach (var modItem in modItems)
                    {
                        if (category.CategoryList.Contains(modItem.name)) continue;

                        categoryItem = new Category
                        {
                            Name = modItem.name,
                            Item = MakeItemModel(modItem),
                            ParentCategory = category
                        };

                        category.Categories.Add(categoryItem);
                        category.CategoryList.Add(modItem.name);
                    }

                    if (_nameSort)
                    {
                        category.Categories = new ObservableCollection<Category>(category.Categories.OrderBy(i => i.Name));
                    }

                    Application.Current.Dispatcher.Invoke(() => Categories.Add(category));
                }
            });
        }

        /// <summary>
        /// Gets the categoreis based on the mod pack filter
        /// </summary>
        private Task GetCategoriesModPackFilter()
        {
            Categories = new ObservableCollection<Category>();

            return Task.Run(() =>
            {
                var modList = JsonConvert.DeserializeObject<ModList>(File.ReadAllText(_modListDirectory.FullName));

                var modPackCatDict = new Dictionary<string, Category>();

                if (modList == null) return;

                // Mod Packs

                var modPacksParent = new Category
                {
                    Name = "ModPacks",
                };

                var category = new Category
                {
                    Name = UIStrings.Standalone_Non_ModPack,
                    Categories = new ObservableCollection<Category>(),
                    CategoryList = new List<string>(),
                    ParentCategory = modPacksParent
                };

                modPackCatDict.Add(category.Name, category);

                foreach (var modListModPack in modList.ModPacks)
                {
                    category = new Category
                    {
                        Name = modListModPack.name,
                        Categories = new ObservableCollection<Category>(),
                        CategoryList = new List<string>(),
                        ParentCategory = modPacksParent
                    };

                    modPackCatDict.Add(category.Name, category);
                }

                var sortedModPackCatDict = new Dictionary<string, Category>();

                if (_nameSort)
                {
                    sortedModPackCatDict = modPackCatDict.OrderBy(i => i.Value.Name).ToDictionary(pair => pair.Key, pair => pair.Value);
                }
                else
                {
                    sortedModPackCatDict = modPackCatDict;
                }

                foreach (var modPackCategory in sortedModPackCatDict)
                {
                    List<Mod> modsInModpack;

                    if (!modPackCategory.Key.Equals(UIStrings.Standalone_Non_ModPack))
                    {
                        modsInModpack = (from mod in modList.Mods
                            where mod.modPack != null && mod.modPack.name.Equals(modPackCategory.Key)
                            select mod).ToList();
                    }
                    else
                    {
                        modsInModpack = (from mod in modList.Mods
                            where mod.modPack == null
                            select mod).ToList();
                    }

                    var mainCategories = new HashSet<string>();

                    foreach (var modEntry in modsInModpack)
                    {
                        if (!modEntry.name.Equals(string.Empty))
                        {
                            mainCategories.Add(modEntry.category);
                        }
                    }

                    foreach (var mainCategory in mainCategories)
                    {
                        category = new Category
                        {
                            Name = mainCategory,
                            Categories = new ObservableCollection<Category>(),
                            CategoryList = new List<string>(),
                            ParentCategory = modPackCategory.Value
                        };

                        var modItems =
                            from mod in modsInModpack
                            where mod.category.Equals(mainCategory)
                            select mod;

                        foreach (var modItem in modItems)
                        {
                            if (category.CategoryList.Contains(modItem.name)) continue;

                            var categoryItem = new Category
                            {
                                Name = modItem.name,
                                Item = MakeItemModel(modItem),
                                ParentCategory = category
                            };

                            category.Categories.Add(categoryItem);
                            category.CategoryList.Add(modItem.name);

                        }

                        if(_nameSort)
                        {
                            category.Categories = new ObservableCollection<Category>(category.Categories.OrderBy(i => i.Name));
                        }
                        
                        modPackCategory.Value.Categories.Add(category);
                    }

                    Application.Current.Dispatcher.Invoke(() => Categories.Add(modPackCategory.Value));
                }
            });
        }


        public ObservableCollection<ModListModel> ModListPreviewList { get; set; } = new ObservableCollection<ModListModel>();

        /// <summary>
        /// Makes an generic item model from a mod item
        /// </summary>
        /// <param name="modItem">The mod item</param>
        /// <returns>The mod item as a XivGenericItemModel</returns>
        private static XivGenericItemModel MakeItemModel(Mod modItem)
        {
            var fullPath = modItem.fullPath;

            var item = new XivGenericItemModel
            {
                Name = modItem.name,
                SecondaryCategory = modItem.category,
                DataFile = XivDataFiles.GetXivDataFile(modItem.datFile)
            };

            try
            {
                if (modItem.fullPath.Contains("chara/equipment") || modItem.fullPath.Contains("chara/accessory"))
                {
                    item.PrimaryCategory = XivStrings.Gear;
                    item.ModelInfo = new XivModelInfo
                    {
                        PrimaryID = int.Parse(fullPath.Substring(17, 4))
                    };
                }

                if (modItem.fullPath.Contains("chara/weapon"))
                {
                    item.PrimaryCategory = XivStrings.Gear;
                    item.ModelInfo = new XivModelInfo
                    {
                        PrimaryID = int.Parse(fullPath.Substring(14, 4))
                    };
                }

                if (modItem.fullPath.Contains("chara/human"))
                {
                    item.PrimaryCategory = XivStrings.Character;


                    if (item.Name.Equals(XivStrings.Body))
                    {
                        item.ModelInfo = new XivModelInfo
                        {
                            PrimaryID = int.Parse(
                                fullPath.Substring(fullPath.IndexOf("/body", StringComparison.Ordinal) + 7, 4))
                        };
                    }
                    else if (item.Name.Equals(XivStrings.Hair))
                    {
                        item.ModelInfo = new XivModelInfo
                        {
                            PrimaryID = int.Parse(
                                fullPath.Substring(fullPath.IndexOf("/hair", StringComparison.Ordinal) + 7, 4))
                        };
                    }
                    else if (item.Name.Equals(XivStrings.Face))
                    {
                        item.ModelInfo = new XivModelInfo
                        {
                            PrimaryID = int.Parse(
                                fullPath.Substring(fullPath.IndexOf("/face", StringComparison.Ordinal) + 7, 4))
                        };
                    }
                    else if (item.Name.Equals(XivStrings.Tail))
                    {
                        item.ModelInfo = new XivModelInfo
                        {
                            PrimaryID = int.Parse(
                                fullPath.Substring(fullPath.IndexOf("/tail", StringComparison.Ordinal) + 7, 4))
                        };
                    }
                }

                if (modItem.fullPath.Contains("chara/common"))
                {
                    item.PrimaryCategory = XivStrings.Character;

                    if (item.Name.Equals(XivStrings.Face_Paint))
                    {
                        item.ModelInfo = new XivModelInfo
                        {
                            PrimaryID = int.Parse(
                                fullPath.Substring(fullPath.LastIndexOf("_", StringComparison.Ordinal) + 1, 1))
                        };
                    }
                    else if (item.Name.Equals(XivStrings.Equipment_Decals))
                    {
                        item.ModelInfo = new XivModelInfo();

                        if (!fullPath.Contains("_stigma"))
                        {
                            item.ModelInfo.PrimaryID = int.Parse(
                                fullPath.Substring(fullPath.LastIndexOf("_", StringComparison.Ordinal) + 1, 3));
                        }
                    }
                }

                if (modItem.fullPath.Contains("chara/monster"))
                {
                    item.PrimaryCategory = XivStrings.Companions;

                    item.ModelInfo = new XivModelInfo
                    {
                        PrimaryID = int.Parse(fullPath.Substring(15, 4)),
                        SecondaryID = int.Parse(fullPath.Substring(fullPath.IndexOf("/body", StringComparison.Ordinal) + 7, 4))
                    };
                }

                if (modItem.fullPath.Contains("chara/demihuman"))
                {
                    item.PrimaryCategory = XivStrings.Companions;

                    item.ModelInfo = new XivModelInfo
                    {
                        SecondaryID = int.Parse(fullPath.Substring(17, 4)),
                        PrimaryID = int.Parse(
                            fullPath.Substring(fullPath.IndexOf("t/e", StringComparison.Ordinal) + 3, 4))
                    };
                }

                if (modItem.fullPath.Contains("ui/"))
                {
                    item.PrimaryCategory = XivStrings.UI;

                    if (modItem.fullPath.Contains("ui/uld") || modItem.fullPath.Contains("ui/map") || modItem.fullPath.Contains("ui/loadingimage"))
                    {
                        item.ModelInfo = new XivModelInfo
                        {
                            PrimaryID = 0
                        };
                    }
                    else
                    {
                        item.ModelInfo = new XivModelInfo
                        {
                            PrimaryID = int.Parse(fullPath.Substring(fullPath.LastIndexOf("/", StringComparison.Ordinal) + 1,
                                6))
                        };
                    }
                }

                if (modItem.fullPath.Contains("/hou/"))
                {
                    item.PrimaryCategory = XivStrings.Housing;

                    item.ModelInfo = new XivModelInfo
                    {
                        PrimaryID = int.Parse(fullPath.Substring(fullPath.LastIndexOf("_m", StringComparison.Ordinal) + 2,
                            4))
                    };
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format(UIMessages.ModelDataErrorMessage, modItem.name, modItem.fullPath));
            }

            return item;
        }

        /// <summary>
        /// Update the mod list entries
        /// </summary>
        /// <param name="selectedItem">The selected item to update the entries for</param>
        public Task UpdateList(Category category, CancellationTokenSource cts)
        {
            var updateLock = new object();
            ListVisibility = Visibility.Visible;
            InfoGridVisibility = Visibility.Collapsed;
            ModListPreviewList.Clear();

            ProgressValue = 0;
            ProgressText = string.Empty;
            Tex tex;

            return Task.Run(async () =>
            {
                var selectedItem = category.Item as XivGenericItemModel;
                if (selectedItem == null) return;

                var mtrl = new Mtrl(_gameDirectory, selectedItem.DataFile, GetLanguage());
                var modList = JsonConvert.DeserializeObject<ModList>(File.ReadAllText(_modListDirectory.FullName));

                var modItems = new List<Mod>();

                if (ModPackFilter)
                {
                    var modPackCategory = category;

                    while (!modPackCategory.ParentCategory.Name.Equals("ModPacks"))
                    {
                        modPackCategory = modPackCategory.ParentCategory;
                    }

                    foreach (var mod in modList.Mods)
                    {
                        if (!mod.name.Equals(selectedItem.Name)) continue;

                        if (mod.modPack != null)
                        {
                            if (mod.modPack.name == modPackCategory.Name)
                            {
                                modItems.Add(mod);
                            }
                        }
                        else
                        {
                            modItems.Add(mod);
                        }
                    }
                }
                else
                {
                    modItems =
                        (from mod in modList.Mods
                         where mod.name.Equals(selectedItem.Name)
                         select mod).ToList();
                }

                if (modItems.Count > 10)
                {
                    tex = new Tex(_gameDirectory, selectedItem.DataFile);
                    await tex.GetIndexFileDictionary();
                }
                else
                {
                    tex = new Tex(_gameDirectory);
                }

                var modNum = 0;

                await Task.Run(async () =>
                {
                    foreach (var modItem in modItems)
                    {
                        var itemPath = modItem.fullPath;

                        var modListModel = new ModListModel
                        {
                            ModItem = modItem
                        };

                        // Race
                        if (selectedItem.PrimaryCategory.Equals(XivStrings.Gear))
                        {
                            if (modItem.fullPath.Contains("equipment"))
                            {
                                string raceCode;
                                if (itemPath.Contains("/v"))
                                {
                                    raceCode = itemPath.Substring(itemPath.LastIndexOf("_c") + 2, 4);
                                }
                                else
                                {
                                    raceCode = itemPath.Substring(itemPath.LastIndexOf("/c") + 2, 4);
                                }

                                modListModel.Race = XivRaces.GetXivRace(raceCode).GetDisplayName();
                            }
                            else
                            {
                                modListModel.Race = XivStrings.All;
                            }
                        }
                        else if (selectedItem.PrimaryCategory.Equals(XivStrings.Character))
                        {
                            if (!modItem.fullPath.Contains("chara/common"))
                            {
                                var raceCode = itemPath.Substring(itemPath.IndexOf("n/c") + 3, 4);
                                modListModel.Race = XivRaces.GetXivRace(raceCode).GetDisplayName();
                            }
                            else
                            {
                                modListModel.Race = XivStrings.All;
                            }

                        }
                        else if (selectedItem.PrimaryCategory.Equals(XivStrings.Companions))
                        {
                            modListModel.Race = XivStrings.Monster;
                        }
                        else if (selectedItem.PrimaryCategory.Equals(XivStrings.UI))
                        {
                            modListModel.Race = XivStrings.All;
                        }
                        else if (selectedItem.PrimaryCategory.Equals(XivStrings.Housing))
                        {
                            modListModel.Race = XivStrings.All;
                        }

                        XivTexType? xivTexType = null;
                        // Map
                        if (itemPath.Contains("_d."))
                        {
                            xivTexType = XivTexType.Diffuse;
                            modListModel.Map = xivTexType.ToString();
                        }
                        else if (itemPath.Contains("_n."))
                        {
                            xivTexType = XivTexType.Normal;
                            modListModel.Map = xivTexType.ToString();
                        }
                        else if (itemPath.Contains("_s."))
                        {
                            xivTexType = XivTexType.Specular;
                            modListModel.Map = xivTexType.ToString();
                        }
                        else if (itemPath.Contains("_m."))
                        {
                            xivTexType = XivTexType.Multi;
                            modListModel.Map = xivTexType.ToString();
                        }
                        else if (itemPath.Contains("material"))
                        {
                            xivTexType = XivTexType.ColorSet;
                            modListModel.Map = xivTexType.ToString();
                        }
                        else if (itemPath.Contains("decal"))
                        {
                            xivTexType = XivTexType.Mask;
                            modListModel.Map = xivTexType.ToString();
                        }
                        else if (itemPath.Contains("vfx"))
                        {
                            xivTexType = XivTexType.Vfx;
                            modListModel.Map = xivTexType.ToString();
                        }
                        else if (itemPath.Contains("ui/"))
                        {
                            if (itemPath.Contains("icon"))
                            {
                                xivTexType = XivTexType.Icon;
                                modListModel.Map = xivTexType.ToString();
                            }
                            else if (itemPath.Contains("map"))
                            {
                                xivTexType = XivTexType.Map;
                                modListModel.Map = xivTexType.ToString();
                            }
                            else
                            {
                                modListModel.Map = "UI";
                            }
                        }
                        else if (itemPath.Contains(".mdl"))
                        {
                            modListModel.Map = "3D";
                        }
                        else
                        {
                            modListModel.Map = "--";
                        }

                        // Part
                        if (itemPath.Contains("_b_")|| itemPath.Contains("_b."))
                        {
                            modListModel.Part = "b";
                        }
                        else if (itemPath.Contains("_c_")|| itemPath.Contains("_c."))
                        {
                            modListModel.Part = "c";
                        }
                        else if (itemPath.Contains("_d_")|| itemPath.Contains("_d."))
                        {
                            modListModel.Part = "d";
                        }
                        else
                        {
                            modListModel.Part = "a";
                        }

                        // Number
                        if (itemPath.Contains("/hair"))
                        {
                            modListModel.Number = itemPath.Substring(itemPath.IndexOf("/hair") + 8, 3).TrimStart('0');
                        }
                        else if (itemPath.Contains("/body"))
                        {
                            modListModel.Number = itemPath.Substring(itemPath.IndexOf("/body") + 8, 3).TrimStart('0');
                        }
                        else if (itemPath.Contains("/face"))
                        {
                            modListModel.Number = itemPath.Substring(itemPath.IndexOf("/face") + 8, 3).TrimStart('0');
                        }
                        else if (itemPath.Contains("/tail"))
                        {
                            modListModel.Number = itemPath.Substring(itemPath.IndexOf("/tail") + 8, 3).TrimStart('0');
                        }
                        else if (itemPath.Contains("/zear"))
                        {
                            modListModel.Number = itemPath.Substring(itemPath.IndexOf("/zear") + 8, 3).TrimStart('0');
                        }
                        else if (itemPath.Contains("/decal_face"))
                        {
                            modListModel.Number = itemPath.Substring(itemPath.IndexOf("/decal_face") + 19, 2).TrimEnd('.');                            
                        }
                        else if (itemPath.Contains("/decal_equip") && !itemPath.Contains("stigma"))
                        {
                            modListModel.Number = itemPath.Substring(itemPath.IndexOf("/decal_equip") + 20, 3);
                        }
                        else
                        {
                            modListModel.Number = "--";
                        }

                        // Image
                        if (itemPath.Contains("material"))
                        {
                            var dxVersion = int.Parse(Properties.Settings.Default.DX_Version);

                            var offset = modItem.enabled ? modItem.data.modOffset : modItem.data.originalOffset;

                            try
                            {
                                mtrl.DataFile = XivDataFiles.GetXivDataFile(modItem.datFile);

                                var mtrlData = await mtrl.GetMtrlData(offset, modItem.fullPath, dxVersion);

                                var floats = Half.ConvertToFloat(mtrlData.ColorSetData.ToArray());

                                var floatArray = Utilities.ToByteArray(floats);

                                if (floatArray.Length > 0)
                                {
                                    using (var img = Image.LoadPixelData<RgbaVector>(floatArray, 4, 16))
                                    {
                                        img.Mutate(x => x.Opacity(1));

                                        BitmapImage bmp;

                                        using (var ms = new MemoryStream())
                                        {
                                            img.Save(ms, new BmpEncoder());

                                            bmp = new BitmapImage();
                                            bmp.BeginInit();
                                            bmp.StreamSource = ms;
                                            bmp.CacheOption = BitmapCacheOption.OnLoad;
                                            bmp.EndInit();
                                            bmp.Freeze();
                                        }

                                        modListModel.Image =
                                            Application.Current.Dispatcher.Invoke(() => bmp);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                FlexibleMessageBox.Show(
                                    string.Format(UIMessages.MaterialFileReadErrorMessage, modItem.fullPath,
                                        ex.Message),
                                    UIMessages.MaterialDataReadErrorTitle,
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }

                        }
                        else if (itemPath.Contains(".mdl"))
                        {
                            modListModel.Image = Application.Current.Dispatcher.Invoke(() => new BitmapImage(
                                new Uri("pack://application:,,,/FFXIV_TexTools;component/Resources/3DModel.png")));
                        }
                        else
                        {
                            var ttp = new TexTypePath
                            {
                                Type = xivTexType.GetValueOrDefault(),
                                DataFile = XivDataFiles.GetXivDataFile(modItem.datFile),
                                Path = modItem.fullPath
                            };

                            XivTex texData;
                            try
                            {
                                if (modItems.Count > 10)
                                {
                                    texData = await tex.GetTexDataPreFetchedIndex(ttp);
                                }
                                else
                                {
                                    texData = await tex.GetTexData(ttp);
                                }

                            }
                            catch (Exception ex)
                            {
                                FlexibleMessageBox.Show(
                                    string.Format(UIMessages.TextureFileReadErrorMessage, ttp.Path, ex.Message),
                                    UIMessages.TextureDataReadErrorTitle,
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }

                            var mapBytes = await tex.GetImageData(texData);

                            using (var img = Image.LoadPixelData<Rgba32>(mapBytes, texData.Width, texData.Height))
                            {
                                img.Mutate(x => x.Opacity(1));

                                BitmapImage bmp;

                                using (var ms = new MemoryStream())
                                {
                                    img.Save(ms, new BmpEncoder());

                                    bmp = new BitmapImage();
                                    bmp.BeginInit();
                                    bmp.StreamSource = ms;
                                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                                    bmp.EndInit();
                                    bmp.Freeze();
                                }

                                modListModel.Image =
                                    Application.Current.Dispatcher.Invoke(() => bmp);
                            }
                        }

                        // Status
                        if (modItem.enabled)
                        {
                            modListModel.ActiveBorder = Brushes.Green;
                            modListModel.Active = Brushes.Transparent;
                            modListModel.ActiveOpacity = 1;
                        }
                        else
                        {
                            modListModel.ActiveBorder = Brushes.Red;
                            modListModel.Active = Brushes.Gray;
                            modListModel.ActiveOpacity = 0.5f;
                        }

                        cts.Token.ThrowIfCancellationRequested();

                        lock (updateLock)
                        {
                            progress.Report((++modNum, modItems.Count));
                        }

                        if (!cts.IsCancellationRequested)
                        {
                            Application.Current.Dispatcher.Invoke(() => ModListPreviewList.Add(modListModel));
                        }
                    }
                }, cts.Token);
            }, cts.Token);
        }

        /// <summary>
        /// Update the info grid
        /// </summary>
        /// <remarks>
        /// The info grid shows mod pack details
        /// </remarks>
        /// <param name="category">The category to update the info grid for</param>
        public void UpdateInfoGrid(Category category)
        {
            ListVisibility = Visibility.Collapsed;
            InfoGridVisibility = Visibility.Visible;
            ModPackContentList = string.Empty;
            var enabledCount = 0;
            var disabledCount = 0;

            ProgressValue = 0;
            ProgressText = string.Empty;

            var modList = JsonConvert.DeserializeObject<ModList>(File.ReadAllText(_modListDirectory.FullName));
            List<Mod> modPackModList = null;

            if (category.Name.Equals(UIStrings.Standalone_Non_ModPack))
            {
                modPackModList = (from items in modList.Mods
                    where !items.name.Equals(string.Empty) && items.modPack == null
                    select items).ToList();

                ModPackModAuthorLabel = "[ N/A ]";
                ModPackModVersionLabel = "[ N/A ]";
            }
            else
            {
                var modPackData = (from data in modList.ModPacks
                    where data.name == category.Name
                    select data).FirstOrDefault();

                modPackModList = (from items in modList.Mods
                    where (items.modPack != null && items.modPack.name == category.Name)
                    select items).ToList();

                ModPackModAuthorLabel = modPackData.author;
                ModPackModVersionLabel = modPackData.version;
            }

            ModPackTitle = category.Name;
            ModPackModCountLabel = modPackModList.Count.ToString();

            var modNameDict = new Dictionary<string, int>();

            foreach (var mod in modPackModList)
            {
                if (mod.enabled)
                {
                    enabledCount++;
                }
                else
                {
                    disabledCount++;
                }

                if (!modNameDict.ContainsKey(mod.name))
                {
                    modNameDict.Add(mod.name, 1);
                }
                else
                {
                    modNameDict[mod.name] += 1;
                }
            }

            foreach (var mod in modNameDict)
            {
                ModPackContentList += $"[{ mod.Value}] {mod.Key}\n";
            }

            ModToggleText = enabledCount > disabledCount ? UIStrings.Disable : UIStrings.Enable;
        }

        /// <summary>
        /// Clears the list of mods 
        /// </summary>
        public void ClearList()
        {
            ListVisibility = Visibility.Visible;
            InfoGridVisibility = Visibility.Collapsed;
            ModListPreviewList.Clear();
        }

        /// <summary>
        /// Removes an item from the list when deleted
        /// </summary>
        /// <param name="item">The mod item to remove</param>
        /// <param name="category">The Category object for the item</param>
        public void RemoveItem(ModListModel item, Category category)
        {
            var modList = JsonConvert.DeserializeObject<ModList>(File.ReadAllText(_modListDirectory.FullName));

            var remainingList = (from items in modList.Mods
                                where items.name == item.ModItem.name
                                select items).ToList();

            if (remainingList.Count == 0)
            {
                Category parentCategory = null;
                if (ModPackFilter)
                {
                    foreach (var modPackCategory in Categories)
                    {
                        parentCategory = (from parent in modPackCategory.Categories
                            where parent.Name.Equals(item.ModItem.category)
                            select parent).FirstOrDefault();

                        if (parentCategory != null)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    parentCategory = (from parent in Categories
                        where parent.Name.Equals(item.ModItem.category)
                        select parent).FirstOrDefault();
                }

                if (Categories != null)
                {
                    parentCategory.Categories.Remove(category);

                    if (parentCategory.Categories.Count == 0)
                    {
                        Categories.Remove(parentCategory);
                    }
                }
            }

            ModListPreviewList.Remove(item);
        }

        /// <summary>
        /// Refreshes the view after a mod pack is deleted
        /// </summary>
        public void RemoveModPack()
        {
            SetFilter(ItemFilter ? "ItemFilter" : "ModPackFilter");
        }

        /// <summary>
        /// The text for the mod toggle button
        /// </summary>
        public string ModToggleText
        {
            get => _modToggleText;
            set
            {
                _modToggleText = value;
                OnPropertyChanged(nameof(ModToggleText));
            }
        }

        /// <summary>
        /// The visibility of the mod list item view
        /// </summary>
        public Visibility ListVisibility
        {
            get => _listVisibility;
            set
            {
                _listVisibility = value;
                OnPropertyChanged(nameof(ListVisibility));
            }
        }

        /// <summary>
        /// THe visibility of the info grid view
        /// </summary>
        public Visibility InfoGridVisibility
        {
            get => _infoGridVisibility;
            set
            {
                _infoGridVisibility = value;
                OnPropertyChanged(nameof(InfoGridVisibility));
            }
        }

        /// <summary>
        /// The mod pack title
        /// </summary>
        public string ModPackTitle
        {
            get => _modPackTitle;
            set
            {
                _modPackTitle = value;
                OnPropertyChanged(nameof(ModPackTitle));
            }
        }

        /// <summary>
        /// The label for the mod pack author in the info grid
        /// </summary>
        public string ModPackModAuthorLabel
        {
            get => _modPackModAuthorLabel;
            set
            {
                _modPackModAuthorLabel = value;
                OnPropertyChanged(nameof(ModPackModAuthorLabel));
            }
        }

        /// <summary>
        /// The label for the mod pack mod count in the info grid
        /// </summary>
        public string ModPackModCountLabel
        {
            get => _modPackModCountLabel;
            set
            {
                _modPackModCountLabel = value;
                OnPropertyChanged(nameof(ModPackModCountLabel));
            }
        }

        /// <summary>
        /// the label for the mod pack version in the info grid
        /// </summary>
        public string ModPackModVersionLabel
        {
            get => _modPackModVersionLabel;
            set
            {
                _modPackModVersionLabel = value;
                OnPropertyChanged(nameof(ModPackModVersionLabel));
            }
        }

        /// <summary>
        /// The content of the mod pack as a string
        /// </summary>
        public string ModPackContentList
        {
            get => _modPackContentList;
            set
            {
                _modPackContentList = value;
                OnPropertyChanged(nameof(ModPackContentList));
            }
        }

        /// <summary>
        /// The status of the item filter
        /// </summary>
        public bool ItemFilter
        {
            get => _itemFilter;
            set
            {                
                if (value && !_itemFilter)
                {
                    SetFilter("ItemFilter");
                }
                _itemFilter = value;
                OnPropertyChanged(nameof(ItemFilter));
            }
        }

        /// <summary>
        /// The status of the mod pack filter
        /// </summary>
        public bool ModPackFilter
        {
            get => _modPackFilter;
            set
            {                
                if (value && !_modPackFilter)
                {
                    SetFilter("ModPackFilter");
                }
                _modPackFilter = value;
                OnPropertyChanged(nameof(ModPackFilter));
            }
        }

        /// <summary>
        /// The status of the name sort
        /// </summary>
        public bool NameSort
        {
            get => _nameSort;
            set
            {                
                if (value && !_nameSort)
                {
                    if (_modPackFilter) SetFilter("ModPackFilter");
                    if (_itemFilter) SetFilter("ItemFilter");
                }
                _nameSort = value;
                OnPropertyChanged(nameof(NameSort));
            }
        }

        /// <summary>
        /// The status of the date sort
        /// </summary>
        public bool DateSort
        {
            get => _dateSort;
            set
            {               
                if (value && !_dateSort)
                {
                    if (_modPackFilter) SetFilter("ModPackFilter");
                    if (_itemFilter) SetFilter("ItemFilter");
                }
                _dateSort = value;
                OnPropertyChanged(nameof(DateSort));
            }
        }

        public int ProgressValue
        {
            get => _progressValue;
            set
            {
                _progressValue = value;
                OnPropertyChanged(nameof(ProgressValue));
            }
        }

        public string ProgressText
        {
            get => _progressText;
            set
            {
                _progressText = value;
                OnPropertyChanged(nameof(ProgressText));
            }
        }

        /// <summary>
        /// Sets the filter for the mod list treeview
        /// </summary>
        /// <param name="type">The type of the filter</param>
        private async void SetFilter(string type)
        {
            if (type.Equals("ItemFilter"))
            {
                await GetCategoriesItemFilter();
            }
            else if (type.Equals("ModPackFilter"))
            {
                await GetCategoriesModPackFilter();
            }
        }

        public void Dispose()
        {
            Categories = null;
            ModListPreviewList = null;

            string sortMethod;
            if (_nameSort)
                sortMethod = "NameSort";
            else
                sortMethod = "DateSort";

            string filter;
            if (_modPackFilter)
                filter = "ModPack";
            else
                filter = "Item";

            Properties.Settings.Default.ModList_Filter = filter;
            Properties.Settings.Default.ModList_Sorting = sortMethod;
            Properties.Settings.Default.Save();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public class ModListModel : INotifyPropertyChanged
        {
            private SolidColorBrush _active, _activeBorder;
            private float _opacity;

            /// <summary>
            /// The race of the modded item
            /// </summary>
            public string Race { get; set; }

            /// <summary>
            /// The texture map of the modded item
            /// </summary>
            public string Map { get; set; }

            /// <summary>
            /// The part of the modded item
            /// </summary>
            public string Part { get; set; }

            /// <summary>
            /// The number of the modded item
            /// </summary>
            public string Number { get; set; }

            /// <summary>
            /// The brush color reflecting the active status of the modded item
            /// </summary>
            public SolidColorBrush Active
            {
                get => _active;
                set
                {
                    _active = value;
                    OnPropertyChanged(nameof(Active));
                }
            }

            /// <summary>
            /// The opacity reflecting the active status of the modded item
            /// </summary>
            public float ActiveOpacity
            {
                get => _opacity;

                set
                {
                    _opacity = value;
                    OnPropertyChanged(nameof(ActiveOpacity));
                }
            }

            /// <summary>
            /// The border brush color reflecting the active status of the modded item
            /// </summary>
            public SolidColorBrush ActiveBorder
            {
                get => _activeBorder;
                set
                {
                    _activeBorder = value;
                    OnPropertyChanged(nameof(ActiveBorder));
                }
            }

            /// <summary>
            /// The mod item
            /// </summary>
            public Mod ModItem { get; set; }

            /// <summary>
            /// The image of the modded item
            /// </summary>
            public BitmapSource Image { get; set; }


            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        /// <summary>
        /// Gets the language for the application
        /// </summary>
        /// <returns>The application language as XivLanguage</returns>
        private static XivLanguage GetLanguage()
        {
            return XivLanguages.GetXivLanguage(Properties.Settings.Default.Application_Language);
        }
    }
}