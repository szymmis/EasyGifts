using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using System;

namespace EasyGifts
{
    internal class VillagerNeedsChatModule : IDisposable
    {
        private readonly PerScreen<float> _yMovementPerDraw = new();
        private readonly PerScreen<float> _alpha = new();

        private readonly IModHelper _helper;

        public VillagerNeedsChatModule(IModHelper helper)
        {
            _helper = helper;
            _helper.Events.Display.RenderingHud += On_RenderingHud_DrawIcon;
            _helper.Events.GameLoop.UpdateTicked += UpdateTicked;
        }

        public void On_RenderingHud_DrawIcon(object sender, RenderingHudEventArgs e)
        {
            if (!Game1.eventUp && Game1.activeClickableMenu == null)
            {
                foreach (var character in Game1.currentLocation.characters)
                {
                    if (character.isVillager())
                    {
                        if (CharacterNeedsChat(character))
                        {
                            Vector2 pos = GetChatIconPositionAboveVillager(character);

                            Game1.spriteBatch.Draw(
                                Game1.mouseCursors,
                                Utility.ModifyCoordinatesForUIScale(
                                    new Vector2(
                                        pos.X + character.GetBoundingBox().Width / 2 - 16.0f,
                                        pos.Y
                                            - character.GetBoundingBox().Height * 2.5f
                                            - 12.0f
                                            + _yMovementPerDraw.Value
                                    )
                                ),
                                new Rectangle(66, 4, 14, 12),
                                Color.White * _alpha.Value,
                                0.0f,
                                Vector2.Zero,
                                3f,
                                SpriteEffects.None,
                                1f
                            );
                        }
                    }
                }
            }
        }

        private static bool CharacterNeedsChat(Character villager)
        {
            if (!Game1.NPCGiftTastes.ContainsKey(villager.Name))
            {
                return false;
            }

            if (!Game1.player.friendshipData.ContainsKey(villager.Name))
            {
                return true;
            }

            Friendship friendship = Game1.player.friendshipData[villager.Name];

            if (friendship.Points >= Utility.GetMaximumHeartsForCharacter(villager) * 250)
            {
                return false;
            }

            return !friendship.TalkedToToday;
        }

        private void UpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (Game1.eventUp || Game1.activeClickableMenu != null)
                return;

            float sine = (float)Math.Sin(e.Ticks / 20.0);
            _yMovementPerDraw.Value = -6f + 6f * sine;
            _alpha.Value = 0.8f + 0.2f * sine;
        }

        private Vector2 GetChatIconPositionAboveVillager(Character animal)
        {
            return new Vector2(
                Game1.viewport.Width <= Game1.currentLocation.map.DisplayWidth
                    ? animal.position.X - Game1.viewport.X
                    : animal.position.X
                        + ((Game1.viewport.Width - Game1.currentLocation.map.DisplayWidth) / 2),
                Game1.viewport.Height <= Game1.currentLocation.map.DisplayHeight
                    ? animal.position.Y - Game1.viewport.Y
                    : animal.position.Y
                        + ((Game1.viewport.Height - Game1.currentLocation.map.DisplayHeight) / 2)
            );
        }

        public void Dispose()
        {
            _helper.Events.Display.RenderingHud -= On_RenderingHud_DrawIcon;
            _helper.Events.GameLoop.UpdateTicked -= UpdateTicked;
        }
    }
}
