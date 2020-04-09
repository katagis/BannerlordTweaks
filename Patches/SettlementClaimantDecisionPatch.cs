using System;
using System.Reflection;
using System.Windows.Forms;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.Core;
using TaleWorlds.Library;
using System.Linq;
using System.Collections.Generic;

namespace BannerlordTweaks.Patches
{



    [HarmonyPatch(typeof(SettlementClaimantDecision), "CalculateMeritOfOutcome")]
    public class SettlementClaimantDecisionPatch
    {
        public static object AttemptGet(object instance, string variable)
        {
            var type = instance.GetType();
            if (type == null)
            {
                return null;
            }

            var property = type.GetField(variable, BindingFlags.NonPublic | BindingFlags.Instance);

            if (property == null)
            {
                property = type.GetField(variable, BindingFlags.Public | BindingFlags.Instance);
                if (property == null)
                {
                    return null;
                }
            }

            return property.GetValue(instance);
        }


        public static Hero DetermineStongestPartyClanHero(Hero armyLeader, Settlement settlement)
        {
            if (armyLeader.GetPosition().Distance(settlement.GetPosition()) > Campaign.AverageDistanceBetweenTwoTowns / 2)
            {
                return armyLeader;
            }

            if (armyLeader.PartyBelongedTo == null || armyLeader.PartyBelongedTo.Army == null)
            {
                return armyLeader;
            }

            MobileParty strongestParty = armyLeader.PartyBelongedTo;
            float strongestValue = strongestParty.Party.TotalStrength + 20.0f;

            foreach (MobileParty party in armyLeader.PartyBelongedTo.AttachedParties)
            {
                if (party.GetPosition().Distance(armyLeader.GetPosition()) > 1.0f)
                {
                    continue;
                }

                if (strongestParty.Party.TotalStrength < party.Party.TotalStrength)
                {
                    strongestParty = party;
                    strongestValue = party.Party.TotalStrength;
                }
            }

            if (strongestParty.IsLeaderless || strongestParty.LeaderHero == null)
            {
                return armyLeader;
            }

            return strongestParty.LeaderHero;
        }

        public static float DetermineClanContribution(Clan clan, Hero armyLeader, Settlement settlement)
        {
            float errValue = armyLeader.Clan == clan ? 1.0f : 0.1f; // Value returned when we are not confident we got the right army

            if (armyLeader.GetPosition().Distance(settlement.GetPosition()) > Campaign.AverageDistanceBetweenTwoTowns / 2)
            {
                return errValue;
            }

            if (armyLeader.PartyBelongedTo == null || armyLeader.PartyBelongedTo.Army == null)
            {
                return errValue;
            }

            Army army = armyLeader.PartyBelongedTo.Army;
            var partyList = army.LeaderPartyAndAttachedParties;

            Dictionary<Clan, float> clanPower = new Dictionary<Clan, float>();

            float sumPower = 0.0f;
            foreach (MobileParty party in partyList)
            {
                if (party.LeaderHero == null)
                {
                    continue;
                }
                var partysClan = party.LeaderHero.Clan;

                float power = 0.0f;
                clanPower.TryGetValue(partysClan, out power);
                clanPower[partysClan] = power + party.Party.TotalStrength;

                sumPower += party.Party.TotalStrength;
            }

            float requestedPower = 0.0f;
            clanPower.TryGetValue(clan, out requestedPower);
            return requestedPower / Math.Max(1.0f, sumPower);
        }
         

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
                }
                if (_capturerHero != null)
                {
                    if (__instance.RemainingHours >= 20)
                    {
                        _capturerHero = DetermineStongestPartyClanHero(_capturerHero, __instance.Settlement);
                    }
                }


