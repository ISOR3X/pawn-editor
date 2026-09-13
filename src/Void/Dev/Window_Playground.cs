#if DEBUG
using LudeonTK;
using Taffy;
using UnityEngine;
using Verse;
using Void.Taffy;
using static VoidComponents;

namespace Void.Dev;

/// <summary>
/// Dev-only sandbox for exercising components on the new <see cref="UITree" /> without needing
/// PawnEditor to compile. Opened from the dev-mode debug actions menu under the "Void" category,
/// available on the main menu as well as in play. Edit the <see cref="DoWindowContents" /> body
/// and EditCompileReload picks the change up live.
/// </summary>
public class Window_Playground : Window
{
    private readonly UITree _tree = new();
    private int pageNo = 0;
    private int _clicks;
    private bool _showExtra;

    public override Vector2 InitialSize => new(640f, 480f);

    public Window_Playground()
    {
        resizeable = true;
        draggable = true;
        doCloseX = true;
        closeOnClickedOutside = false;
        absorbInputAroundWindow = false;
    }

    [DebugAction("Void", "Open playground",
        allowedGameStates = AllowedGameStates.Invalid)]
    private static void Open()
    {
        if (Find.WindowStack.IsOpen<Window_Playground>()) Find.WindowStack.TryRemove(typeof(Window_Playground));
        else Find.WindowStack.Add(new Window_Playground());
    }

    public override void DoWindowContents(Rect inRect)
    {
        _tree.Build(root =>
        {
            root.Div(builder: b =>
            {
                b.Button("<", onClick: _ => pageNo--);
                b.Text($"Page {pageNo}");
                b.Button(">", onClick: _ => pageNo++);
            }, style: new Style {display = TaffyDisplay.Flex, justifyContent = TaffyAlignContent.SpaceBetween});
            if (pageNo == 1) ButtonPlayground(root);

        }, new Style
        {
            display = TaffyDisplay.Flex,
            flexDirection = TaffyFlexDirection.Column,
            gap = Axes(10f),
            width = Dimension.Percent(1f),
            height = Dimension.Auto()
        });

        _tree.Draw(inRect);
    }

    public override void PostClose()
    {
        base.PostClose();
        _tree.Dispose();
    }

    private void ButtonPlayground(UIBranch builder)
    {
        builder.Text($"Button playground (clicks: {_clicks})", new Style { fontSize = GameFont.Medium });

        // One row per size, so measured widths can be compared side by side.
        builder.Div(b =>
        {
            b.Button("Small", size: ComponentSize.Small, onClick: _ => _clicks++);
            b.Button("Small + icon", TexUI.ArrowRight, size: ComponentSize.Small, onClick: _ => _clicks++);
            b.Button(icon: TexUI.ArrowRight, size: ComponentSize.Small, onClick: _ => _clicks++);
        }, style: Row());

        builder.Div(b =>
        {
            b.Button("Default", onClick: _ => _clicks++);
            b.Button("Default + icon", TexUI.ArrowRight, onClick: _ => _clicks++);
            b.Button(icon: TexUI.ArrowRight, onClick: _ => _clicks++);
            b.Button("Ghost", variant: ButtonVariant.Ghost, onClick: _ => _clicks++);
            b.Button("Disabled", disabled: true);
        }, style: Row());

        builder.Div(b =>
        {
            b.Button("Large", size: ComponentSize.Large, onClick: _ => _clicks++);
            b.Button("Large + icon", TexUI.ArrowRight, size: ComponentSize.Large, onClick: _ => _clicks++);
        }, style: Row());

        // Block button stretches to the parent width; resize the window to see it follow.
        builder.Button(block: true, onClick: _ => _showExtra = !_showExtra);

        // Toggled subtree exercises keyed add/remove of children.
        if (_showExtra)
            builder.Div(b =>
            {
                b.Text("This block is added and removed by the button above. A very long label follows to check truncation:");
                b.Button("Click to toggle debug. Also, this label is far too long for the space it has been given and should truncate",
                    TexUI.ArrowLeft, onClick: _ => { VoidMod.Settings.drawDebug = !VoidMod.Settings.drawDebug; }, style: new Style { maxWidth = Dimension.Px(220f) });
            }, style: new Style
            {
                display = TaffyDisplay.Flex,
                flexDirection = TaffyFlexDirection.Column,
                gap = Axes(6f),
                padding = Edges(8f),
                width = Dimension.Percent(1f)
            });
    }

    private static Style Row() => new()
    {
        display = TaffyDisplay.Flex,
        flexDirection = TaffyFlexDirection.Row,
        alignItems = TaffyAlignItems.Center,
        gap = Axes(8f),
        width = Dimension.Percent(1f)
    };

    private static TaffyAxes Axes(float v) => new(Dimension.Px(v));

    private static TaffyEdges Edges(float v) => new(Dimension.Px(v));
}
#endif
