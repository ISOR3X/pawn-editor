using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlexLayout;

/// <summary>
///     Top-level entry points for the builder-style layout API.
///     <code>
/// Flex.Column(inRect, gap: 4f, col => {
///     col.Item(height: StyleLength.Px(60f), draw: rect => Widgets.DrawRectFast(rect, Color.blue));
///     col.Row(grow: 1f, gap: 4f, build: row => {
///         row.Item(grow: 1f, draw: rect => Widgets.DrawRectFast(rect, Color.yellow));
///         row.Item(grow: 2f, draw: rect => Widgets.DrawRectFast(rect, Color.cyan));
///     });
///     col.Item(height: StyleLength.Px(40f), draw: rect => Widgets.DrawRectFast(rect, Color.magenta));
/// });
/// </code>
///     For cases where you need to inspect <see cref="LayoutElement.ComputedRect" /> before
///     drawing (e.g. interactive widgets, tooltips), use the explicit
///     <see cref="FlexSolver.Compute" /> + <see cref="FlexSolver.Draw" /> API instead.
/// </summary>
public static class Flex
{
    private static readonly Dictionary<string, ScrollState> ScrollStates = new();

    // Bootstrap cache for fitContent — stores last known content size so the
    // synchronous measurement has a valid parent size to work with on frame 1.
    private static readonly Dictionary<string, float> FitContentSizes = new();

    internal static float GetFitContentSize(string key)
        => FitContentSizes.TryGetValue(key, out var v) ? v : 0f;

    internal static void SetFitContentSize(string key, float size)
        => FitContentSizes[key] = size;


    public static ScrollState GetScrollState(string key)
    {
        return ScrollStates.GetValueOrDefault(key);
    }

    public static void SetScrollState(string key, ScrollState state)
    {
        ScrollStates[key] = state;
    }

    // ──────────────────────────────────────────────────────────────────────
    // Row entry points
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>Builds and draws a row layout within <paramref name="rect" />.</summary>
    public static void Row(Rect rect, Action<FlexBuilder> build)
    {
        Run(rect, FlexDirection.Row, 0f, 0f, FlexWrap.NoWrap, build);
    }

    /// <summary>Builds and draws a row layout with a gap between items.</summary>
    public static void Row(Rect rect, float gap, Action<FlexBuilder> build)
    {
        Run(rect, FlexDirection.Row, gap, 0f, FlexWrap.NoWrap, build);
    }

    /// <summary>Builds and draws a row layout with independent column and row gaps.</summary>
    public static void Row(Rect rect, float columnGap, float rowGap, Action<FlexBuilder> build)
    {
        Run(rect, FlexDirection.Row, columnGap, rowGap, FlexWrap.NoWrap, build);
    }

    /// <summary>Builds and draws a wrapping row layout.</summary>
    public static void RowWrap(Rect rect, Action<FlexBuilder> build)
    {
        Run(rect, FlexDirection.Row, 0f, 0f, FlexWrap.Wrap, build);
    }

    /// <summary>Builds and draws a wrapping row layout with gaps.</summary>
    public static void RowWrap(Rect rect, float columnGap, float rowGap, Action<FlexBuilder> build)
    {
        Run(rect, FlexDirection.Row, columnGap, rowGap, FlexWrap.Wrap, build);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Column entry points
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>Builds and draws a column layout within <paramref name="rect" />.</summary>
    public static void Column(Rect rect, Action<FlexBuilder> build)
    {
        Run(rect, FlexDirection.Column, 0f, 0f, FlexWrap.NoWrap, build);
    }

    /// <summary>Builds and draws a column layout with a gap between items.</summary>
    public static void Column(Rect rect, float gap, Action<FlexBuilder> build)
    {
        Run(rect, FlexDirection.Column, 0f, gap, FlexWrap.NoWrap, build);
    }

    /// <summary>Builds and draws a column layout with independent column and row gaps.</summary>
    public static void Column(Rect rect, float columnGap, float rowGap, Action<FlexBuilder> build)
    {
        Run(rect, FlexDirection.Column, columnGap, rowGap, FlexWrap.NoWrap, build);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Shared runner
    // ──────────────────────────────────────────────────────────────────────

    private static void Run(
        Rect rect, FlexDirection direction,
        float columnGap, float rowGap, FlexWrap wrap,
        Action<FlexBuilder> build)
    {
        var container = new LayoutContainer
        {
            Style =
                new ElementStyle(display: Display.Flex, flexDirection: direction, columnGap: columnGap,
                    rowGap: rowGap, flexWrap: wrap)
        };

        build(new FlexBuilder(container, availableRect: rect));
        FlexSolver.Compute(container, rect);
        FlexSolver.Draw(container);
    }
    // ──────────────────────────────────────────────────────────────────────
    // Scroll state
    // ──────────────────────────────────────────────────────────────────────

    public struct ScrollState
    {
        public Vector2 scrollPos;
    }
}