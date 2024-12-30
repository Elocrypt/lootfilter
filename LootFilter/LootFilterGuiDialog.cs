using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace lootfilter
{
    public class FilterGuiDialog : GuiDialog
    {
        private Action reloadConfigAction;
        private LootFilterConfig config;
        private List<CollectibleObject> cachedFilteredItems = new List<CollectibleObject>();
        private int activeTabIndex = 0;
        private int currentPageItems = 0;
        private int currentPageKeywords = 0;
        private int itemsPerPage = 11;
        private string searchQuery = "";
        private string currentSearchText = "";
        private string currentKeywordInput = "";
        private bool suppressNextInput = false;
        public FilterGuiDialog(ICoreClientAPI capi, LootFilterConfig config, Action reloadConfigWrapper) : base(capi)
        {
            this.capi = capi ?? throw new ArgumentNullException(nameof(capi));
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            reloadConfigAction = reloadConfigWrapper;
            config.OnConfigChanged += () =>
            {
                RefreshFilteredItems();
                ComposeDialog();
            };
        }
        public override string ToggleKeyCombinationCode => "lootfilter.toggle";
        public override void OnGuiOpened()
        {
            if (capi == null || capi.World == null || capi.Gui == null)
            {
                capi?.Logger?.Error("[Loot Filter] Critical error: Client API or World is not initialized.");
                return;
            }
            base.OnGuiOpened();
            suppressNextInput = true;
            currentPageItems = 0;
            currentPageKeywords = 0;
            searchQuery = "";
            currentSearchText = "";
            currentKeywordInput = "";
            try
            {
                reloadConfigAction?.Invoke();
                ComposeDialog();
                RefreshFilteredItems();
                AttachSearchEventHandler();
                var searchField = SingleComposer?.GetTextInput("searchField");
                if (searchField != null)
                {
                    searchField?.SetPlaceHolderText("Search... (Regex: /.../ or Text)");
                    searchField?.SetValue("");
                }
                var keywordField = SingleComposer?.GetTextInput("keywordField");
                if (keywordField != null)
                {
                    keywordField.SetValue("");
                    keywordField.SetPlaceHolderText("Add a keyword...");
                }
                if (!capi.IsGamePaused)
                {
                    capi.Event.RegisterCallback((dt) =>
                    {
                        if (suppressNextInput)
                        {
                            suppressNextInput = false;
                            searchField?.SetValue("");
                            keywordField?.SetValue("");
                        }
                    }, 50);
                }
                else
                {
                    capi.Logger.Warning("[Loot Filter] Callback not registered because the game is paused.");
                }
            }
            catch (Exception ex)
            {
                capi.Logger.Error("[Loot Filter] Error during OnGuiOpened: " + ex.Message);
                throw;
            }
        }
        private void AttachSearchEventHandler()
        {
            var searchField = SingleComposer?.GetTextInput("searchField");
            if (searchField != null)
            {
                searchField.OnTextChanged = HandleSearchTextChanged;
                searchField.SetValue(currentSearchText);
            }
        }
        private void HandleSearchTextChanged(string text)
        {
            searchQuery = text.Trim();
            RefreshFilteredItems();
            UpdateItemList();
        }
        private void RefreshFilteredItems()
        {
            if (capi.World?.Collectibles == null)
            {
                capi.Logger.Warning("[Loot Filter] No collectibles found. World collectibles are not loaded.");
                cachedFilteredItems.Clear();
                UpdateItemList();
                return;
            }

            Task.Run(() =>
            {
                List<CollectibleObject> filteredItems = new List<CollectibleObject>();

                try
                {
                    if (string.IsNullOrWhiteSpace(searchQuery))
                    {
                        filteredItems = capi.World.Collectibles.ToList();
                    }
                    else if (searchQuery.StartsWith("/") && searchQuery.EndsWith("/"))
                    {
                        string regexPattern = searchQuery.Trim('/');
                        var regex = new System.Text.RegularExpressions.Regex(regexPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                        filteredItems = capi.World.Collectibles
                            .Where(c => c.Code != null && regex.IsMatch(c.GetHeldItemName(new ItemStack(c))))
                            .ToList();
                    }
                    else
                    {
                        filteredItems = capi.World.Collectibles
                            .Where(c => c.Code != null && c.GetHeldItemName(new ItemStack(c))
                                .Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                    }
                }
                catch (Exception ex)
                {
                    capi.Logger.Error("[Loot Filter] Error during search: " + ex.Message);
                }

                capi.Event.EnqueueMainThreadTask(() =>
                {
                    cachedFilteredItems = filteredItems;
                    UpdateItemList();
                }, "UpdateFilteredItems");
            });
        }

        private void UpdateItemList()
        {
            if (SingleComposer == null) return;
            var itemsListBounds = ElementBounds.Fixed(10, 100, 430, 390);
            SingleComposer.BeginChildElements(itemsListBounds);
            int yOffset = 5;
            foreach (var item in cachedFilteredItems
                .Skip(currentPageItems * itemsPerPage)
                .Take(itemsPerPage))
            {
                string displayName = item.GetHeldItemName(new ItemStack(item));
                string itemCode = item.Code.ToString();
                SingleComposer.AddStaticText(
                    displayName,
                    CairoFont.WhiteSmallText(),
                    ElementBounds.Fixed(10, yOffset, 300, 30)
                );
                AddFilterSwitch(itemCode, yOffset);
                yOffset += 35;
            }
            SingleComposer.EndChildElements();
            SingleComposer.ReCompose();
        }
        private void ComposeDialog()
        {
            var dialogBounds = ElementBounds.Fixed(0, 0, 400, 550)
                .WithAlignment(EnumDialogArea.CenterMiddle)
                .WithFixedAlignmentOffset(0, 0);
            SingleComposer = capi.Gui.CreateCompo("lootfilterdialog", dialogBounds)
                .AddShadedDialogBG(ElementBounds.Fill)
                .AddDialogTitleBar("Loot Filter", OnTitleBarClose)
                .AddVerticalTabs(new GuiTab[]
                {
                    new GuiTab() { Name = "Items", DataInt = 0 },
                    new GuiTab() { Name = "Keywords", DataInt = 1 },
                }, ElementBounds.Fixed(-100, 35, 100, 300), OnTabChanged, "filterTabs");
            var verticalTabs = SingleComposer.GetVerticalTab("filterTabs");
            if (verticalTabs != null)
            {
                verticalTabs.SetValue(activeTabIndex, false);
            }
            if (activeTabIndex == 0)
            {
                ComposeItemsTab(ElementBounds.Fixed(10, 35, 430, 30));
            }
            else if (activeTabIndex == 1)
            {
                ComposeKeywordsTab(ElementBounds.Fixed(10, 35, 430, 30));
            }
            SingleComposer.Compose();
            RefreshFilteredItems();
        }
        private void ComposeItemsTab(ElementBounds elementBounds)
        {
            ElementBounds itemsListBounds = ElementBounds.Fixed(10, 100, 360, 390).WithFixedPadding(1);
            ElementBounds searchBounds = ElementBounds.Fixed(10, 35, 280, 30);
            SingleComposer
                .AddTextInput(searchBounds, OnSearchTextChanged, CairoFont.WhiteSmallText(), key: "searchField")
                .AddStaticText("Item Name", CairoFont.WhiteSmallText(), ElementBounds.Fixed(13, 75, 200, 20))
                .AddStaticText("Filter Item", CairoFont.WhiteSmallText(), ElementBounds.Fixed(293, 75, 100, 20))
                .AddInset(itemsListBounds, 3)
                .BeginClip(itemsListBounds);
            var itemsToDisplay = cachedFilteredItems
                .Skip(currentPageItems * itemsPerPage)
                .Take(itemsPerPage)
                .ToList();
            int yOffset = 5;
            foreach (var item in itemsToDisplay)
            {
                string displayName = item.GetHeldItemName(new ItemStack(item));
                string itemCode = item.Code.ToString();
                SingleComposer.AddStaticText(
                    displayName,
                    CairoFont.WhiteSmallText(),
                    ElementBounds.Fixed(10, yOffset, 300, 30)
                );
                AddFilterSwitch(itemCode, yOffset);
                yOffset += 35;
            }
            SingleComposer.EndClip();
            ElementBounds prevItemPageButtonBounds = ElementBounds.Fixed(10, 500, 80, 30);
            ElementBounds nextItemPageButtonBounds = ElementBounds.Fixed(292, 500, 80, 30);
            ElementBounds refreshItemButtonBounds = ElementBounds.Fixed(160, 500, 80, 30);
            SingleComposer.AddSmallButton(
                Lang.Get("◄──"),
                () =>
                {
                    if (currentPageItems > 0)
                    {
                        currentPageItems--;
                        RefreshFilteredItems();
                        ComposeDialog();
                    }
                    return true;
                },
                prevItemPageButtonBounds
            );
            SingleComposer.AddSmallButton(
                Lang.Get("Refresh"),
                () =>
                {
                    reloadConfigAction?.Invoke();
                    return true;
                },
                refreshItemButtonBounds
                );
            SingleComposer.AddSmallButton(
                Lang.Get("──►"),
                () =>
                {
                    if ((currentPageItems + 1) * itemsPerPage < cachedFilteredItems.Count)
                    {
                        currentPageItems++;
                        RefreshFilteredItems();
                        ComposeDialog();
                    }
                    return true;
                },
                nextItemPageButtonBounds
            );
            SingleComposer.GetTextInput("searchField").SetPlaceHolderText("Search... (Regex: /.../ or Text)");
            SingleComposer.GetTextInput("searchField").SetValue(currentSearchText);
            SingleComposer.FocusElement(SingleComposer.GetTextInput("searchField").TabIndex);
            RefreshFilteredItems();
        }
        private void ComposeKeywordsTab(ElementBounds elementBounds)
        {
            ElementBounds keywordListBounds = ElementBounds.Fixed(10, 100, 320, 390).WithFixedPadding(1);
            ElementBounds searchBounds = ElementBounds.Fixed(10, 35, 320, 30);
            ElementBounds addBounds = ElementBounds.Fixed(335, 35, 50, 30);
            SingleComposer
                .AddStaticText("Keywords", CairoFont.WhiteSmallText(), ElementBounds.Fixed(13, 75, 200, 20))
                .AddStaticText("Remove", CairoFont.WhiteSmallText(), ElementBounds.Fixed(263, 75, 100, 20))
                .AddTextInput(searchBounds, OnKeywordInputChanged, CairoFont.WhiteSmallText(), key: "keywordField")
                .AddSmallButton(Lang.Get("Add"), OnAddKeyword, addBounds)
                .AddInset(keywordListBounds, 3)
                .BeginClip(keywordListBounds);
            int yOffset = 5;
            var keywordsToDisplay = config.FilteredKeywords
                .Skip(currentPageKeywords * itemsPerPage)
                .Take(itemsPerPage)
                .ToList();
            foreach (var keyword in keywordsToDisplay)
            {
                ElementBounds textBounds = ElementBounds.Fixed(10, yOffset, 300, 30);
                ElementBounds buttonBounds = ElementBounds.Fixed(260, yOffset, 40, 25);
                SingleComposer.AddStaticText(keyword, CairoFont.WhiteSmallText(), textBounds);
                SingleComposer.AddSmallButton(Lang.Get("X"), () => { RemoveKeyword(keyword); return true; }, buttonBounds);
                yOffset += 35;
            }
            SingleComposer.EndClip();
            ElementBounds prevKeywordPageButtonBounds = ElementBounds.Fixed(10, 500, 80, 30);
            ElementBounds nextKeywordPageButtonBounds = ElementBounds.Fixed(253, 500, 80, 30);
            ElementBounds refreshKeywordButtonBounds = ElementBounds.Fixed(131, 500, 80, 30);
            SingleComposer.AddSmallButton(
                Lang.Get("◄──"),
                () =>
                {
                    if (currentPageKeywords > 0)
                    {
                        currentPageKeywords--;
                        ComposeDialog();
                    }
                    return true;
                },
                prevKeywordPageButtonBounds
            );
            SingleComposer.AddSmallButton(
                Lang.Get("Refresh"),
                () =>
                {
                    reloadConfigAction?.Invoke();
                    return true;
                },
                refreshKeywordButtonBounds
                );
            SingleComposer.AddSmallButton(
                Lang.Get("──►"),
                () =>
                {
                    if ((currentPageKeywords + 1) * itemsPerPage < config.FilteredKeywords.Count)
                    {
                        currentPageKeywords++;
                        ComposeDialog();
                    }
                    return true;
                },
                nextKeywordPageButtonBounds
            );
            var keywordField = SingleComposer.GetTextInput("keywordField");
            if (keywordField != null)
            {
                keywordField.SetPlaceHolderText("Add a keyword...");
                keywordField.SetValue(currentKeywordInput);
            }
        }
        private void OnTabChanged(int index, GuiTab tab)
        {
            activeTabIndex = index;
            var verticalTabs = SingleComposer.GetVerticalTab("filterTabs");
            if (verticalTabs != null)
            {
                verticalTabs.SetValue(activeTabIndex, false);
            }
            if (index == 0)
            {
                currentPageItems = 0;
            }
            else if (index == 1)
            {
                currentPageKeywords = 0;
            }
            ComposeDialog();
        }
        private void AddFilterSwitch(string itemCode, int yOffset)
        {
            bool isFiltered = config.FilteredItemCodes.Contains(itemCode);
            SingleComposer.AddSwitch(
                (toggleState) =>
                {
                    if (toggleState)
                    {
                        if (!config.FilteredItemCodes.Contains(itemCode))
                        {
                            config.FilteredItemCodes.Add(itemCode);
                            SaveConfig();
                        }
                    }
                    else
                    {
                        config.FilteredItemCodes.Remove(itemCode);
                        SaveConfig();
                    }
                },
                ElementBounds.Fixed(320, yOffset, 100, 30),
                $"{itemCode}-filter-switch",
                30,
                5
            );
            var switchElement = SingleComposer.GetSwitch($"{itemCode}-filter-switch");
            if (switchElement != null)
            {
                switchElement.SetValue(isFiltered);
            }
        }
        private void OnKeywordInputChanged(string text)
        {
            currentKeywordInput = text.Trim();
        }
        private bool OnAddKeyword()
        {
            if (!string.IsNullOrEmpty(currentKeywordInput) && !config.FilteredKeywords.Contains(currentKeywordInput))
            {
                config.FilteredKeywords.Add(currentKeywordInput);
                SaveConfig();
                currentKeywordInput = "";
                var keywordField = SingleComposer.GetTextInput("keywordField");
                if (keywordField != null)
                {
                    keywordField.SetValue("");
                }
                ComposeDialog();
                return true;
            }
            capi.Logger.Warning("[Loot Filter] Keyword not added. Either it's empty or already exists.");
            return false;
        }
        private void RemoveKeyword(string keyword)
        {
            config.FilteredKeywords.Remove(keyword);
            SaveConfig();
            ComposeDialog();
        }
        private void OnSearchTextChanged(string text)
        {
            var searchField = SingleComposer?.GetTextInput("searchField");
            if (searchField != null)
            {
                // Save the current cursor position
                int cursorPosition = searchField.CaretPosInLine;

                if (text == currentSearchText) return;

                currentSearchText = text;
                searchQuery = text.Trim();
                RefreshFilteredItems();
                ComposeDialog();
                cursorPosition = Math.Min(cursorPosition, currentSearchText.Length);

                // Restore the cursor position after processing
                searchField.CaretPosInLine = cursorPosition;
                SingleComposer?.FocusElement(searchField.TabIndex);
            }
        }
        public void UpdateConfig(LootFilterConfig newConfig)
        {
            config = newConfig ?? throw new ArgumentNullException(nameof(newConfig));
            RefreshFilteredItems();
            ComposeDialog();
        }
        private void SaveConfig()
        {
            string configPath = System.IO.Path.Combine(GamePaths.ModConfig, "lootfilterconfig.json");
            System.IO.File.WriteAllText(configPath, Newtonsoft.Json.JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented));
        }
        private void OnTitleBarClose()
        {
            TryClose();
        }
        public override bool PrefersUngrabbedMouse => true;
    }
}