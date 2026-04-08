using HotSwap;
using Taffy;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_Faction(SectionDef def) : SectionWorker(def)
{
    public override bool ShowSection(Pawn p) => base.ShowSection(p) && p.def.CanHaveFaction;


    protected override void DoSectionContents(TaffyBuilder builder, Pawn pawn)
    {
        // var (label, icon, color) = FactionUtility.GetFactionMeta(pawn.Faction);
        //
        // builder.Text("Faction",
        //     style: new Style { margin = new Rect<LengthPercentageAuto>(0f, GenUI.GapLabel, 0f, 0f) });
        // builder.Button(label, icon, color, onClick: _ =>
        //     {
        //         Find.WindowStack.Add(new FloatMenu(Find.FactionManager.AllFactionsInViewOrder.Select(f =>
        //         {
        //             var (l, i, c) = FactionUtility.GetFactionMeta(f);
        //             return new FloatMenuOption(l, () => { FactionUtility.SetFaction(pawn, f); }, i, c);
        //         }).ToList()));
        //     }, onHover: r => { TooltipHandler.TipRegion(r, FactionUtility.GetFactionTooltip(pawn.Faction)); }
        //     , style: new Style { size = new Size<Dimension>(200f, Dimension.AUTO) });
        builder.Text("Dolor velit aut enim nisi. Ea et adipisci rerum quaerat qui. Corrupti maxime assumenda autem ipsam pariatur sed explicabo. Sint nihil accusantium cupiditate vel.\n\nProvident aspernatur perferendis temporibus. Possimus et id maiores fuga. Consequuntur aut qui quis illo rerum omnis. Quasi ut vel vero velit explicabo eveniet quidem ut.\n\nSed voluptatem sunt dolor eius. Ipsum atque repellat doloribus placeat ut dolores nihil libero. Exercitationem voluptatem distinctio amet. Dolorum quibusdam nihil dolorem ullam incidunt sit maiores quidem. Et temporibus id quae sapiente ut aliquam.\n\nConsequatur perferendis ex ipsa velit ratione corporis. Consequatur et optio debitis amet ut voluptatum et quia. Qui quos consequatur et qui natus voluptas quia sit. Quis reiciendis voluptas et placeat doloremque sunt sint officiis.\n\nDignissimos non sunt praesentium. Tempora sint minus eius totam sit est ab ut. Soluta adipisci veniam autem nulla ullam. Qui iure provident qui sunt velit omnis architecto voluptatem.");
    }
}