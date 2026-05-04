using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace LootFilter
{
    /// <summary>
    /// Lightweight <see cref="GuiElement"/> that renders a single item-stack
    /// icon during the OpenGL interactive-render phase.  Uses GL scissor
    /// clipping to prevent oversized 3D models from overflowing into
    /// neighbouring rows.
    /// </summary>
    internal class GuiElementItemIcon : GuiElement
    {
        private readonly ItemSlot slot;
        private readonly float renderSize;

        /// <param name="capi">Client API.</param>
        /// <param name="stack">The item stack to render.  Must not be null.</param>
        /// <param name="bounds">Bounding box for the icon.</param>
        /// <param name="unscaledSize">
        /// Unscaled pixel size passed to <c>RenderItemstackToGui</c>.
        /// Smaller values produce smaller icons that are less likely to
        /// overflow.  Default 14 keeps most items within a 30×30 cell.
        /// </param>
        public GuiElementItemIcon(
            ICoreClientAPI capi, ItemStack stack, ElementBounds bounds, float unscaledSize = 14f)
            : base(capi, bounds)
        {
            var inv = new DummyInventory(capi, 1);
            inv[0].Itemstack = stack;
            slot = inv[0];
            renderSize = unscaledSize;
        }

        public override void RenderInteractiveElements(float deltaTime)
        {
            if (slot?.Itemstack?.Collectible == null) return;

            // ── GL scissor: clip to our bounds so oversized 3D models
            //    (planks, tools, weapons) don't bleed into adjacent rows.
            //    GL scissor uses window coords with origin at lower-left,
            //    while VS GUI uses origin at upper-left — hence the Y flip.
            int sx = (int)Bounds.renderX;
            int sw = (int)Bounds.OuterWidth;
            int sh = (int)Bounds.OuterHeight;
            int sy = api.Render.FrameHeight - (int)Bounds.renderY - sh;

            api.Render.GlScissor(sx, sy, sw, sh);
            api.Render.GlScissorFlag(true);

            api.Render.RenderItemstackToGui(
                slot,
                Bounds.renderX + Bounds.InnerWidth / 2,
                Bounds.renderY + Bounds.InnerHeight / 2,
                100,                                        // z-depth
                (float)GuiElement.scaled(renderSize),        // scaled size
                ColorUtil.WhiteArgb,                         // tint
                true,                                       // shading
                false,                                      // original rotation
                false                                       // show stack size
            );

            api.Render.GlScissorFlag(false);
        }
    }
}
