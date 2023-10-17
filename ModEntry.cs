using StardewModdingAPI;
using System;
using System.Collections.Generic;

namespace EasyGifts
{
    internal sealed class ModEntry : Mod
    {
        private readonly List<IDisposable> modules = new();

        public override void Entry(IModHelper helper)
        {
            modules.Add(new VillagerNeedsChatModule(helper));
            modules.Add(new GiftLoversTooltipModule(helper));
        }
    }
}
