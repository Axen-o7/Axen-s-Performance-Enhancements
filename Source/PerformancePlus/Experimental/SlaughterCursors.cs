// slaughter selection cursors.
using HarmonyLib;
using Verse;
using RimWorld;
using static Verse.AutoSlaughterManager;
namespace PerformancePlus.Experimental;

internal static class SlaughterCursors
{
    internal static void Apply(Harmony h) => h.Patch(AccessTools.PropertyGetter(typeof(AutoSlaughterManager), nameof(AutoSlaughterManager.AnimalsToSlaughter)), prefix: new HarmonyMethod(typeof(SlaughterCursors), nameof(Before)));
    private static bool Before(AutoSlaughterManager __instance, ref List<Pawn> __result)
    {
        if (__instance.cacheDirty)
        {
            try
            {
                __instance.animalsToSlaughterCached.Clear();
                foreach (AutoSlaughterConfig config in __instance.configs)
                {
                    if (!config.AnyLimit)
                    {
                        continue;
                    }
                    tmpAnimals.Clear();
                    tmpAnimalsMale.Clear();
                    tmpAnimalsMaleYoung.Clear();
                    tmpAnimalsFemale.Clear();
                    tmpAnimalsFemaleYoung.Clear();
                    tmpAnimalsPregnant.Clear();
                    foreach (Pawn spawnedColonyAnimal in __instance.map.mapPawns.SpawnedColonyAnimals)
                    {
                        if (spawnedColonyAnimal.def != config.animal || !CanAutoSlaughterNow(spawnedColonyAnimal) || (!config.allowSlaughterBonded && spawnedColonyAnimal.relations.GetDirectRelationsCount(PawnRelationDefOf.Bond) > 0))
                        {
                            continue;
                        }
                        if (spawnedColonyAnimal.gender == Gender.Male)
                        {
                            if (spawnedColonyAnimal.ageTracker.CurLifeStage.reproductive)
                            {
                                tmpAnimalsMale.Add(spawnedColonyAnimal);
                            }
                            else
                            {
                                tmpAnimalsMaleYoung.Add(spawnedColonyAnimal);
                            }
                            tmpAnimals.Add(spawnedColonyAnimal);
                        }
                        else if (spawnedColonyAnimal.gender == Gender.Female)
                        {
                            if (spawnedColonyAnimal.ageTracker.CurLifeStage.reproductive)
                            {
                                if (!spawnedColonyAnimal.health.hediffSet.HasHediff(HediffDefOf.Pregnant))
                                {
                                    tmpAnimalsFemale.Add(spawnedColonyAnimal);
                                    tmpAnimals.Add(spawnedColonyAnimal);
                                }
                                else if (config.allowSlaughterPregnant)
                                {
                                    tmpAnimalsPregnant.Add(spawnedColonyAnimal);
                                }
                            }
                            else
                            {
                                tmpAnimalsFemaleYoung.Add(spawnedColonyAnimal);
                                tmpAnimals.Add(spawnedColonyAnimal);
                            }
                        }
                        else
                        {
                            tmpAnimals.Add(spawnedColonyAnimal);
                        }
                    }
                    tmpAnimals.SortByDescending((Pawn a) => a.ageTracker.AgeBiologicalTicks);
                    tmpAnimalsMale.SortByDescending((Pawn a) => a.ageTracker.AgeBiologicalTicks);
                    tmpAnimalsMaleYoung.SortByDescending((Pawn a) => a.ageTracker.AgeBiologicalTicks);
                    tmpAnimalsFemale.SortByDescending((Pawn a) => a.ageTracker.AgeBiologicalTicks);
                    tmpAnimalsFemaleYoung.SortByDescending((Pawn a) => a.ageTracker.AgeBiologicalTicks);
                    if (config.allowSlaughterPregnant)
                    {
                        tmpAnimalsPregnant.SortByDescending((Pawn a) => a.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Pregnant).Severity);
                        tmpAnimalsFemale.AddRange(tmpAnimalsPregnant);
                        tmpAnimals.AddRange(tmpAnimalsPregnant);
                    }
                    var selected = new HashSet<Pawn>();
                    Select(tmpAnimalsFemale, config.maxFemales, selected, __instance.animalsToSlaughterCached);
                    Select(tmpAnimalsFemaleYoung, config.maxFemalesYoung, selected, __instance.animalsToSlaughterCached);
                    Select(tmpAnimalsMale, config.maxMales, selected, __instance.animalsToSlaughterCached);
                    Select(tmpAnimalsMaleYoung, config.maxMalesYoung, selected, __instance.animalsToSlaughterCached);
                    if (config.maxTotal != -1)
                    {
                        int remaining = tmpAnimals.Count - selected.Count;
                        foreach (var animal in tmpAnimals)
                        {
                            if (remaining <= config.maxTotal) break;
                            if (selected.Contains(animal)) continue;
                            __instance.animalsToSlaughterCached.Add(animal);
                            remaining--;
                        }
                    }

                }
                __instance.cacheDirty = false;
            }
            finally
            {
                tmpAnimals.Clear();
                tmpAnimalsMale.Clear();
                tmpAnimalsFemale.Clear();
                tmpAnimalsMaleYoung.Clear();
                tmpAnimalsFemaleYoung.Clear();
                tmpAnimalsPregnant.Clear();
            }
        }
        __result = __instance.animalsToSlaughterCached; return false;
    }
    private static void Select(List<Pawn> animals, int maximum, HashSet<Pawn> selected, List<Pawn> output)
    {
        if (maximum == -1) return;
        for (int i = 0; i < animals.Count - maximum; i++)
        { var animal = animals[i]; selected.Add(animal); output.Add(animal); }
    }
}
