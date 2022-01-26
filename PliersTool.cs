using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using PeterHan.PLib.Options;

namespace Pliers
{
    public sealed class PliersTool : FilteredDragTool
    {
        private static readonly UtilityConnections[] connections = {
            UtilityConnections.Left,
            UtilityConnections.Right,
            UtilityConnections.Up,
            UtilityConnections.Down
        };

        private PliersConfig config;
        
        public static PliersTool Instance { get; private set; }

        public PliersTool() {
            Instance = this;
            config = POptions.ReadSettings<PliersConfig>() ?? new PliersConfig();
        }
        
        public static void DestroyInstance() {
            Instance = null;
        }
        
        protected override void OnPrefabInit() {
            base.OnPrefabInit();

            visualizer = new GameObject("PliersVisualizer");
            visualizer.SetActive(false);

            var offsetObject = new GameObject();
            var spriteRenderer = offsetObject.AddComponent<SpriteRenderer>();
            spriteRenderer.color = PliersAssets.PLIERS_COLOR_DRAG;
            spriteRenderer.sprite = PliersAssets.PLIERS_VISUALIZER_SPRITE;

            offsetObject.transform.SetParent(visualizer.transform);
            offsetObject.transform.localPosition = new Vector3(0, Grid.HalfCellSizeInMeters);
            var sprite = spriteRenderer.sprite;
            offsetObject.transform.localScale = new Vector3(
                Grid.CellSizeInMeters / (sprite.texture.width / sprite.pixelsPerUnit),
                Grid.CellSizeInMeters / (sprite.texture.height / sprite.pixelsPerUnit)
            );

            offsetObject.SetLayerRecursively(LayerMask.NameToLayer("Overlay"));
            visualizer.transform.SetParent(transform);

            var areaVisualizerField = AccessTools.Field(typeof(DragTool), "areaVisualizer");
            var areaVisualizerSpriteRendererField = AccessTools.Field(typeof(DragTool), "areaVisualizerSpriteRenderer");

            var areaVisualizer = Util.KInstantiate((GameObject)AccessTools.Field(typeof(DeconstructTool), "areaVisualizer").GetValue(DeconstructTool.Instance));
            areaVisualizer.SetActive(false);

            areaVisualizer.name = "PliersAreaVisualizer";
            areaVisualizerSpriteRendererField.SetValue(this, areaVisualizer.GetComponent<SpriteRenderer>());
            areaVisualizer.transform.SetParent(transform);
            areaVisualizer.GetComponent<SpriteRenderer>().color = PliersAssets.PLIERS_COLOR_DRAG;
            areaVisualizer.GetComponent<SpriteRenderer>().material.color = PliersAssets.PLIERS_COLOR_DRAG;

            areaVisualizerField.SetValue(this, areaVisualizer);

            gameObject.AddComponent<PliersToolHoverCard>();
        }
        
        protected override void GetDefaultFilters(Dictionary<string, ToolParameterMenu.ToggleState> filters) {
            filters.Add(ToolParameterMenu.FILTERLAYERS.ALL, ToolParameterMenu.ToggleState.On);
            filters.Add(ToolParameterMenu.FILTERLAYERS.WIRES, ToolParameterMenu.ToggleState.Off);
            filters.Add(ToolParameterMenu.FILTERLAYERS.LIQUIDCONDUIT, ToolParameterMenu.ToggleState.Off);
            filters.Add(ToolParameterMenu.FILTERLAYERS.GASCONDUIT, ToolParameterMenu.ToggleState.Off);
            filters.Add(ToolParameterMenu.FILTERLAYERS.SOLIDCONDUIT, ToolParameterMenu.ToggleState.Off);
            filters.Add(ToolParameterMenu.FILTERLAYERS.LOGIC, ToolParameterMenu.ToggleState.Off);
        }

