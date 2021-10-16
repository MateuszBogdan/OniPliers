﻿using System.Collections.Generic;

namespace Pliers
{
    public class PliersToolHoverCard: HoverTextConfiguration
    {
        public PliersToolHoverCard()
        {
            ToolName = PliersStrings.STRING_PLIERS_TOOLTIP_TITLE;
        }
        
        public override void UpdateHoverElements(List<KSelectable> hoveredObjects) {
            HoverTextScreen screenInstance = HoverTextScreen.Instance;
            HoverTextDrawer drawer = screenInstance.BeginDrawing();
            drawer.BeginShadowBar(false);

            DrawTitle(screenInstance, drawer);
            drawer.NewLine(26);

            drawer.DrawIcon(screenInstance.GetSprite("icon_mouse_left"), 20);
            drawer.DrawText(PliersStrings.STRING_PLIERS_ACTION_DRAG, Styles_Instruction.Standard);
            drawer.AddIndent(8);

            drawer.DrawIcon(screenInstance.GetSprite("icon_mouse_right"), 20);
            drawer.DrawText(PliersStrings.STRING_PLIERS_ACTION_BACK, Styles_Instruction.Standard);

            drawer.EndShadowBar();
            drawer.EndDrawing();
        }
    }
}