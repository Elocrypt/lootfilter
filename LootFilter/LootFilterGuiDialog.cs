using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace lootfilter
{
    public class FilterGuiDialog : GuiDialog
    {
        private LootFilterConfig config;
        private int itemsPerPage = 10;
        private int currentPage = 0;
        private string searchQuery = "";
        private List<CollectibleObject> cachedFilteredItems = new List<CollectibleObject>();
        private string currentSearchText = "";
        private bool suppressNextInput = false;

        public FilterGuiDialog(ICoreClientAPI capi, LootFilterConfig config) : base(capi)
        {
            this.capi = capi ?? throw new ArgumentNullException(nameof(capi));
            this.config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public override string ToggleKeyCombinationCode => "lootfilter.toggle";

        public override void OnGuiOpened()
        {
            base.OnGuiOpened();
            suppressNextInput = true;
            currentPage = 0;
            searchQuery = "";
            RefreshFilteredItems();
            ComposeDialog();

            var searchField = SingleComposer.GetTextInput("searchField");
            searchField.SetPlaceHolderText("Search...");
            searchField.SetValue("");

            capi.Event.RegisterCallback((dt) =>
            {
                if (suppressNextInput)
                {
                    suppressNextInput = false;
                    searchField.SetValue("");
                }
            }, 50);
        }

        private void RefreshFilteredItems()
        {
            Task.Run(() =>
            {
                var filteredItems = capi.World.Collectibles
                    .Where(c => c.Code != null &&
                                (string.IsNullOrEmpty(searchQuery) ||
                                 c.GetHeldItemName(new ItemStack(c))
                                     .Contains(searchQuery, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                capi.Event.EnqueueMainThreadTask(() =>
                {
                    cachedFilteredItems = filteredItems;
                    ComposeDialog();
                }, "UpdateFilteredItems");
            });
        }

        private void ComposeDialog()
        {
            var dialogBounds = ElementBounds.Fixed(0, 0, 450, 550)
                .WithAlignment(EnumDialogArea.CenterMiddle)
                .WithFixedAlignmentOffset(0, 0);

            var searchBounds = ElementBounds.Fixed(10, 35, 430, 30);
            var itemsListBounds = ElementBounds.Fixed(10, 100, 430, 390);
            var itemsTitleBounds = ElementBounds.Fixed(10, 75, 200, 20);
            var toggleTitleBounds = ElementBounds.Fixed(320, 75, 100, 20);


            SingleComposer = capi.Gui.CreateCompo("lootfilterdialog", dialogBounds)
                .AddShadedDialogBG(ElementBounds.Fill)
                .AddDialogTitleBar("Item Filter", OnTitleBarClose)
                .AddTextInput(
                    searchBounds,
                    OnSearchTextChanged,
                    CairoFont.WhiteSmallText(),
                    key: "searchField"
                )
                .AddStaticText("Items", CairoFont.WhiteSmallText(), itemsTitleBounds)
                .AddStaticText("Toggle", CairoFont.WhiteSmallText(), toggleTitleBounds)
                .AddInset(itemsListBounds, 3)
                .BeginClip(itemsListBounds);

            var itemsToDisplay = cachedFilteredItems
                .Skip(currentPage * itemsPerPage)
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

            SingleComposer.AddButton(
                "Previous Page",
                () =>
                {
                    if (currentPage > 0)
                    {
                        currentPage--;
                        RefreshFilteredItems();
                        ComposeDialog();
                    }
                    return true;
                },
                ElementBounds.Fixed(10, 500, 150, 30)
            );

            SingleComposer.AddButton(
                "Next Page",
                () =>
                {
                    if ((currentPage + 1) * itemsPerPage < cachedFilteredItems.Count)
                    {
                        currentPage++;
                        RefreshFilteredItems();
                        ComposeDialog();
                    }
                    return true;
                },
                ElementBounds.Fixed(290, 500, 150, 30)
            );

            SingleComposer.Compose();

            SingleComposer.GetTextInput("searchField").SetPlaceHolderText("Search...");
            SingleComposer.GetTextInput("searchField").SetValue(currentSearchText);
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

        private void OnSearchTextChanged(string text)
        {
            if (text == currentSearchText) return;

            currentSearchText = text;
            searchQuery = text.Trim();

            RefreshFilteredItems();
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