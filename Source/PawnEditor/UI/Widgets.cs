using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace PawnEditor;

public static partial class PawnEditor
{
    private static readonly List<WidgetInfo> layoutInfo = new();
    private static readonly List<LayerInfo> layers = new();
    private static float totalHeight;

    private static Vector2 widgetsScrollPos;

    private static void DoWidgets(Rect inRect)
    {
        if (Event.current.type == EventType.Layout)
            LayoutWidgets(inRect.width - 20);
        else
            DrawWidgets(inRect);
    }

    private static void LayoutWidgets(float width)
    {
        layoutInfo.Clear();
        for (var i = 0; i < widgets.Count; i++)
        {
            Rect rect;
            if (showFactionInfo)
                rect = new(0, 0, widgets[i].GetWidth(selectedFaction), widgets[i].GetHeight(selectedFaction));
            else
                rect = new(0, 0, widgets[i].GetWidth(selectedPawn), widgets[i].GetHeight(selectedPawn));
            layoutInfo.Add(new(widgets[i], rect));
        }

        layoutInfo.SortByDescending(static info => info.rect.height, static info => info.rect.width);

        layers.Clear();

        for (var i = 0; i < layoutInfo.Count; i++)
        {
            var layout = layoutInfo[i];
            var placed = false;
            for (var j = 0; j < layers.Count; j++)
            {
                var layer = layers[j];
                if (layer.rect.height < layout.rect.height) continue;
                for (var k = 0; k < layer.freeSpace.Count; k++)
                {
                    var rect = layer.freeSpace[k];
                    if (rect.height < layout.rect.height) continue;
                    if (rect.width < layout.rect.width) continue;
                    layer.freeSpace.RemoveAt(k);
                    var layoutRect = new Rect(rect.position, layout.rect.size);
                    layoutInfo[i] = new(layout.def, layoutRect);
                    for (var l = 0; l < layer.freeSpace.Count; l++)
                        if (layer.freeSpace[l].Overlaps(layoutRect))
                        {
                            var overlappingRect = layer.freeSpace[l];
                            if (overlappingRect.xMax > layoutRect.xMin) overlappingRect.xMax = layoutRect.xMin;
                            if (overlappingRect.yMax > layoutRect.yMin) overlappingRect.yMax = layoutRect.yMin;
                            layer.freeSpace.Add(overlappingRect);
                            layer.freeSpace.RemoveAt(l);
                        }

                    layer.freeSpace.Add(new(layoutRect.xMin, layoutRect.yMax, rect.width, rect.height - layoutRect.height));
                    layer.freeSpace.Add(new(layoutRect.xMax, layoutRect.yMin, rect.width - layoutRect.width, rect.height));
                    placed = true;
                    break;
                }

                if (placed) break;
            }

            if (!placed)
            {
                var rect = new Rect(0, layers.Count == 0 ? 0 : layers[layers.Count - 1].rect.yMax, width, layout.rect.height);
                var layer = new LayerInfo(rect);
                var layoutRect = new Rect(rect.position, layout.rect.size);
                layoutInfo[i] = new(layout.def, layoutRect);
                layer.freeSpace.Add(new(layoutRect.xMax, rect.y, rect.width - layoutRect.width, rect.height));
                layers.Add(layer);
            }
        }

        totalHeight = layers[layers.Count - 1].rect.yMax;
        layers.Clear();
    }

    private static void DrawWidgets(Rect inRect)
    {
        var viewRect = new Rect(0, 0, inRect.width - 20, totalHeight);
        Widgets.BeginScrollView(inRect, ref widgetsScrollPos, viewRect);
        for (var i = 0; i < layoutInfo.Count; i++)
        {
            var layout = layoutInfo[i];
            Widgets.DrawMenuSection(layout.rect);
            if (showFactionInfo)
                layout.def.Draw(layout.rect.ContractedBy(2), selectedFaction);
            else
                layout.def.Draw(layout.rect.ContractedBy(2), selectedPawn);
        }

        Widgets.EndScrollView();
    }

    private readonly struct WidgetInfo
    {
        public readonly WidgetDef def;
        public readonly Rect rect;

        public WidgetInfo(WidgetDef def, Rect rect)
        {
            this.def = def;
            this.rect = rect;
        }
    }

    private readonly struct LayerInfo
    {
        public readonly Rect rect;
        public readonly List<Rect> freeSpace;

        public LayerInfo(Rect rect)
        {
            freeSpace = new();
            this.rect = rect;
        }
    }
}
