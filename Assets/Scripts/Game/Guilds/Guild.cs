// Project:         Daggerfall Unity
// Copyright:       Copyright (C) 2009-2023 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: Hazelnut
// Contributors:    

using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Utility;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DaggerfallWorkshop.Game.Guilds
{
    /// <summary>
    ///  Guild objects define player status and benefits with the guild.
    /// </summary>
    public abstract class Guild : IGuild
    {
        #region Constants

        public const int defaultTrainingMax = 50;
        public const int memberTrainingCost = 100;
        public const int nonMemberTrainingCost = 400;

        protected const int DemotionId = 667;
        protected const int ExpulsionId = 668;

        #endregion

        #region Static Data

        public static int[] rankReqReputation = {  0, 10, 20, 30, 40, 50, 60, 70, 80, 90 };
        public static int[] rankReqSkillHigh =  { 22, 23, 31, 39, 47, 55, 63, 71, 79, 87 };
        public static int[] rankReqSkillLow =   {  4,  5,  9, 13, 17, 21, 25, 29, 33, 37 };
        public static int DefaultNumDaysToCheckForPromotion { get; set; } = 28;
        public static int NumDaysToCheckForPromotion { get; set; } = -1;

        #endregion

        #region Properties

        public abstract string[] RankTitles { get; }

        public abstract List<DFCareer.Skills> GuildSkills { get; }

        public abstract List<DFCareer.Skills> TrainingSkills { get; }

        public virtual bool IsSatisfyQuestReqByLevel()
        {
            return false;
        }

        #endregion

        #region Guild Ranks

        protected int rank = -1;

        protected int lastRankChange = 0;

        public int Rank { get { return rank; } set { rank = value; } }

        public virtual int GetNumDaysToCheckForPromotion()
        {
            var guildgroup = GameManager.Instance.GuildManager.GetGuildGroup(GetFactionId());
            return FormulaHelper.GetIndividualCheckForPromotion(guildgroup);
        }

        public virtual void ImportLastRankChange(uint timeOfLastRankChange)
        {
            // In classic, time of last rank change is measured by minute, not day
            DaggerfallDateTime classicTime = new DaggerfallDateTime();
            classicTime.FromClassicDaggerfallTime(timeOfLastRankChange);
            lastRankChange = CalculateDaySinceZero(classicTime);
        }

        public static bool IsDivine(int factionId)
        {
            return Enum.IsDefined(typeof(Temple.Divines), factionId);
        }

        public static bool IsOrder(int factionId)
        {
            return Enum.IsDefined(typeof(KnightlyOrder.Orders), factionId);
        }

        public virtual TextFile.Token[] UpdateRank(PlayerEntity playerEntity)
        {
            TextFile.Token[] tokens = null;

            // Have NumDaysToCheckForPromotion days passed?
            var days = GetNumDaysToCheckForPromotion();
            if (CalculateDaySinceZero(DaggerfallUnity.Instance.WorldTime.Now) >= lastRankChange + days)
            {
                // Does player qualify for promotion / demotion?
                int newRank = CalculateNewRank(playerEntity);
                if (newRank != rank)
                {
                    if (newRank > rank) {           // Promotion
                        tokens = TokensPromotion(newRank);
                    } else if (newRank < 0) {       // Expulsion
                        tokens = TokensExpulsion();
                        GameManager.Instance.GuildManager.RemoveMembership(this);
                    } else if (newRank < rank) {    // Demotion
                        tokens = TokensDemotion();
                    }
                    rank = newRank;
                    lastRankChange = CalculateDaySinceZero(DaggerfallUnity.Instance.WorldTime.Now);
                }
            }
            return tokens;
        }

        protected virtual int CalculateNewRank(PlayerEntity playerEntity)
        {
            // Check reputation & skills
            int rep = GetReputation(playerEntity);
            if (rep < 0)
                return -1;  // Expelled.

            int high = 0, low = 0, r;
            for (r = 0; r < rankReqReputation.Length; r++)
            {
                CalculateNumHighLowSkills(playerEntity, r, out high, out low);
                if (rep < rankReqReputation[r] || high < 1 || low + high < 2)
                    break;
            }
            Debug.LogFormat("rep: {0} high#: {1} low#: {2} new rank: {3}", rep, high, low, r - 1);
            return --r;
        }

        protected void CalculateNumHighLowSkills(PlayerEntity playerEntity, int rank, out int high, out int low)
        {
            int rep = GetReputation(playerEntity);

            high = FormulaHelper.CalculateNumHighSkills(playerEntity, rep, GuildSkills, rankReqReputation, rankReqSkillHigh, rank);
            low = FormulaHelper.CalculateNumLowSkills(playerEntity, rep, GuildSkills, rankReqReputation, rankReqSkillHigh, rankReqSkillLow, rank);
        }

        public static int CalculateDaySinceZero(DaggerfallDateTime date)
        {
            return (date.Year * DaggerfallDateTime.DaysPerYear) + date.DayOfYear;
        }

        public abstract TextFile.Token[] TokensPromotion(int newRank);

        public virtual TextFile.Token[] TokensDemotion()
        {
            return DaggerfallUnity.Instance.TextProvider.GetRandomTokens(DemotionId);
        }

        public virtual TextFile.Token[] TokensExpulsion()
        {
            return DaggerfallUnity.Instance.TextProvider.GetRandomTokens(ExpulsionId);
        }

        #endregion

        #region Guild Membership and Faction Data

        public virtual bool IsMember()
        {
            return (rank >= 0);
        }

        public abstract int GetFactionId();

        public virtual int GetReputation(PlayerEntity playerEntity)
        {
            return playerEntity.FactionData.GetReputation(GetFactionId());
        }

        public virtual string GetGuildName()
        {
            return GetAffiliation();
        }

        public virtual string GetAffiliation()
        {
            FactionFile.FactionData factionData;
            if (GameManager.Instance.PlayerEntity.FactionData.GetFactionData(GetFactionId(), out factionData))
                return factionData.name;
            return "unknown-guild";
        }

        public virtual string GetTitle()
        {
            return IsMember() ? RankTitles[rank] : GameManager.Instance.PlayerEntity.Name;
        }

        #endregion

        #region Benefits

        public virtual bool CanRest()
        {
            return false;
        }

        public virtual bool HallAccessAnytime()
        {
            return false;
        }

        public virtual bool FreeHealing()
        {
            return FormulaHelper.FreeHealing(GetFactionId());
        }

        public virtual bool FreeMagickaRecharge()
        {
            return false;
        }

        public virtual int AlterReward(int reward)
        {
            return reward;
        }

        public virtual int ReducedRepairCost(int price)
        {
            return price;
        }

        public virtual int ReducedIdentifyCost(int price)
        {
            return price;
        }

        public virtual int ReducedCureCost(int price)
        {
            return price;
        }

        #endregion

        #region Special benefits:

        public virtual bool FreeTavernRooms()
        {
            return false;
        }

        public virtual bool FreeShipTravel()
        {
            return false;
        }

        public virtual int FastTravel(int duration)
        {
            return duration;
        }

        public virtual int DeepBreath(int duration)
        {
            return duration;
        }

        public virtual bool AvoidDeath()
        {
            return false;
        }

        #endregion

        #region Service Access:

        public virtual bool CanAccessLibrary()
        {
            return false;
        }

        public virtual bool CanAccessService(GuildServices service)
        {
            switch (service)
            {
                case GuildServices.Training:
                    return IsMember();
                case GuildServices.Quests:
                    return true;
                case GuildServices.Repair:
                    return IsMember();
                case GuildServices.Identify:
                    return true;
                case GuildServices.BuySpells:
                    return false;
                case GuildServices.BuySpellsMages:
                    return true;
                case GuildServices.Donate:
                    return true;
                case GuildServices.CureDisease:
                    return true;
                default:
                    return false;
            }
        }

        #endregion

        #region Service: Training

        public virtual int GetTrainingMax(DFCareer.Skills skill)
        {
            return FormulaHelper.GetTrainingMax(GameManager.Instance.PlayerEnterExit.BuildingDiscoveryData.quality, skill, rank);
        }

        public virtual int GetTrainingPrice()
        {
            return FormulaHelper.GetTrainingPrice(IsMember(), memberTrainingCost, nonMemberTrainingCost, rank);
        }

        #endregion

        #region Joining

        public virtual void Join()
        {
            rank = 0;
            lastRankChange = CalculateDaySinceZero(DaggerfallUnity.Instance.WorldTime.Now);
        }

        public virtual void Leave()
        {
        }

        public virtual bool IsEligibleToJoin(PlayerEntity playerEntity)
        {
            // Check reputation & skills
            int rep = playerEntity.FactionData.GetReputation(GetFactionId());
            int high, low;
            CalculateNumHighLowSkills(playerEntity, 0, out high, out low);
            return (rep >= rankReqReputation[0] && high > 0 && low + high > 1);
        }

        public abstract TextFile.Token[] TokensIneligible(PlayerEntity playerEntity);

        public abstract TextFile.Token[] TokensEligible(PlayerEntity playerEntity);

        public abstract TextFile.Token[] TokensWelcome();

        #endregion


        #region Serialization

        public virtual GuildMembership_v1 GetGuildData()
        {
            return new GuildMembership_v1() { rank = rank, lastRankChange = lastRankChange };
        }

        public virtual void RestoreGuildData(GuildMembership_v1 data)
        {
            rank = data.rank;
            lastRankChange = data.lastRankChange;
        }

        #endregion


        #region Macro Handling

        public virtual MacroDataSource GetMacroDataSource()
        {
            return new GuildMacroDataSource(this);
        }

        /// <summary>
        /// MacroDataSource context sensitive methods for guilds.
        /// </summary>
        protected class GuildMacroDataSource : MacroDataSource
        {
            private readonly Guild parent;
            public GuildMacroDataSource(Guild guild)
            {
                parent = guild;
            }

            public override string Amount()
            {
                return parent.GetTrainingPrice().ToString();
            }

            public override string GuildTitle()
            {
                return parent.GetTitle();
            }
        }

        #endregion
    }
}
