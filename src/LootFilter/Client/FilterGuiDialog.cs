using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace LootFilter
{
    /// <summary>
    /// Main client-side GUI for the loot filter.
    /// Reads from <see cref="LootFilterMod.ClientMirror"/> (server-synced)
    /// and pushes changes via <see cref="LootFilterMod.SendConfigToServer"/>.
    /// Three tabs: Items, Keywords, Settings.
    /// </summary>
    internal class FilterGuiDialog : GuiDialog
    {
        private readonly LootFilterMod mod;

        // ── Working state ────────────────────────────────────────────────
        private LootFilterConfig workingCopy = new LootFilterConfig();

        // ── Tab state ────────────────────────────────────────────────────
        private int activeTabIndex;

        // ── Items tab ────────────────────────────────────────────────────
        private const int ItemsPerPage = 12;
        private string searchQuery = "";
        private string currentSearchText = "";
        private int currentPageItems;
        private List<CollectibleObject> cachedFilteredItems = new List<CollectibleObject>();
        private int searchGeneration;
        private int sendGeneration;

        // ── Keywords tab ─────────────────────────────────────────────────
        private string currentKeywordInput = "";
        private int currentPageKeywords;

        // ── Recompose guard ──────────────────────────────────────────────
        // Prevents SetValue() on the search field from re-triggering the
        // search handler → refresh → recompose → SetValue → infinite loop.
        private bool isRecomposing;

        // ── Input suppression ────────────────────────────────────────────
        private bool suppressNextInput;

        public override string ToggleKeyCombinationCode => "lootfilter.toggle";
        public override bool PrefersUngrabbedMouse => true;

        // ─────────────────────────────────────────────────────────────────
        //  Construction
        // ─────────────────────────────────────────────────────────────────

        public FilterGuiDialog(ICoreClientAPI capi, LootFilterMod mod)
            : base(capi)
        {
            this.mod = mod ?? throw new ArgumentNullException(nameof(mod));
        }

        // ─────────────────────────────────────────────────────────────────
        //  Open / Close
        // ─────────────────────────────────────────────────────────────────

        public override void OnGuiOpened()
        {
            base.OnGuiOpened();

            workingCopy = CloneConfig(mod.ClientMirror);

            currentPageItems = 0;
            currentPageKeywords = 0;
            searchQuery = "";
            currentSearchText = "";
            currentKeywordInput = "";
            suppressNextInput = true;

            // Build the initial item list synchronously so we don't
            // immediately recompose (and steal focus) when the background
            // thread completes 50 ms later.
            BuildFilteredItemsSync("");

            ComposeDialog();
            ApplyPostComposeStates();

            // Clear the tilde character that leaks into the text field
            // from the hotkey that opened the dialog.
            capi.Event.RegisterCallback(_ =>
            {
                if (!suppressNextInput) return;
                suppressNextInput = false;
                ClearTextInputSafe("searchField");
                ClearTextInputSafe("keywordField");
            }, 60);
        }

        public void OnMirrorUpdated()
        {
            if (!IsOpened()) return;

            workingCopy = CloneConfig(mod.ClientMirror);

            if (activeTabIndex == 0)
                BuildFilteredItemsSync(searchQuery);

            ComposeDialog();
            ApplyPostComposeStates();
        }

        // ─────────────────────────────────────────────────────────────────
        //  Dialog composition
        // ─────────────────────────────────────────────────────────────────

        private void ComposeDialog()
        {
            isRecomposing = true;

            var dialogBounds = ElementBounds
                .Fixed(0, 0, 420, 580)
                .WithAlignment(EnumDialogArea.CenterMiddle)
                .WithFixedAlignmentOffset(0, 0);

            var bgBounds = ElementBounds.Fill;

            var tabs = new GuiTab[]
            {
                new GuiTab { Name = "Items",    DataInt = 0 },
                new GuiTab { Name = "Keywords", DataInt = 1 },
                new GuiTab { Name = "Settings", DataInt = 2 }
            };

            var tabBounds = ElementBounds.Fixed(-110, 35, 110, 400);

            SingleComposer = capi.Gui
                .CreateCompo("lootfilterdialog", dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar("Loot Filter", () => TryClose())
                .AddVerticalTabs(tabs, tabBounds, OnTabChanged, "filterTabs");

            SingleComposer.GetVerticalTab("filterTabs")
                ?.SetValue(activeTabIndex, triggerHandler: false);

            switch (activeTabIndex)
            {
                case 0: ComposeItemsTab(); break;
                case 1: ComposeKeywordsTab(); break;
                case 2: ComposeSettingsTab(); break;
            }

            SingleComposer.Compose();
            isRecomposing = false;
        }

        // ── Items tab ────────────────────────────────────────────────────

        private void ComposeItemsTab()
        {
            var searchBounds   = ElementBounds.Fixed(10, 40, 290, 30);
            var headerName     = ElementBounds.Fixed(48, 80, 200, 20);
            var headerToggle   = ElementBounds.Fixed(310, 80, 100, 20);
            var listInset      = ElementBounds.Fixed(10, 105, 395, 410).WithFixedPadding(1);

            SingleComposer
                .AddTextInput(searchBounds, OnSearchTextChangedInternal,
                              CairoFont.WhiteSmallText(), "searchField")
                .AddStaticText("Item Name", CairoFont.WhiteSmallText(), headerName)
                .AddStaticText("Filter", CairoFont.WhiteSmallText(), headerToggle)
                .AddInset(listInset, 3)
                .BeginClip(listInset);

            var page = GetItemsPage();
            int y = 5;
            for (int i = 0; i < page.Count; i++)
            {
                var item = page[i];
                string code = item.Code.ToString();
                string displayName;
                try { displayName = item.GetHeldItemName(new ItemStack(item)); }
                catch { displayName = code; }

                // Item icon — rendered during the GL phase by our custom element.
                try
                {
                    var iconStack = new ItemStack(item, 1);
                    var iconBounds = ElementBounds.Fixed(5, y, 30, 30);
                    SingleComposer.AddInteractiveElement(
                        new GuiElementItemIcon(capi, iconStack, iconBounds),
                        "itemIcon-" + i);
                }
                catch
                {
                    // Some collectibles can't create valid stacks (e.g. air).
                    // Skip the icon; the name is still shown.
                }

                SingleComposer.AddStaticText(
                    displayName, CairoFont.WhiteSmallText(),
                    ElementBounds.Fixed(40, y + 5, 255, 25));

                string capturedCode = code;
                SingleComposer.AddSwitch(
                    state => OnItemToggled(capturedCode, state),
                    ElementBounds.Fixed(330, y, 40, 30),
                    "itemSwitch-" + i, 30, 5);

                y += 33;
            }

            SingleComposer.EndClip();

            // Pagination.
            var prevBounds = ElementBounds.Fixed(10, 525, 80, 30);
            var pageBounds = ElementBounds.Fixed(140, 528, 140, 25);
            var nextBounds = ElementBounds.Fixed(315, 525, 80, 30);

            int totalPages = Math.Max(1, (cachedFilteredItems.Count + ItemsPerPage - 1) / ItemsPerPage);
            string pageLabel = $"Page {currentPageItems + 1} of {totalPages}";

            SingleComposer
                .AddSmallButton("◄", () => { PageItems(-1); return true; }, prevBounds)
                .AddStaticText(pageLabel, CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center), pageBounds)
                .AddSmallButton("►", () => { PageItems(1); return true; }, nextBounds);

            // Restore search text and placeholder.
            var searchField = SingleComposer.GetTextInput("searchField");
            if (searchField != null)
            {
                searchField.SetPlaceHolderText("Search... (Regex: /.../ or plain text)");
                if (!string.IsNullOrEmpty(currentSearchText))
                    searchField.SetValue(currentSearchText);
            }
        }

        private void ApplyItemSwitchStates()
        {
            var page = GetItemsPage();
            for (int i = 0; i < page.Count; i++)
            {
                string code = page[i].Code.ToString();
                string switchKey = "itemSwitch-" + i;
                var sw = SingleComposer?.GetSwitch(switchKey);
                if (sw != null)
                    sw.SetValue(workingCopy.FilteredItemCodes.Contains(code));
            }
        }

        private void OnItemToggled(string code, bool state)
        {
            if (state)
            {
                if (!workingCopy.FilteredItemCodes.Contains(code))
                    workingCopy.FilteredItemCodes.Add(code);
            }
            else
            {
                workingCopy.FilteredItemCodes.Remove(code);
            }
            QueueSendUpdate();
        }

        private List<CollectibleObject> GetItemsPage()
        {
            int skip = currentPageItems * ItemsPerPage;
            int count = Math.Min(ItemsPerPage, Math.Max(0, cachedFilteredItems.Count - skip));
            if (count <= 0) return new List<CollectibleObject>();
            return cachedFilteredItems.GetRange(skip, count);
        }

        private void PageItems(int delta)
        {
            int totalPages = Math.Max(1, (cachedFilteredItems.Count + ItemsPerPage - 1) / ItemsPerPage);
            int next = currentPageItems + delta;
            if (next < 0 || next >= totalPages) return;
            currentPageItems = next;
            ComposeDialog();
            ApplyPostComposeStates();
        }

        // ── Keywords tab ─────────────────────────────────────────────────

        private void ComposeKeywordsTab()
        {
            var inputBounds  = ElementBounds.Fixed(10, 40, 300, 30);
            var addBtnBounds = ElementBounds.Fixed(315, 40, 50, 30);
            var headerKw     = ElementBounds.Fixed(13, 80, 200, 20);
            var headerRemove = ElementBounds.Fixed(320, 80, 80, 20);
            var listInset    = ElementBounds.Fixed(10, 105, 395, 410).WithFixedPadding(1);

            SingleComposer
                .AddTextInput(inputBounds, OnKeywordInputChanged,
                              CairoFont.WhiteSmallText(), "keywordField")
                .AddSmallButton("Add", () => { AddKeyword(); return true; }, addBtnBounds)
                .AddStaticText("Keyword", CairoFont.WhiteSmallText(), headerKw)
                .AddStaticText("Remove", CairoFont.WhiteSmallText(), headerRemove)
                .AddInset(listInset, 3)
                .BeginClip(listInset);

            int skip = currentPageKeywords * ItemsPerPage;
            int count = Math.Min(ItemsPerPage, Math.Max(0, workingCopy.FilteredKeywords.Count - skip));
            int y = 5;

            for (int i = 0; i < count; i++)
            {
                int idx = skip + i;
                if (idx >= workingCopy.FilteredKeywords.Count) break;
                string kw = workingCopy.FilteredKeywords[idx];

                SingleComposer.AddStaticText(
                    kw, CairoFont.WhiteSmallText(),
                    ElementBounds.Fixed(10, y, 280, 30));

                string capturedKw = kw;
                SingleComposer.AddSmallButton("X", () =>
                {
                    RemoveKeyword(capturedKw);
                    return true;
                }, ElementBounds.Fixed(325, y, 40, 25));

                y += 33;
            }

            SingleComposer.EndClip();

            var prevBounds = ElementBounds.Fixed(10, 525, 80, 30);
            var pageBounds = ElementBounds.Fixed(140, 528, 140, 25);
            var nextBounds = ElementBounds.Fixed(315, 525, 80, 30);

            int totalPages = Math.Max(1, (workingCopy.FilteredKeywords.Count + ItemsPerPage - 1) / ItemsPerPage);
            string pageLabel = $"Page {currentPageKeywords + 1} of {totalPages}";

            SingleComposer
                .AddSmallButton("◄", () => { PageKeywords(-1); return true; }, prevBounds)
                .AddStaticText(pageLabel, CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center), pageBounds)
                .AddSmallButton("►", () => { PageKeywords(1); return true; }, nextBounds);

            var kwField = SingleComposer.GetTextInput("keywordField");
            if (kwField != null)
            {
                kwField.SetPlaceHolderText("Add a keyword...");
                if (!string.IsNullOrEmpty(currentKeywordInput))
                    kwField.SetValue(currentKeywordInput);
            }
        }

        private void OnKeywordInputChanged(string text)
        {
            if (isRecomposing) return;
            currentKeywordInput = text?.Trim() ?? "";
        }

        private void AddKeyword()
        {
            if (string.IsNullOrWhiteSpace(currentKeywordInput)) return;
            if (workingCopy.FilteredKeywords.Contains(currentKeywordInput)) return;

            workingCopy.FilteredKeywords.Add(currentKeywordInput);
            currentKeywordInput = "";
            mod.SendConfigToServer(workingCopy);
            ComposeDialog();
        }

        private void RemoveKeyword(string kw)
        {
            workingCopy.FilteredKeywords.Remove(kw);
            mod.SendConfigToServer(workingCopy);
            ComposeDialog();
        }

        private void PageKeywords(int delta)
        {
            int totalPages = Math.Max(1, (workingCopy.FilteredKeywords.Count + ItemsPerPage - 1) / ItemsPerPage);
            int next = currentPageKeywords + delta;
            if (next < 0 || next >= totalPages) return;
            currentPageKeywords = next;
            ComposeDialog();
        }

        // ── Settings tab ─────────────────────────────────────────────────

        private void ComposeSettingsTab()
        {
            int y = 50;
            int labelX = 10;
            int switchX = 310;
            int rowH = 45;

            SingleComposer
                .AddStaticText("Trash-on-Sight", CairoFont.WhiteSmallText(),
                    ElementBounds.Fixed(labelX, y, 280, 30))
                .AddHoverText("Auto-drop filtered items from your inventory",
                    CairoFont.WhiteSmallText(), 260,
                    ElementBounds.Fixed(labelX, y, 280, 30))
                .AddSwitch(state =>
                {
                    workingCopy.AutoDropFiltered = state;
                    mod.SendConfigToServer(workingCopy);
                }, ElementBounds.Fixed(switchX, y, 40, 30), "switchAutoDrop", 30, 5);

            y += rowH;

            SingleComposer
                .AddStaticText("Allowlist Mode", CairoFont.WhiteSmallText(),
                    ElementBounds.Fixed(labelX, y, 280, 30))
                .AddHoverText("Only pick up items on the list; block everything else",
                    CairoFont.WhiteSmallText(), 260,
                    ElementBounds.Fixed(labelX, y, 280, 30))
                .AddSwitch(state =>
                {
                    workingCopy.AllowlistMode = state;
                    mod.SendConfigToServer(workingCopy);
                }, ElementBounds.Fixed(switchX, y, 40, 30), "switchAllowlist", 30, 5);

            y += rowH;

            SingleComposer
                .AddStaticText("Crouch to bypass filter", CairoFont.WhiteSmallText(),
                    ElementBounds.Fixed(labelX, y, 280, 30))
                .AddHoverText("Hold Sneak to pick up everything regardless of filter",
                    CairoFont.WhiteSmallText(), 260,
                    ElementBounds.Fixed(labelX, y, 280, 30))
                .AddSwitch(state =>
                {
                    workingCopy.CrouchBypassEnabled = state;
                    mod.SendConfigToServer(workingCopy);
                }, ElementBounds.Fixed(switchX, y, 40, 30), "switchCrouch", 30, 5);

            y += rowH + 20;

            SingleComposer.AddSmallButton("Export to Chat", () =>
            {
                ExportToChat();
                return true;
            }, ElementBounds.Fixed(10, y, 150, 30));
        }

        private void ApplySettingsSwitchStates()
        {
            SingleComposer?.GetSwitch("switchAutoDrop")?.SetValue(workingCopy.AutoDropFiltered);
            SingleComposer?.GetSwitch("switchAllowlist")?.SetValue(workingCopy.AllowlistMode);
            SingleComposer?.GetSwitch("switchCrouch")?.SetValue(workingCopy.CrouchBypassEnabled);
        }

        private void ExportToChat()
        {
            try
            {
                string json = JsonConvert.SerializeObject(workingCopy, Formatting.Indented);
                capi.ShowChatMessage("[LootFilter] Current config:\n" + json);
            }
            catch (Exception ex)
            {
                capi.ShowChatMessage("[LootFilter] Export failed: " + ex.Message);
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  Tab switching
        // ─────────────────────────────────────────────────────────────────

        private void OnTabChanged(int index, GuiTab tab)
        {
            activeTabIndex = index;

            if (index == 0) currentPageItems = 0;
            if (index == 1) currentPageKeywords = 0;

            ComposeDialog();
            ApplyPostComposeStates();

            if (index == 0)
                BuildFilteredItemsSync(searchQuery);
        }

        // ─────────────────────────────────────────────────────────────────
        //  Search & item list refresh
        // ─────────────────────────────────────────────────────────────────

        private void OnSearchTextChangedInternal(string text)
        {
            // Guard: ignore events fired by our own SetValue() during recompose.
            if (isRecomposing) return;

            if (suppressNextInput)
            {
                suppressNextInput = false;
                ClearTextInputSafe("searchField");
                return;
            }

            currentSearchText = text ?? "";
            searchQuery = currentSearchText.Trim();

            // Debounce: only refresh 300ms after the last keystroke.
            int gen = ++searchGeneration;
            capi.Event.RegisterCallback(_ =>
            {
                if (gen != searchGeneration) return;
                RefreshFilteredItemsAsync();
            }, 300);
        }

        /// <summary>
        /// Builds the filtered item list synchronously on the main thread.
        /// Used on open and tab switch to avoid a deferred recompose that
        /// would steal focus from the search field.
        /// </summary>
        private void BuildFilteredItemsSync(string query)
        {
            cachedFilteredItems = FilterCollectibles(query);
            ClampItemsPage();
        }

        /// <summary>
        /// Runs the search on a background thread, then enqueues a
        /// recompose on the main thread.  Used for debounced search-as-
        /// you-type.
        /// </summary>
        private void RefreshFilteredItemsAsync()
        {
            if (capi.World?.Collectibles == null)
            {
                cachedFilteredItems.Clear();
                RecomposeItemsIfActive();
                return;
            }

            string query = searchQuery;

            Task.Run(() =>
            {
                var results = FilterCollectibles(query);

                capi.Event.EnqueueMainThreadTask(() =>
                {
                    cachedFilteredItems = results;
                    ClampItemsPage();
                    RecomposeItemsIfActive();
                }, "LootFilterRefresh");
            });
        }

        /// <summary>
        /// Pure filtering logic — safe to call from any thread.
        /// Returns a new list of collectibles matching <paramref name="query"/>.
        /// </summary>
        private List<CollectibleObject> FilterCollectibles(string query)
        {
            if (capi.World?.Collectibles == null)
                return new List<CollectibleObject>();

            try
            {
                if (string.IsNullOrWhiteSpace(query))
                    return new List<CollectibleObject>(capi.World.Collectibles);

                if (query.StartsWith("/") && query.EndsWith("/") && query.Length > 2)
                {
                    string pattern = query.Substring(1, query.Length - 2);
                    var rx = new Regex(pattern, RegexOptions.IgnoreCase);
                    var list = new List<CollectibleObject>();
                    var collectibles = capi.World.Collectibles;
                    for (int i = 0; i < collectibles.Count; i++)
                    {
                        var c = collectibles[i];
                        if (c?.Code == null) continue;
                        try
                        {
                            string name = c.GetHeldItemName(new ItemStack(c));
                            if (rx.IsMatch(name)) list.Add(c);
                        }
                        catch { }
                    }
                    return list;
                }
                else
                {
                    var list = new List<CollectibleObject>();
                    var collectibles = capi.World.Collectibles;
                    for (int i = 0; i < collectibles.Count; i++)
                    {
                        var c = collectibles[i];
                        if (c?.Code == null) continue;
                        try
                        {
                            string name = c.GetHeldItemName(new ItemStack(c));
                            if (name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                                list.Add(c);
                        }
                        catch { }
                    }
                    return list;
                }
            }
            catch
            {
                return new List<CollectibleObject>();
            }
        }

        private void ClampItemsPage()
        {
            int totalPages = Math.Max(1, (cachedFilteredItems.Count + ItemsPerPage - 1) / ItemsPerPage);
            if (currentPageItems >= totalPages)
                currentPageItems = totalPages - 1;
        }

        private void RecomposeItemsIfActive()
        {
            if (activeTabIndex != 0) return;
            ComposeDialog();
            ApplyPostComposeStates();

            // The recompose created a new search field that isn't focused.
            // Schedule a focus-restore so the user can keep typing without
            // having to re-click the field after every result update.
            if (!string.IsNullOrEmpty(currentSearchText))
            {
                capi.Event.RegisterCallback(_ =>
                {
                    SingleComposer?.GetTextInput("searchField")?.OnFocusGained();
                }, 20);
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  Post-compose switch state application
        // ─────────────────────────────────────────────────────────────────

        private void ApplyPostComposeStates()
        {
            switch (activeTabIndex)
            {
                case 0: ApplyItemSwitchStates(); break;
                case 2: ApplySettingsSwitchStates(); break;
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  Debounced send (item toggles batch rapid clicks)
        // ─────────────────────────────────────────────────────────────────

        private void QueueSendUpdate()
        {
            int gen = ++sendGeneration;
            capi.Event.RegisterCallback(_ =>
            {
                if (gen != sendGeneration) return;
                mod.SendConfigToServer(workingCopy);
            }, 300);
        }

        // ─────────────────────────────────────────────────────────────────
        //  Utilities
        // ─────────────────────────────────────────────────────────────────

        private void ClearTextInputSafe(string key)
        {
            var field = SingleComposer?.GetTextInput(key);
            if (field != null)
            {
                isRecomposing = true;
                field.SetValue("");
                isRecomposing = false;
            }
        }

        private static LootFilterConfig CloneConfig(LootFilterConfig src)
        {
            if (src == null) return new LootFilterConfig();
            return new LootFilterConfig
            {
                FilteredItemCodes   = new List<string>(src.FilteredItemCodes),
                FilteredKeywords    = new List<string>(src.FilteredKeywords),
                AutoDropFiltered    = src.AutoDropFiltered,
                AllowlistMode       = src.AllowlistMode,
                CrouchBypassEnabled = src.CrouchBypassEnabled
            };
        }
    }
}