                {
                    var type = candidateOutcome.GetType();
                    var field = type.GetField("Clan", BindingFlags.Public | BindingFlags.Instance);
                     
                    clan = (Clan)field.GetValue(candidateOutcome);
                    if (clan == null)
                    {
                        MessageBox.Show("Clan was null.");
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


            float partiesStrengthValue = 0.0f;
            //foreach (MobileParty party in clan.Parties)
            //{
            //    partiesStrengthValue += party.Party.TotalStrength / 5.0f;
            //}

            //partiesStrengthValue = (float)Math.Pow(partiesStrengthValue, 0.7) / 2.0f;

            float settlementValueForFaction = __instance.Settlement.GetSettlementValueForFaction((IFaction)clan.Kingdom);


            float noblesInClanVal = clan.Nobles.Count() * 15.0f;
            float commandersInClanVal = clan.CommanderHeroes.Count * 25.0f;
            

            float isleaderVal = 0;
            if (clan.Leader == clan.Kingdom.Leader)
            {
                isleaderVal = 40.0f;
            }

            float noSettlementVal = 0;
            if (ownedSettlements == 0)
            {
                noSettlementVal = 100.0f;
            }

            float isPlayerVal = 0;
            if (clan == Clan.PlayerClan)
            {
                isPlayerVal = 40.0f;
            }

            bool isCapturer = _capturerHero != null && _capturerHero.Clan == clan;
            float isCapturerVal = 0;
            if (isCapturer)
            {
                isCapturerVal = 100.0f;
            }


            sumSettlementValue = (float)Math.Pow(sumSettlementValue, 0.77);

            //float influenceValue = clan.Influence > 1.0f ? (float)Math.Pow(clan.Influence, 0.5) : -10.0f;

            float num14 = (float)(
                (
                clan.Tier * 20.0 
                + partiesStrengthValue
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


            bool hasClanContribution = false;
            float clanContribution = 1.0f;
            if (_capturerHero != null)
            {
                if (__instance.RemainingHours >= 20)
                {
                    hasClanContribution = true;
                    clanContribution = DetermineClanContribution(clan, _capturerHero, __instance.Settlement);
                }
            }

            if (!hasClanContribution)
            {
                return num14;
            }


            return Math.Max(clanContribution, 0.01f) * num14 * 2;


            //float displaySum = clan.Tier * 20.0f
            //    + partiesStrengthValue 
            //    + influenceValue
            //    + commandersInClanVal
            //    + noblesInClanVal
            //    + noSettlementVal 
            //    + isPlayerVal
            //    + isCapturerVal
            //    + isleaderVal;

            //float mulDist = displaySum * distanceCalc;
            //MessageBox.Show($"Clan: {clan.Name}\n" +
            //    $" + Tier: {clan.Tier} * 50\n" +
            //    $" + InfluenceVal: {influenceValue}\n" +
            //    $" + Commanders In Clan: {commandersInClanVal}\n" +
            //    $" + Nobles In Clan: {noblesInClanVal}\n" +
            //    $" + noSettlementBonus: {noSettlementVal}\n" +
            //    $" + isPlayerBonus: {isPlayerVal}\n" +
            //    $" + isCapturerBonus: {isCapturerVal}\n" +
            //    $" + isLeaderBonus: {isleaderVal}\n" +
            //    $" == {displaySum}\n" +
            //    $" .. * DistanceCalc: {distanceCalc}\n" +
            //    $" == {mulDist}\n" +
            //    $" / SettlementsValue {(sumSettlementValue + settlementValueForFaction) / 1000.0f}\n" +
            //    $"\n" +
            //    $"Total: {num14}");


            // return num14;
        }

        private static void Postfix(SettlementClaimantDecision __instance, DecisionOutcome candidateOutcome, ref float __result)
        {
            float myRes = Calculate(__instance, candidateOutcome);
            if (Math.Abs(myRes - __result) > 0.001)
            {
                //MessageBox.Show($"My Result: {myRes} -> {__result} actual");
            }
            __result = myRes;
        }

        static bool Prepare()
        {
            return true; 
        }
    }
}
