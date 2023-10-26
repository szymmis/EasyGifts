using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using System.Text;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using StardewValley.Tools;
using SObject = StardewValley.Object;
using System.Collections.Generic;

namespace EasyGifts
{
    public static class Utils
    {
        public static Item GetHoveredItem()
        {
            if (Game1.activeClickableMenu == null && Game1.onScreenMenus != null)
            {
                foreach (var menu in Game1.onScreenMenus)
                {
                    if (menu is Toolbar toolbar)
                    {
                        FieldInfo hoverItemField = typeof(Toolbar).GetField(
                            "hoverItem",
                            BindingFlags.Instance | BindingFlags.NonPublic
                        );
                        return hoverItemField.GetValue(toolbar) as Item;
                    }
                }
            }

            if (
                Game1.activeClickableMenu is GameMenu gameMenu
                && gameMenu.GetCurrentPage() is InventoryPage inventory
            )
            {
                FieldInfo hoveredItemField = typeof(InventoryPage).GetField(
                    "hoveredItem",
                    BindingFlags.Instance | BindingFlags.NonPublic
                );
                return hoveredItemField.GetValue(inventory) as Item;
            }

            if (Game1.activeClickableMenu is ItemGrabMenu itemMenu)
            {
                return itemMenu.hoveredItem;
            }

            if (Game1.activeClickableMenu is CraftingPage craftingPage)
            {
                return typeof(CraftingPage)
                        .GetField("hoverItem", BindingFlags.Instance | BindingFlags.NonPublic)
                        .GetValue(craftingPage) as Item;
            }

            return null;
        }

        public static CraftingRecipe GetHoveredCraftingRecipe()
        {
            CraftingRecipe hoverRecipe = null;

            if (Game1.activeClickableMenu is CraftingPage craftingPage)
            {
                FieldInfo hoveredCraftingRecipeField = typeof(CraftingPage).GetField(
                    "hoverRecipe",
                    BindingFlags.Instance | BindingFlags.NonPublic
                );
                hoverRecipe = hoveredCraftingRecipeField.GetValue(craftingPage) as CraftingRecipe;
            }

            return hoverRecipe;
        }

        public static Vector2 GetRecipeLoversTooltipOffset(CraftingRecipe recipe)
        {
            string boldTitleText =
                recipe.numberProducedPerCraft > 1
                    ? $"{recipe.DisplayName} x{recipe.numberProducedPerCraft}"
                    : recipe.DisplayName;

            SObject item = recipe.createItem() as SObject;
            string[] objInfo = Game1.objectInformation[item.ParentSheetIndex].Split('/');
            string[] buffIconsToDisplay = (objInfo.Length > 7) ? objInfo[7].Split(' ') : null;

            // TODO(szymmis): Use getContainerContents() from CraftingPage class
            IList<Item> additionalCraftMaterials = new List<Item>();

            return CalculateTooltipOffset(
                boldTitleText,
                " ",
                -1,
                buffIconsToDisplay,
                recipe.createItem(),
                recipe,
                additionalCraftMaterials
            );
        }

        public static Vector2 GetItemLoversTooltipOffset(Item item)
        {
            bool edibleItem = item is SObject && (item as SObject).Edibility != -300;
            int healAmountToDisplay = edibleItem ? (item as SObject).Edibility : (-1);

            string[] objInfo = edibleItem
                ? Game1.objectInformation[item.ParentSheetIndex].Split('/')
                : Array.Empty<string>();
            string[] buffIconsToDisplay =
                (edibleItem && objInfo.Length > 7)
                    ? item.ModifyItemBuffs(objInfo[7].Split(' '))
                    : null;

            return CalculateTooltipOffset(
                    item.DisplayName,
                    item.getDescription(),
                    healAmountToDisplay,
                    buffIconsToDisplay,
                    item,
                    null,
                    new List<Item>()
                ) + new Vector2(-20f, 0f);
        }

