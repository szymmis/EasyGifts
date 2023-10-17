using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;

namespace EasyGifts
{
    internal class GiftLoversTooltipModule : IDisposable
    {
        private readonly IModHelper _helper;
        private readonly PerScreen<Item> _hoverItem = new();
        private readonly PerScreen<CraftingRecipe> _hoverRecipe = new();
        private readonly List<NPC> _characters = new();

        public GiftLoversTooltipModule(IModHelper helper)
        {
            _helper = helper;

            _helper.Events.Display.Rendering += OnRendering;
            _helper.Events.Display.RenderedHud += OnRenderedHud;
            _helper.Events.Display.Rendered += OnRendered;
            _helper.Events.GameLoop.DayStarted += OnDayStarted;
        }

        public void Dispose()
        {
            _helper.Events.Display.Rendering -= OnRendering;
            _helper.Events.Display.RenderedHud -= OnRenderedHud;
            _helper.Events.Display.Rendered -= OnRendered;
            _helper.Events.GameLoop.DayStarted -= OnDayStarted;
        }

        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            LoadCharacters();
        }

        private void OnRendering(object sender, EventArgs e)
        {
            _hoverItem.Value = Utils.GetHoveredItem();
            _hoverRecipe.Value = Utils.GetHoveredCraftingRecipe();
        }

        private void OnRenderedHud(object sender, EventArgs e)
        {
            if (Game1.activeClickableMenu == null)
            {
                DrawItemLoversTooltip();
            }
        }

        private void OnRendered(object sender, EventArgs e)
        {
            if (Game1.activeClickableMenu != null)
            {
                DrawItemLoversTooltip();
            }
        }

        private void DrawItemLoversTooltip()
        {
            if (_hoverRecipe.Value != null || _hoverItem.Value != null)
            {
                Vector2 offset =
                    _hoverRecipe.Value != null
                        ? Utils.GetRecipeLoversTooltipOffset(_hoverRecipe.Value)
                        : Utils.GetItemLoversTooltipOffset(_hoverItem.Value);

                Item item =
                    _hoverRecipe.Value != null ? _hoverRecipe.Value.createItem() : _hoverItem.Value;

                List<NPC> itemLovers = GetItemLovers(item);

                int lovingCharsWidth =
                    (32 * Math.Min(3, itemLovers.Count)) + (itemLovers.Count >= 4 ? 32 : 0);

                DrawItemLovers(itemLovers, offset + new Vector2(-lovingCharsWidth, 65.0f));
            }
        }

        private static void DrawSmallTextWithShadow(SpriteBatch b, string text, Vector2 position)
        {
            b.DrawString(
                Game1.smallFont,
                text,
                position + new Vector2(2, 2),
                Game1.textShadowColor
            );
            b.DrawString(Game1.smallFont, text, position, Game1.textColor);
        }

        private static Rectangle DrawNPCHeadshot(NPC npc, Vector2 position)
        {
            ClickableTextureComponent icon =
                new(
                    new Rectangle(0, 0, Game1.tileSize, Game1.tileSize),
                    npc.Sprite.Texture,
                    npc.getMugShotSourceRect(),
                    Game1.pixelZoom
                );

            Game1.spriteBatch.Draw(
                icon.texture,
                position,
                icon.sourceRect,
                Color.White,
                0f,
                new Vector2(0, icon.sourceRect.Height / 4),
                2,
                SpriteEffects.None,
                0.86f
            );

            return icon.sourceRect;
        }

        private static void DrawItemLovers(List<NPC> npcs, Vector2 position)
        {
            float offset = 0.0f;
            int count = 0;
            foreach (NPC character in npcs)
            {
                Rectangle r = DrawNPCHeadshot(character, position + new Vector2(offset, 0));

                offset += r.Width + 12.0f;

                if (++count >= 3 && npcs.Count > 3)
                {
                    DrawSmallTextWithShadow(
                        Game1.spriteBatch,
                        "+" + (npcs.Count - 3).ToString(),
                        position + new Vector2(offset, 10.0f)
                    );
                    break;
                }
            }
        }

        private static readonly int LOVE_GIFT_TASTE = 0;

        private void LoadCharacters()
        {
            _characters.Clear();

            foreach (var location in Game1.locations)
            {
                foreach (var character in location.characters)
                {
                    if (character.isVillager() && Game1.NPCGiftTastes.ContainsKey(character.Name))
                    {
                        _characters.Add(character);
                    }
                }
            }
        }

        private List<NPC> GetItemLovers(Item item)
        {
            List<NPC> itemLovers = new();

            foreach (NPC npc in _characters)
            {
                if (item != null && npc.getGiftTasteForThisItem(item) == LOVE_GIFT_TASTE)
                {
                    if (
                        Game1.player.friendshipData.ContainsKey(npc.Name)
                        && Game1.player.friendshipData[npc.Name].Points
                            >= Utility.GetMaximumHeartsForCharacter(npc) * 250
                    )
                    {
                        continue;
                    }

                    itemLovers.Add(npc);
                }
            }

            return itemLovers;
        }
    }
}
