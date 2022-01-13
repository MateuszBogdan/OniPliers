using System;
using KSerialization;
using TUNING;

namespace Pliers;

public class PliersWorkable: Workable
{
    [MyCmpGet]
    private Conduit conduit;
    [MyCmpGet]
    private Wire wire;
    [MyCmpGet]
    private LogicWire logicWire;
    
    [MyCmpGet]
    private SolidConduit solidConduit;
    

    [MyCmpGet] private Building building;
    
    public Chore chore;

    private static StatusItem PliersWiresStatusItem;
    private static StatusItem PliersLiquidStatusItem;
    private static StatusItem PliersGasStatusItem;
    private static StatusItem PliersSolidStatusItem;
    private static StatusItem PliersLogicStatusItem;
    [Serialize]
    private UtilityConnections connectionsToRemove;
    
    
    private static readonly EventSystem.IntraObjectHandler<PliersWorkable> OnPliersCancelledDelegate = new((component, data) => component.OnEmptyConduitCancelled(data));

    protected override void OnPrefabInit()
    {
        base.OnPrefabInit();
        SetOffsetTable(OffsetGroups.InvertedStandardTable);
        SetWorkTime(float.PositiveInfinity);
        faceTargetWhenWorking = true;
        minimumAttributeMultiplier = 0.75f;
        workerStatusItem = Db.Get().DuplicantStatusItems.Deconstructing;
        attributeConverter = Db.Get().AttributeConverters.ConstructionSpeed;
        attributeExperienceMultiplier = DUPLICANTSTATS.ATTRIBUTE_LEVELING.MOST_DAY_EXPERIENCE;
        skillExperienceSkillGroup = Db.Get().SkillGroups.Building.Id;
        skillExperienceMultiplier = SKILLS.MOST_DAY_EXPERIENCE;
        multitoolContext = (HashedString) "build";
        multitoolHitEffectTag = (Tag) EffectConfigs.DemolishSplashId;
        SetWorkTime(4f);
        Subscribe((int)GameHashes.Cancel, OnPliersCancelledDelegate);
        if (PliersWiresStatusItem == null)
        {
            if (Assets.GetTintedSprite(PliersAssets.PLIERS_STATUS_ICON_NAME) is null)
                throw new Exception("Icon not loaded properly");
            PliersWiresStatusItem = new StatusItem("PliersWires", PliersStrings.STRING_PLIERS_STATUS_ITEM_NAME,
                PliersStrings.STRING_PLIERS_STATUS_ITEM_TOOLTIP, PliersAssets.PLIERS_STATUS_ICON_NAME, StatusItem.IconType.Custom,
                NotificationType.BadMinor, false, OverlayModes.Power.ID,
                (int)(StatusItem.StatusItemOverlays.PowerMap | StatusItem.StatusItemOverlays.None));

            PliersLiquidStatusItem = new StatusItem("PliersLiquid", PliersStrings.STRING_PLIERS_STATUS_ITEM_NAME,
                PliersStrings.STRING_PLIERS_STATUS_ITEM_TOOLTIP, PliersAssets.PLIERS_STATUS_ICON_NAME, StatusItem.IconType.Custom,
                NotificationType.BadMinor, false, OverlayModes.LiquidConduits.ID,
                (int) (StatusItem.StatusItemOverlays.LiquidPlumbing | StatusItem.StatusItemOverlays.None));
            
            PliersGasStatusItem = new StatusItem("PliersGas", PliersStrings.STRING_PLIERS_STATUS_ITEM_NAME,
                PliersStrings.STRING_PLIERS_STATUS_ITEM_TOOLTIP, PliersAssets.PLIERS_STATUS_ICON_NAME, StatusItem.IconType.Custom,
                NotificationType.BadMinor, false, OverlayModes.GasConduits.ID,
                (int) (StatusItem.StatusItemOverlays.GasPlumbing | StatusItem.StatusItemOverlays.None));
            
            PliersSolidStatusItem = new StatusItem("PliersSolid", PliersStrings.STRING_PLIERS_STATUS_ITEM_NAME,
                PliersStrings.STRING_PLIERS_STATUS_ITEM_TOOLTIP, PliersAssets.PLIERS_STATUS_ICON_NAME, StatusItem.IconType.Custom,
                NotificationType.BadMinor, false, OverlayModes.SolidConveyor.ID,
                (int) (StatusItem.StatusItemOverlays.Conveyor | StatusItem.StatusItemOverlays.None));
            
            PliersLogicStatusItem = new StatusItem("PliersLogic", PliersStrings.STRING_PLIERS_STATUS_ITEM_NAME,
                PliersStrings.STRING_PLIERS_STATUS_ITEM_TOOLTIP, PliersAssets.PLIERS_STATUS_ICON_NAME, StatusItem.IconType.Custom,
                NotificationType.BadMinor, false, OverlayModes.Logic.ID,
                (int) (StatusItem.StatusItemOverlays.Logic | StatusItem.StatusItemOverlays.None));
        }
        shouldShowSkillPerkStatusItem = false;
    }