        private static Vector2 CalculateTooltipOffset(
            string boldTitleText,
            string description,
            int healAmountToDisplay,
            string[] buffIconsToDisplay,
            Item hoveredItem,
            CraftingRecipe craftingIngredients,
            IList<Item> additionalCraftMaterials
        )
        {
            int xOffset = 0;
            int yOffset = 0;
            int moneyAmountToDisplayAtBottom = -1;
            int extraItemToShowIndex = -1;
            int extraItemToShowAmount = -1;

            StringBuilder text = new(description);
            SpriteFont font = Game1.smallFont;

            string bold_title_subtext = null;
            if (boldTitleText != null && boldTitleText.Length == 0)
            {
                boldTitleText = null;
            }
            int width =
                Math.Max(
                    (healAmountToDisplay != -1)
                        ? ((int)font.MeasureString(healAmountToDisplay + "+ Energy" + 32).X)
                        : 0,
                    Math.Max(
                        (int)font.MeasureString(text).X,
                        (boldTitleText != null)
                            ? ((int)Game1.dialogueFont.MeasureString(boldTitleText).X)
                            : 0
                    )
                ) + 32;
            int height2 = Math.Max(
                20 * 3,
                (int)font.MeasureString(text).Y
                    + 32
                    + (int)(
                        (moneyAmountToDisplayAtBottom > -1)
                            ? (
                                font.MeasureString(string.Concat(moneyAmountToDisplayAtBottom)).Y
                                + 4f
                            )
                            : 8f
                    )
                    + (int)(
                        (boldTitleText != null)
                            ? (Game1.dialogueFont.MeasureString(boldTitleText).Y + 16f)
                            : 0f
                    )
            );

            if (extraItemToShowIndex != -1)
            {
                string[] split = Game1.objectInformation[extraItemToShowIndex].Split('/');
                string objName = split[0];
                if (LocalizedContentManager.CurrentLanguageCode != 0)
                {
                    objName = split[4];
                }
                string requirement2 = Game1.content.LoadString(
                    "Strings\\UI:ItemHover_Requirements",
                    extraItemToShowAmount,
                    (extraItemToShowAmount > 1) ? Lexicon.makePlural(objName) : objName
                );
                int spriteWidth =
                    Game1
                        .getSourceRectForStandardTileSheet(
                            Game1.objectSpriteSheet,
                            extraItemToShowIndex,
                            16,
                            16
                        )
                        .Width
                    * 2
                    * 4;
                width = Math.Max(width, spriteWidth + (int)font.MeasureString(requirement2).X);
            }
            if (buffIconsToDisplay != null)
            {
                for (int k = 0; k < buffIconsToDisplay.Length; k++)
                {
                    if (!buffIconsToDisplay[k].Equals("0"))
                    {
                        height2 += 34;
                    }
                }
                height2 += 4;
            }
            if (
                craftingIngredients != null
                && Game1.options.showAdvancedCraftingInformation
                && craftingIngredients.getCraftCountText() != null
            )
            {
                height2 += (int)font.MeasureString("T").Y;
            }
            if (hoveredItem != null)
            {
                height2 += 68 * hoveredItem.attachmentSlots();
                string categoryName = hoveredItem.getCategoryName();
                if (categoryName.Length > 0)
                {
                    width = Math.Max(width, (int)font.MeasureString(categoryName).X + 32);
                    height2 += (int)font.MeasureString("T").Y;
                }
                int maxStat = 9999;
                int buffer = 92;
                Point p = hoveredItem.getExtraSpaceNeededForTooltipSpecialIcons(
                    font,
                    width,
                    buffer,
                    height2,
                    text,
                    boldTitleText,
                    moneyAmountToDisplayAtBottom
                );
                width = (p.X != 0) ? p.X : width;
                height2 = (p.Y != 0) ? p.Y : height2;
                if (
                    hoveredItem is MeleeWeapon
                    && (hoveredItem as MeleeWeapon).GetTotalForgeLevels() > 0
                )
                {
                    height2 += (int)font.MeasureString("T").Y;
                }
                if (
                    hoveredItem is MeleeWeapon
                    && (hoveredItem as MeleeWeapon).GetEnchantmentLevel<GalaxySoulEnchantment>() > 0
                )
                {
                    height2 += (int)font.MeasureString("T").Y;
                }
                if (hoveredItem is SObject && (hoveredItem as SObject).Edibility != -300)
                {
                    height2 =
                        (healAmountToDisplay == -1)
                            ? (height2 + 40)
                            : (height2 + 40 * ((healAmountToDisplay <= 0) ? 1 : 2));
                    healAmountToDisplay = (hoveredItem as SObject).staminaRecoveredOnConsumption();
                    width = (int)
                        Math.Max(
                            width,
                            Math.Max(
                                font.MeasureString(
                                    Game1.content.LoadString(
                                        "Strings\\UI:ItemHover_Energy",
                                        maxStat
                                    )
                                ).X + buffer,
                                font.MeasureString(
                                    Game1.content.LoadString(
                                        "Strings\\UI:ItemHover_Health",
                                        maxStat
                                    )
                                ).X + buffer
                            )
                        );
                }
                if (buffIconsToDisplay != null)
                {
                    for (int j = 0; j < buffIconsToDisplay.Length; j++)
                    {
                        if (!buffIconsToDisplay[j].Equals("0") && j <= 11)
                        {
                            width = (int)
                                Math.Max(
                                    width,
                                    font.MeasureString(
                                        Game1.content.LoadString(
                                            "Strings\\UI:ItemHover_Buff" + j,
                                            maxStat
                                        )
                                    ).X + buffer
                                );
                        }
                    }
                }
            }
            Vector2 small_text_size = Vector2.Zero;
            if (craftingIngredients != null)
            {
                if (Game1.options.showAdvancedCraftingInformation)
                {
                    int craftable_count = craftingIngredients.getCraftableCount(
                        additionalCraftMaterials
                    );
                    if (craftable_count > 1)
                    {
                        bold_title_subtext = " (" + craftable_count + ")";
                        small_text_size = Game1.smallFont.MeasureString(bold_title_subtext);
                    }
                }
                width = (int)
                    Math.Max(
                        Game1.dialogueFont.MeasureString(boldTitleText).X + small_text_size.X + 12f,
                        384f
                    );
                height2 +=
                    craftingIngredients.getDescriptionHeight(width - 8)
                    + ((healAmountToDisplay == -1) ? (-32) : 0);
            }
            else if (bold_title_subtext != null && boldTitleText != null)
            {
                small_text_size = Game1.smallFont.MeasureString(bold_title_subtext);
                width = (int)
                    Math.Max(
                        width,
                        Game1.dialogueFont.MeasureString(boldTitleText).X + small_text_size.X + 12f
                    );
            }
            int x = Game1.getOldMouseX() + 32 + xOffset;
            int y4 = Game1.getOldMouseY() + 32 + yOffset;

            if (x + width > Utility.getSafeArea().Right)
            {
                x = Utility.getSafeArea().Right - width;
                y4 += 16;
            }
            if (y4 + height2 > Utility.getSafeArea().Bottom)
            {
                x += 16;
                if (x + width > Utility.getSafeArea().Right)
                {
                    x = Utility.getSafeArea().Right - width;
                }
                y4 = Utility.getSafeArea().Bottom - height2;
            }

            return new Vector2(
                x + width,
                hoveredItem.getCategoryName().Length > 0 ? y4 + 65.0f : y4 + 32.0f
            );
        }
    }
}