        protected override void OnDragComplete(Vector3 cursorDown, Vector3 cursorUp) {
            base.OnDragComplete(cursorDown, cursorUp);

            if (hasFocus) {
                Grid.PosToXY(cursorDown, out var x0, out var y0);
                Grid.PosToXY(cursorUp, out var x1, out var y1);

                if (x0 > x1) {
                    Util.Swap(ref x0, ref x1);
                }

                if (y0 > y1) {
                    Util.Swap(ref y0, ref y1);
                }

                for (var x = x0; x <= x1; ++x) {
                    for (var y = y0; y <= y1; ++y) {
                        var cell = Grid.XYToCell(x, y);

                        if (Grid.IsVisible(cell)) {
                            for (var layer = 0; layer < Grid.ObjectLayers.Length; ++layer) {
                                var gameObject = Grid.Objects[cell, layer];
                                Building building;

                                if (gameObject != null && (building = gameObject.GetComponent<Building>()) != null && IsActiveLayer(GetFilterLayerFromGameObject(gameObject))) {
                                    IHaveUtilityNetworkMgr utilityNetworkManager;

                                    if ((utilityNetworkManager = building.Def.BuildingComplete.GetComponent<IHaveUtilityNetworkMgr>()) != null) {
                                        UtilityConnections connectionsToRemove = 0;
                                        var buildingConnections = utilityNetworkManager.GetNetworkManager().GetConnections(cell, false);

                                        foreach (var utilityConnection in connections) {
                                            if ((buildingConnections & utilityConnection) != utilityConnection) {
                                                continue;
                                            }

                                            var offsetCell = Grid.OffsetCell(cell, Utilities.ConnectionsToOffset(utilityConnection));
                                            if (Grid.IsValidBuildingCell(offsetCell)) {
                                                Grid.CellToXY(offsetCell, out var x2, out var y2);

                                                if (x2 >= x0 && x2 <= x1 && y2 >= y0 && y2 <= y1) {
                                                    var otherGameObject = Grid.Objects[offsetCell, layer];
                                                    Building otherBuilding;

                                                    if (otherGameObject != null && (otherBuilding = otherGameObject.GetComponent<Building>()) != null && otherBuilding.Def.BuildingComplete.GetComponent<IHaveUtilityNetworkMgr>() != null && IsActiveLayer(GetFilterLayerFromGameObject(gameObject))) {
                                                        connectionsToRemove |= utilityConnection;
                                                    }
                                                }
                                            }
                                        }

                                        if (connectionsToRemove != 0) {
                                            ProcessPliers(gameObject, building, buildingConnections, connectionsToRemove, utilityNetworkManager, cell, layer);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void ProcessPliers(GameObject gameObject, Building building, UtilityConnections buildingConnections, UtilityConnections connectionsToRemove, IHaveUtilityNetworkMgr utilityNetworkManager, int cell, int layer)
        {
            if (config.ErrandEnabled)
            {
                if (connectionsToRemove.HasFlag(UtilityConnections.Up) ||
                    connectionsToRemove.HasFlag(UtilityConnections.Left))
                {
                    ProcessPliersErrand(gameObject, connectionsToRemove & (UtilityConnections.Up | UtilityConnections.Left));    
                }
            }
            else
            {
                ProcessPliersInstant(building, buildingConnections, connectionsToRemove, utilityNetworkManager, cell);
            }
        }

        private void ProcessPliersInstant(Building building, UtilityConnections buildingConnections, UtilityConnections connectionsToRemove, IHaveUtilityNetworkMgr utilityNetworkManager, int cell)
        {
            if (building.GetComponent<KAnimGraphTileVisualizer>() != null) {
                building.GetComponent<KAnimGraphTileVisualizer>().UpdateConnections(buildingConnections & ~connectionsToRemove);
                building.GetComponent<KAnimGraphTileVisualizer>().Refresh();
            }

            TileVisualizer.RefreshCell(cell, building.Def.TileLayer, building.Def.ReplacementLayer);
            utilityNetworkManager.GetNetworkManager()?.ForceRebuildNetworks();
        }

        private void ProcessPliersErrand(GameObject gameObject, UtilityConnections connectionsToRemove)
        {
            var pliersWorkable = gameObject.GetComponent<PliersWorkable>();
            if (pliersWorkable is null)
            {
                throw new Exception(gameObject.GetType().Name);
            }
            if (DebugHandler.InstantBuildMode)
            {
                pliersWorkable.JobFinished();
            }
            else
            {
                pliersWorkable.MarkForCut(connectionsToRemove);
                var prioritizable = gameObject.GetComponent<Prioritizable>();
                if (prioritizable is not null)
                {
                    prioritizable.SetMasterPriority(ToolMenu.Instance.PriorityScreen.GetLastSelectedPriority());
                }
            }
        }
    }
}