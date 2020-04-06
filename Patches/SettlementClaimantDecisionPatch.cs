using System;
using System.Reflection;
using System.Windows.Forms;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.Core;
using TaleWorlds.Library;
using System.Linq;

namespace BannerlordTweaks.Patches
{
    [HarmonyPatch(typeof(SettlementClaimantDecision), "CalculateMeritOfOutcome")]
    public class SettlementClaimantDecisionPatch
    {
        private static float Calculate(SettlementClaimantDecision __instance, DecisionOutcome candidateOutcome)
        {
            Clan clan = null;
            Hero _capturerHero = null;
            try
            {
                {
                    var type = __instance.GetType();
                    var property = type.GetField("_capturerHero", BindingFlags.NonPublic | BindingFlags.Instance);
                    _capturerHero = (Hero)property.GetValue(__instance);

                    if (_capturerHero == null)
                    {
                        MessageBox.Show("Capturer Hero was null.");
                        return 0.0f;
                    }
                }


                {
                    var type = candidateOutcome.GetType();
                    var field = type.GetField("Clan", BindingFlags.Public | BindingFlags.Instance);

                    clan = (Clan)field.GetValue(candidateOutcome);
                    if (_capturerHero == null)
                    {
                        MessageBox.Show("Clan was null.");
                        return 0.0f;
                    }

                }
            }
            catch(Exception ex)
            {
                MessageBox.Show($"Exception: {ex.Message}");
                return 0.0f;
            }


            float sumSettlementValue = 0.0f;
            int ownedSettlements = 0;
            float num3 = Campaign.MapDiagonal + 1f;
            float maximumDistance = Campaign.MapDiagonal + 1f;
            foreach (Settlement fromSettlement in Settlement.All)
            {
                if (fromSettlement.OwnerClan == clan && fromSettlement.IsFortification)
                {
                    sumSettlementValue += fromSettlement.GetSettlementValueForFaction((IFaction)clan.Kingdom);
                    float distance;
                    if (Campaign.Current.Models.MapDistanceModel.GetDistance(fromSettlement, __instance.Settlement, maximumDistance, out distance))
                    {
                        if ((double)distance < (double)num3)
                        {
                            maximumDistance = num3;
                            num3 = distance;
                        }
                        else if ((double)distance < (double)maximumDistance)
                        {
                            maximumDistance = distance;
                        }
                            
                    }
                    ++ownedSettlements;
                }
            }


            double num4 = (double)Campaign.AverageDistanceBetweenTwoTowns * 1.5;
            float val1 = (float)(num4 * 0.25);
            float val2 = (float)num4;

            if ((double)maximumDistance < (double)Campaign.MapDiagonal)
            {
                val2 = (float)(((double)maximumDistance + (double)num3) / 2.0);
            }
            else if ((double)num3 < (double)Campaign.MapDiagonal)
            {
                val2 = num3;
            }
            float distanceCalc = (float)Math.Pow(num4 / (double)Math.Max(val1, Math.Min(400f, val2)), 0.5);


            float noblePartiesStrengthValue = 0.0f;
            foreach (Hero noble in clan.Nobles)
            {
                if (noble.PartyBelongedTo != null)
                {
                    noblePartiesStrengthValue += noble.PartyBelongedTo.Party.TotalStrength;
                }
            }

            float settlementValueForFaction = __instance.Settlement.GetSettlementValueForFaction((IFaction)clan.Kingdom);


            float noblesInClanVal = clan.Nobles.Count() * 25.0f;
            float commandersInClanVal = clan.CommanderHeroes.Count * 50.0f;
            

            float isleaderVal = 0;
            if (clan.Leader == clan.Kingdom.Leader)
            {
                isleaderVal = 75.0f;
            }

            float noSettlementVal = 0;
            if (ownedSettlements == 0)
            {
                noSettlementVal = 25.0f;
            }

            float isPlayerVal = 0;
            if (clan == Clan.PlayerClan)
            {
                isPlayerVal = 50.0f;
            }

            float isCapturerVal = 0;
            if (_capturerHero != null && _capturerHero.Clan == clan)
            {
                isCapturerVal = 25.0f;
            }

            
            float num14 = (float)(
                (
                clan.Tier * 50.0 
                + noblePartiesStrengthValue 
                + commandersInClanVal 
                + noblesInClanVal 
                + noSettlementVal 
                + isPlayerVal 
                + isCapturerVal 
                + isleaderVal)
                
                / (sumSettlementValue + settlementValueForFaction)
                
                ) * distanceCalc * 20000f;


            if (noblesInClanVal <= 0 && clan != Clan.PlayerClan)
                return 0.0f;


            float displaySum = clan.Tier * 50.0f
                + noblePartiesStrengthValue
                + commandersInClanVal
                + noblesInClanVal
                + noSettlementVal
                + isPlayerVal
                + isCapturerVal
                + isleaderVal;

            float mulDist = displaySum * distanceCalc;

            MessageBox.Show($"Clan: {clan.Name}\n" +
                $" + Tier: {clan.Tier} * 50\n" +
                $" + Noble Parties: {noblePartiesStrengthValue}\n" +
                $" + Commanders In Clan: {commandersInClanVal}\n" +
                $" + Nobles In Clan: {noblesInClanVal}\n" +
                $" + noSettlementBonus: {noSettlementVal}\n" +
                $" + isPlayerBonus: {isPlayerVal}\n" +
                $" + isCapturerBonus: {isCapturerVal}\n" +
                $" + isLeaderBonus: {isleaderVal}\n" +
                $" == {displaySum}\n" +
                $" .. * DistanceCalc: {distanceCalc}\n" +
                $" == {mulDist}\n" +
                $" / SettlementsValue {(sumSettlementValue + settlementValueForFaction) / 1000.0f}\n" +
                $"\n" +
                $"Total: {num14}");

            return num14;
        }

        private static void Postfix(SettlementClaimantDecision __instance, DecisionOutcome candidateOutcome, ref float __result)
        {
            float myRes = Calculate(__instance, candidateOutcome);
            if (Math.Abs(myRes - __result) > 0.001)
            {
                //MessageBox.Show($"My Result: {myRes} -> {__result} actual");
            }
        }

        static bool Prepare()
        {
            return true;
        }
    }
}
