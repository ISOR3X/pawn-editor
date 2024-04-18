using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class TabWorker_Bio_AnimalMech : TabWorker<Pawn>
{
    private string ageBiologicalBuffer;

    public override void DrawTabContents(Rect rect, Pawn pawn)
    {
        var headerRect = rect.TakeTopPart(170);
        var portraitRect = headerRect.TakeLeftPart(170);
        PawnEditor.DrawPawnPortrait(portraitRect);
        DoButtons(headerRect.TakeRightPart(212).TopPartPixels(80), pawn);
        headerRect.xMin += 3;
        DoBasics(headerRect.TakeLeftPart(headerRect.width * 0.67f).ContractedBy(5, 0), pawn);
        if (pawn.training != null) DoSkills(rect.LeftPart(0.34f), pawn);
    }

    private void DoButtons(Rect buttonsRect, Pawn pawn)
    {
        using var block = new TextBlock(TextAnchor.MiddleCenter);
        Widgets.DrawHighlight(buttonsRect);
        buttonsRect = buttonsRect.ContractedBy(6);

        var devStageRect = buttonsRect.LeftHalf().ContractedBy(2);
        if (ModsConfig.BiotechActive)
        {
            var devStage = (pawn.ageTracker.CurLifeStageIndex / (float)pawn.RaceProps.lifeStageAges.Count) switch
            {
                <= 0.33f => DevelopmentalStage.Baby,
                <= 0.66f => DevelopmentalStage.Child,
                _ => DevelopmentalStage.Adult
            };
            var text = devStage.ToString().Translate().CapitalizeFirst();
            if (Mouse.IsOver(devStageRect))
            {
                Widgets.DrawHighlight(devStageRect);
                if (Find.WindowStack.FloatMenu == null)
                    TooltipHandler.TipRegion(devStageRect, text.Colorize(ColoredText.TipSectionTitleColor) + "\n\n" + "DevelopmentalAgeSelectionDesc".Translate());
            }

            Widgets.Label(devStageRect.BottomHalf(), text);
            if (Widgets.ButtonImageWithBG(devStageRect.TopHalf(), devStage.Icon().Texture, new Vector2(22f, 22f)))
            {
                var options = new List<FloatMenuOption>
                {
                    new("Adult".Translate().CapitalizeFirst(), () => SetDevStage(pawn, DevelopmentalStage.Adult),
                        DevelopmentalStageExtensions.AdultTex.Texture, Color.white),
                    new("Child".Translate().CapitalizeFirst(), () => SetDevStage(pawn, DevelopmentalStage.Child),
                        DevelopmentalStageExtensions.ChildTex.Texture, Color.white),
                    new("Baby".Translate().CapitalizeFirst(), () => SetDevStage(pawn, DevelopmentalStage.Baby),
                        DevelopmentalStageExtensions.BabyTex.Texture, Color.white)
                };
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        var sexRect = buttonsRect.RightHalf().ContractedBy(2);
        Widgets.DrawHighlightIfMouseover(sexRect);
        Widgets.Label(sexRect.BottomHalf(), "PawnEditor.Sex".Translate());
        if (Widgets.ButtonImageWithBG(sexRect.TopHalf(), pawn.gender.GetIcon(), new Vector2(22f, 22f)) && pawn.kindDef.fixedGender == null
                                                                                                       && pawn.RaceProps.hasGenders)
        {
            var list = new List<FloatMenuOption>
            {
                new("Female".Translate().CapitalizeFirst(), () => SetGender(pawn, Gender.Female), GenderUtility.FemaleIcon,
                    Color.white),
                new("Male".Translate().CapitalizeFirst(), () => SetGender(pawn, Gender.Male), GenderUtility.MaleIcon, Color.white)
            };

            Find.WindowStack.Add(new FloatMenu(list));
        }
    }

    private void SetDevStage(Pawn pawn, DevelopmentalStage stage)
    {
        var lifeStage = stage switch
        {
            DevelopmentalStage.Baby or DevelopmentalStage.Newborn => pawn.RaceProps.lifeStageAges.MinBy(lifeStage => lifeStage.minAge),
            DevelopmentalStage.Child => pawn.RaceProps.lifeStageAges.OrderBy(lifeStage => lifeStage.minAge).Skip(1).First(),
            DevelopmentalStage.Adult => pawn.RaceProps.lifeStageAges.MaxBy(lifeStage => lifeStage.minAge),
            _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, null)
        };
        if (lifeStage != null) SetAge(pawn, lifeStage.minAge);
    }

    private void SetAge(Pawn pawn, float age)
    {
        pawn.ageTracker.growth = Mathf.InverseLerp(0f, pawn.RaceProps.lifeStageAges[pawn.RaceProps.lifeStageAges.Count - 1].minAge, age);
        pawn.ageTracker.AgeBiologicalTicks = (long)(age * 3600000L);
        ageBiologicalBuffer = null;
    }

    private static void SetGender(Pawn pawn, Gender gender)
    {
        pawn.gender = gender;
        RecacheGraphics(pawn);
    }

    private static void RecacheGraphics(Pawn pawn)
    {
        LongEventHandler.ExecuteWhenFinished(pawn.Drawer.renderer.SetAllGraphicsDirty);
    }

    private void DoBasics(Rect inRect, Pawn pawn)
    {
        inRect.xMax -= 10;
        Widgets.Label(inRect.TakeTopPart(Text.LineHeight), "PawnEditor.Basic".Translate().Colorize(ColoredText.TipSectionTitleColor));
        inRect.xMin += 5;
        var name = "PawnEditor.Name".Translate();
        var age = "PawnEditor.Age".Translate();
        var bonded = pawn.RaceProps.IsMechanoid ? "Overseer".Translate() : "PawnEditor.Bonded".Translate();
        var leftWidth = UIUtility.ColumnWidth(3, name, age, bonded) + 18f;
        var nameRect = inRect.TakeTopPart(30);
        using (new TextBlock(TextAnchor.MiddleLeft)) Widgets.Label(nameRect.TakeLeftPart(leftWidth), name);
        if (pawn.Name is NameTriple nameTriple)
        {
            var firstRect = new Rect(nameRect);
            firstRect.width *= 0.333f;
            var nickRect = new Rect(nameRect);
            nickRect.width *= 0.333f;
            nickRect.x += nickRect.width;
            var lastRect = new Rect(nameRect);
            lastRect.width *= 0.333f;
            lastRect.x += nickRect.width * 2f;
            var first = nameTriple.First;
            var nick = nameTriple.Nick;
            var last = nameTriple.Last;
            CharacterCardUtility.DoNameInputRect(firstRect, ref first, 12);
            if (nameTriple.Nick == nameTriple.First || nameTriple.Nick == nameTriple.Last) GUI.color = new(1f, 1f, 1f, 0.5f);
            CharacterCardUtility.DoNameInputRect(nickRect, ref nick, 16);
            GUI.color = Color.white;
            CharacterCardUtility.DoNameInputRect(lastRect, ref last, 12);
            if (nameTriple.First != first || nameTriple.Nick != nick || nameTriple.Last != last)
                pawn.Name = new NameTriple(first, string.IsNullOrEmpty(nick) ? first : nick, last);

            TooltipHandler.TipRegionByKey(firstRect, "FirstNameDesc");
            TooltipHandler.TipRegionByKey(nickRect, "ShortIdentifierDesc");
            TooltipHandler.TipRegionByKey(lastRect, "LastNameDesc");
        }
        else if (pawn.Name is NameSingle nameSingle)
        {
            var nameSingleName = nameSingle.Name;
            CharacterCardUtility.DoNameInputRect(nameRect, ref nameSingleName, 16);
            if (nameSingleName != nameSingle.Name)
                pawn.Name = new NameSingle(nameSingleName);

            TooltipHandler.TipRegionByKey(nameRect, "ShortIdentifierDesc");
        }
        else
            Widgets.Label(nameRect, pawn.NameFullColored);

        inRect.yMin += 3;
        var ageRect = inRect.TakeTopPart(30);
        using (new TextBlock(TextAnchor.MiddleLeft)) Widgets.Label(ageRect.TakeLeftPart(leftWidth), age);
        var ageBio = pawn.ageTracker.AgeBiologicalYears;
        if (Widgets.ButtonImage(ageRect.TakeLeftPart(25).ContractedBy(0, 5), TexPawnEditor.ArrowLeftHalf))
        {
            ageBio--;
            ageBiologicalBuffer = null;
        }

        if (Widgets.ButtonImage(ageRect.TakeRightPart(25).ContractedBy(0, 5), TexPawnEditor.ArrowRightHalf))
        {
            ageBio++;
            ageBiologicalBuffer = null;
        }

        Widgets.TextFieldNumeric(ageRect, ref ageBio, ref ageBiologicalBuffer);
        if (ageBio != pawn.ageTracker.AgeBiologicalYears) SetAge(pawn, ageBio);
        inRect.yMin += 3;
        var bondRect = inRect.TakeTopPart(30);
        using (new TextBlock(TextAnchor.MiddleLeft)) Widgets.Label(bondRect.TakeLeftPart(leftWidth), bonded);
        var bondRelation = pawn.RaceProps.IsMechanoid ? PawnRelationDefOf.Overseer : PawnRelationDefOf.Bond;
        var bondedTo = pawn.relations.GetFirstDirectRelationPawn(bondRelation, _ => true);
        var bond = bondedTo == null ? null : pawn.relations.GetDirectRelation(bondRelation, bondedTo);
        if (Widgets.ButtonText(bondRect, bondedTo?.Name?.ToStringShort ?? (pawn.RaceProps.IsMechanoid ? "OverseerNone" : "None").Translate()))
        {
            var possiblePawns = pawn.MapHeld == null
                ? Find.WorldPawns.AllPawnsAlive.Where(p =>
                    p.IsColonistPlayerControlled && (!pawn.RaceProps.IsMechanoid || MechanitorUtility.IsMechanitor(p))).ToList()
                : pawn.MapHeld.mapPawns.FreeColonists.Where(p => !pawn.RaceProps.IsMechanoid || MechanitorUtility.IsMechanitor(p)).ToList();
            if (possiblePawns.Any())
            {
                var options = possiblePawns.Select(p => new FloatMenuOption(p.Name.ToStringShort, () =>
                    {
                        if (bond != null && bond.def != PawnRelationDefOf.Overseer)
                        {
                            pawn.relations.RemoveDirectRelation(bond);
                        }
                        pawn.relations.AddDirectRelation(bondRelation, p);
                    }))
                    .ToList();
                options.Add(new FloatMenuOption("None".Translate(), () =>
                {
                    if (bond != null)
                        pawn.relations.RemoveDirectRelation(bond);
                }));
                Find.WindowStack.Add(new FloatMenu(options));
            }
            else
            {
                Messages.Message("PawnEditor.NoOptions".Translate(), MessageTypeDefOf.SilentInput);
            }
        }
    }

    private static void DoSkills(Rect inRect, Pawn pawn)
    {
        var headerRect = inRect.TakeTopPart(Text.LineHeight);
        Widgets.Label(headerRect, "Skills".Translate().Colorize(ColoredText.TipSectionTitleColor));
        GUI.color = Color.white;
        if (Widgets.ButtonText(headerRect.TakeRightPart(60), "PawnEditor.Preset".Translate()))
            Find.WindowStack.Add(new FloatMenu(new()
            {
                new("PawnEditor.SetAllTo".Translate("Skills".Translate().ToLower(), 0), GetSetDelegate(pawn, false)),
                new("PawnEditor.SetAllTo".Translate("Skills".Translate().ToLower(), "PawnEditor.Max".Translate()), GetSetDelegate(pawn, true))
            }));

        inRect.xMin += 4;
        inRect.yMin += 4f;
        var leftWidth = DefDatabase<TrainableDef>.AllDefs.Select(def => Text.CalcSize(def.LabelCap.Resolve()).x).Max() + 16f;
        using (new TextBlock(TextAnchor.MiddleLeft))
            foreach (var def in DefDatabase<TrainableDef>.AllDefsListForReading)
            {
                var rect = inRect.TakeTopPart(26f);
                Widgets.DrawHighlightIfMouseover(rect);
                TooltipHandler.TipRegion(rect, () => def.description, def.GetHashCode() * 397945);
                Widgets.Label(rect.TakeLeftPart(leftWidth), def.LabelCap);
                var level = pawn.training.GetSteps(def);
                var disabled = !pawn.training.CanAssignToTrain(def);
                var texture2D = SkillUI.SkillBarFillTex;

                if (!disabled) Widgets.FillableBar(rect, Mathf.Max(0.01f, level / (float)def.steps), texture2D, TexPawnEditor.SkillBarBGTex, false);
                rect.xMin += 3;
                Widgets.Label(rect, disabled ? "-" : level.ToString());
                if (!disabled && Widgets.ButtonImage(rect.TakeRightPart(30).ContractedBy(5), TexButton.Plus) && level < def.steps)
                {
                    pawn.training.steps[def] = ++level;
                    if (level >= def.steps) pawn.training.learned[def] = true;
                    PawnEditor.Notify_PointsUsed();
                }

                if (!disabled && Widgets.ButtonImage(rect.TakeRightPart(30).ContractedBy(5), TexButton.Minus) && level > 0)
                {
                    pawn.training.steps[def] = --level;
                    if (level < def.steps) pawn.training.learned[def] = false;
                    PawnEditor.Notify_PointsUsed();
                }

                inRect.yMin += 4;
            }
    }


    private static Action GetSetDelegate(Pawn pawn, bool max)
    {
        return () =>
        {
            foreach (var def in DefDatabase<TrainableDef>.AllDefsListForReading)
            {
                pawn.training.steps[def] = max ? def.steps : 0;
                pawn.training.learned[def] = max;
            }

            PawnEditor.Notify_PointsUsed();
        };
    }
}