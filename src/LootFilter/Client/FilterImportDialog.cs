using System;
using Newtonsoft.Json;
using Vintagestory.API.Client;

namespace LootFilter
{
    /// <summary>
    /// Modal dialog that accepts a pasted JSON filter config and merges it into
    /// the player's working copy.  Opened from the Settings tab via "Import".
    /// <para>
    /// Merge semantics: codes, keywords, and attribute rules are union-merged
    /// (duplicates skipped); bool toggles in the import replace the current values
    /// only when the player chooses "Replace All" via the confirm button label.
    /// The default "Merge" action preserves existing toggle states.
    /// </para>
    /// </summary>
    internal class FilterImportDialog : GuiDialog
    {
        public override string ToggleKeyCombinationCode => null!;

        // Callback invoked with the merged result when the player confirms.
        private readonly Action<LootFilterConfig, bool> onConfirm;

        // Current text in the paste area.
        private string pasteText = "";

        // Error message shown beneath the text area (empty = no error).
        private string errorMessage = "";

        // Guards SetValue() calls during recompose from re-triggering the handler.
        private bool isRecomposing;

        /// <param name="capi">Client API.</param>
        /// <param name="onConfirm">
        /// Invoked when the player confirms the import.
        /// First argument: the parsed <see cref="LootFilterConfig"/> from the pasted JSON.
        /// Second argument: <c>true</c> when the player chose "Replace All" (toggles
        /// and lists replaced wholesale), <c>false</c> for Merge (lists union-merged,
        /// toggles preserved).
        /// </param>
        public FilterImportDialog(ICoreClientAPI capi, Action<LootFilterConfig, bool> onConfirm)
            : base(capi)
        {
            this.onConfirm = onConfirm ?? throw new ArgumentNullException(nameof(onConfirm));
        }

        public override void OnGuiOpened()
        {
            base.OnGuiOpened();
            pasteText    = "";
            errorMessage = "";
            ComposeDialog();
        }

        // ── Composition ──────────────────────────────────────────────────

        private void ComposeDialog()
        {
            isRecomposing = true;

            var dialogBounds = ElementBounds
                .Fixed(0, 0, 480, 420)
                .WithAlignment(EnumDialogArea.CenterMiddle)
                .WithFixedAlignmentOffset(0, 0);

            var bgBounds = ElementBounds.Fill;

            // ── Text area for paste ──────────────────────────────────────
            var textAreaBounds = ElementBounds.Fixed(10, 40, 460, 260);

            // ── Error label ──────────────────────────────────────────────
            var errorBounds = ElementBounds.Fixed(10, 308, 460, 30);

            // ── Buttons ──────────────────────────────────────────────────
            var mergeBounds   = ElementBounds.Fixed(10,  350, 140, 30);
            var replaceBounds = ElementBounds.Fixed(165, 350, 140, 30);
            var cancelBounds  = ElementBounds.Fixed(330, 350, 140, 30);

            SingleComposer = capi.Gui
                .CreateCompo("lootfilterimport", dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar("Import Filter Config", () => TryClose())
                .AddTextArea(textAreaBounds, OnTextChanged, CairoFont.WhiteSmallText(), "importText")
                .AddStaticText(
                    string.IsNullOrEmpty(errorMessage) ? "" : errorMessage,
                    CairoFont.WhiteSmallText(),
                    errorBounds,
                    "errorLabel")
                .AddSmallButton("Merge",       () => { Confirm(replaceAll: false); return true; }, mergeBounds)
                .AddSmallButton("Replace All", () => { Confirm(replaceAll: true);  return true; }, replaceBounds)
                .AddSmallButton("Cancel",      () => { TryClose(); return true; },                 cancelBounds);

            SingleComposer.Compose();

            // Restore text after recompose.
            var ta = SingleComposer.GetTextArea("importText");
            if (ta != null && !string.IsNullOrEmpty(pasteText))
            {
                ta.SetValue(pasteText);
            }

            isRecomposing = false;
        }

        // ── Handlers ─────────────────────────────────────────────────────

        private void OnTextChanged(string text)
        {
            if (isRecomposing) return;
            pasteText = text ?? "";
        }

        private void Confirm(bool replaceAll)
        {
            if (string.IsNullOrWhiteSpace(pasteText))
            {
                SetError("Paste area is empty.");
                return;
            }

            LootFilterConfig? parsed;
            try
            {
                parsed = JsonConvert.DeserializeObject<LootFilterConfig>(pasteText);
            }
            catch (Exception ex)
            {
                SetError("Invalid JSON: " + ex.Message);
                return;
            }

            if (parsed == null)
            {
                SetError("JSON parsed to null — is this a valid filter config?");
                return;
            }

            TryClose();
            onConfirm(parsed, replaceAll);
        }

        private void SetError(string message)
        {
            errorMessage = message;
            // Recompose to refresh the error label.
            ComposeDialog();

            // Re-restore text after the recompose cleared the field.
            var ta = SingleComposer?.GetTextArea("importText");
            if (ta != null && !string.IsNullOrEmpty(pasteText))
            {
                isRecomposing = true;
                ta.SetValue(pasteText);
                isRecomposing = false;
            }
        }
    }
}