    private void CancelPliers()
    {
        CleanUpVisualization();
        if (chore == null)
            return;
        chore.Cancel("Cancel");
        chore = null;
        shouldShowSkillPerkStatusItem = false;
        UpdateStatusItem();
    }

    private void CleanUpVisualization()
    {
        var statusItem = GetStatusItem();
        var component = GetComponent<KSelectable>();
        if (component is not null)
            component.ToggleStatusItem(statusItem, false);
        if (chore == null)
            return;
        GetComponent<Prioritizable>().RemoveRef();
    }

    private StatusItem GetStatusItem()
    {
        if (conduit is not null)
        {
            return conduit.ConduitType switch
            {
                ConduitType.Gas => PliersGasStatusItem,
                ConduitType.Liquid => PliersLiquidStatusItem,
                ConduitType.Solid => PliersSolidStatusItem,
                _ => throw new ArgumentException()
            };
        }

        if (wire is not null)
        {
            return PliersWiresStatusItem;
        }

        if (logicWire is not null)
        {
            return PliersLogicStatusItem;
        }

        if (solidConduit is not null)
        {
            return PliersSolidStatusItem;
        }

        throw new ArgumentException(building.Def.name);
    }

    public void JobFinished()
    {
        var utilityNetworkManager = building.Def.BuildingComplete.GetComponent<IHaveUtilityNetworkMgr>();
        var cell = building.GetCell();
        var buildingConnections = utilityNetworkManager.GetNetworkManager().GetConnections(cell, false);
        if (building.GetComponent<KAnimGraphTileVisualizer>() != null) {
            building.GetComponent<KAnimGraphTileVisualizer>().UpdateConnections(buildingConnections & ~connectionsToRemove);
            building.GetComponent<KAnimGraphTileVisualizer>().Refresh();
        }

        TileVisualizer.RefreshCell(cell, building.Def.TileLayer, building.Def.ReplacementLayer);
        utilityNetworkManager.GetNetworkManager()?.ForceRebuildNetworks();

        connectionsToRemove = 0;
        CleanUpVisualization();
        chore = null;
        shouldShowSkillPerkStatusItem = false;
        UpdateStatusItem();
    }

    public void MarkForCut(UtilityConnections connectionsToRemove)
    {
        if (chore is not null)
            return;
        var statusItem = GetStatusItem();
        GetComponent<KSelectable>().ToggleStatusItem(statusItem, true);
        CreateWorkChore();
        this.connectionsToRemove |= connectionsToRemove;
    }
    
    private void CreateWorkChore()
    {
        GetComponent<Prioritizable>().AddRef();
        chore = new WorkChore<PliersWorkable>(Db.Get().ChoreTypes.Deconstruct, this, only_when_operational: false);
        shouldShowSkillPerkStatusItem = true;
        UpdateStatusItem();
    }
    
    protected override void OnCleanUp()
    {
        CancelPliers();
        base.OnCleanUp();
    }

    protected override void OnCompleteWork(Worker worker)
    {
        base.OnCompleteWork(worker);

        JobFinished();
    }

    private void OnEmptyConduitCancelled(object _) => CancelPliers();
}