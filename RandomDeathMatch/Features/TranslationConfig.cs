﻿using CommandSystem;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheRiptide
{
    public class TranslationConfig
    {
        [Description("Main")]
        public string DntMsg { get; set; } = "<size=64><b><color=#FF0000>Warning! You have DNT enabled so any Experience and Rank you gain will be lost at the end of the round! Your Config, Preferences, Killstreak, Statistics, Role and Loadout cant be saved because you have DNT enabled in you game settings. To remain fair your kills/deaths towards ranked players will be ignored.</color></b></size>";

        [Description("Hitbox")]
        public string Body { get; set; } = "Body";
        public string Limb { get; set; } = "Limb";
        public string Head { get; set; } = "Head";

        [Description("killfeed messages")]
        public string FirearmKill { get; set; } = "{killer} shot  {victim} in  the  {hitbox} with  {gun}";
        public string ExplosionKill { get; set; } = "{killer} fragged {victim}";
        public string ExplosionSelfKill { get; set; } = "{victim} humiliated  themselves  with  a  <b><color=#eb0d47>Frag grenade</color></b>";
        public string JailbirdHeadKill { get; set; } = "{killer} bonked {victim} on the {hitbox} with the <b><color=#eb0d47>Jailbird</color></b>";
        public string JailbirdNormalKill { get; set; } = "{killer} slapped {victim} on the {hitbox} with the <b><color=#eb0d47>Jailbird</color></b>";
        public string Scp018Kill { get; set; } = "{killer} pummeled {victim} with <b><color=#eb0d47>SCP 018</color></b>";
        public string Scp018SelfKill { get; set; } = "{victim} humiliated themselves by failing to catch their own ball";
        public string DistruptorKill { get; set; } = "{killer} atomised {victim} in the {hitbox} with the <b><color=#eb0d47>Particle disruptor</color></b>";
        public string DistruptorSelfKill { get; set; } = "{victim} humiliated themselves with the <b><color=#eb0d47>Particle disruptor</color></b>";
        public string CustomReasonKill { get; set; } = "{victim} slain: <b><color=#43BFF0>{reason}</color></b>";
        public string FailedFirstGrade { get; set; } = "{victim} <b><color=#eb0d47>could not read so they left the match humiliating themselves</color></b>";
        public string SelfKill { get; set; } = "<b><color=#eb0d47>{victim}</color></b> humiliated  themselves";

        [Description("killstreak")]
        public string GlobalKillstreak { get; set; } = "<b>{killstreak} <color=#43BFF0>{name}</color></b> is on a <b><color=#FF0000>{count}</color></b> kill streak";
        public string PrivateKillstreak { get; set; } = "Kill streak <b><color=#FF0000>{count}</color></b>";
        public string GlobalKillstreakEnded { get; set; } = "<b>{killer_killstreak} <color=#43BFF0>{killer}</color></b> ended <b>{victim_killstreak} <color=#43BFF0>{victim}'s </color></b>" + "<b><color=#FF0000>{count}</color></b> kill streak";

        [Description("loadout")]
        public string CustomisationHint { get; set; } = "<b>CHECK INVENTORY! <color=#FF0000>Right Click O5 to select gun</color></b>";
        public List<string> CustomisationDenied { get; set; } = new List<string>() {
            "<color=#f8d107>Loadout can not be customised after shooting gun/using item</color>",
            "<color=#43BFF0>Wait until next respawn</color>" };
        public string LastWeapon { get; set; } = "<color=#f8d107>last gun can not be deleted!</color>";
        public string WeaponBanned { get; set; } = "<color=#FF0000>{weapon} is currently banned</color>";

        [Description("lobby")]
        public string Teleport { get; set; } = "<color=#43BFF0>you will be teleported after selecting a gun</color>";
        public List<string> WaitingForPlayers { get; set; } = new List<string>(){
            "<color=#43BFF0>Waiting for 1 player to join</color>",
            "<color=#43BFF0>You get to choose the starting area!</color>"};
        public string Respawn { get; set; } = "<b><color=#FFFF00>Left/Right click to respawn</color></b>";
        public string Attachments { get; set; } = "<b><color=#FF0000>Tab to edit attachments/presets</color></b>";
        public string Teleporting { get; set; } = "<color=#43BFF0>Teleporting in 7 seconds</color>";
        public string TeleportCancel { get; set; } = "<color=#43BFF0>Open [MAIN MENU] to cancel</color> - <color=#FF0000>Right Click O5</color>";
        public string FastTeleport { get; set; } = "<color=#43BFF0>loadout set, teleporting in 3 seconds</color>";
        public string SpectatorMode { get; set; } = "spectate 939 to respawn, if bugged you may need to leave and rejoin to respawn";

        [Description("main menu")]
        public string MainMenu { get; set; } = "<b><color=#43BFF0>[MAIN MENU]</color></b> <b><color=#FF0000>RIGHT CLICK TO SELECT</color></b>";
        public string BackToMainMenu { get; set; } = "<color=#5d318c>[O5]</color> = <b>Back to <color=#43BFF0>[MAIN MENU]</color></b>";
        public string SaveAndExit { get; set; } = "<color=#5d318c>[O5]</color> = <b><color=#5d318c>Save and Exit</color></b>";
        public string CustomiseLoadout { get; set; } = "<color=#e7d77b>[SCIENTIST]</color> = <b><color=#FF0000>Customise Loadout - </color><color=#43BFF0>[GUN SLOT]</color></b>";
        public string KillstreakRewardSystem { get; set; } = "<color=#e1ab21>[RESEARCH SUPERVISOR]</color> = <b><color=#43BFF0>[KILLSTREAK REWARD SYSTEM]</color></b>";
        public string Role { get; set; } = "<color=#bd8f86>[CONTAINMENT ENGINEER]</color> = <b><color=#43BFF0>[ROLE]</color></b>";
        public string Preferences { get; set; } = "<color=#bd1a4a>[FACILITY MANAGER]</color> = <b><color=#43BFF0>[PREFERENCES]</color></b>";

        [Description("gun slot menu")]
        public string GunSlotMenu { get; set; } = "<b><color=#43BFF0>[GUN SLOT]</color></b> <b><color=#FF0000>RIGHT CLICK TO SELECT</color></b>";
        public string Primary { get; set; } = "<color=#bd1a4a>[FACILITY MANAGER]</color> = <b><color=#FF0000>Primary - </color><color=#43BFF0>[GUN CLASS]</color></b>";
        public string Secondary { get; set; } = "<color=#217b7b>[ZONE MANAGER]</color> = <b>Secondary - <color=#43BFF0>[GUN CLASS]</color></b>";
        public string HeavyPrimary { get; set; } = "<color=#1b43cb>[CAPTAIN]</color> = <b><color=#FF0000>Primary - </color><color=#43BFF0>[GUN CLASS]</color></b>";
        public string HeavySecondary { get; set; } = "<color=#177dde>[SERGEANT]</color> = <b>Secondary - <color=#43BFF0>[GUN CLASS]</color></b>";
        public string HeavyTertiary { get; set; } = "<color=#accfe1>[PRIVATE]</color> = <b>Tertiary - <color=#43BFF0>[GUN CLASS]</color></b>";

        [Description("gun class menu")]
        public string GunClassMenu { get; set; } = "<b><color=#43BFF0>[GUN CLASS]</color></b> <b><color=#FF0000>RIGHT CLICK TO SELECT</color></b>";
        public string MtfGuns { get; set; } = "<color=#1b43cb>[CAPTAIN]</color> = <b><color=#1b43cb>[MTF GUNS]</color></b>";
        public string ChaosGuns { get; set; } = "<color=#008f1c>[CHAOS]</color> = <b><color=#008f1c>[CHAOS GUNS]</color></b>";

        [Description("gun menu")]
        public string MtfGunMenu { get; set; } = "<b><color=#1b43cb>[MTF GUNS]</color></b> - <b><color=#FF0000>RIGHT CLICK TO SELECT</color></b>";
        public string ChaosGunMenu { get; set; } = "<b><color=#008f1c>[CHAOS GUNS]</color></b> - <b><color=#FF0000>RIGHT CLICK TO SELECT</color></b>";
        public string GunSelected { get; set; } = "<color=#43BFF0>{gun}</color> added to your loadout as the <color=#FF0000>{slot}</color> weapon";

        [Description("killstreak reward system menu")]
        public string KillstreakRewardMenu { get; set; } = "<b><color=#43BFF0>[KILLSTREAK REWARD SYSTEM]</color></b> - <b><color=#FF0000>RIGHT CLICK TO SELECT</color></b>";
        public string KillstreakSelected { get; set; } = "{killstreak} selected as your killstreak reward system";
        public string CurrentKillstreakSelected { get; set; } = "Current killstreak reward system selected: {killstreak}</color>";

        [Description("role menu")]
        public string RoleMenu { get; set; } = "<b><color=#43BFF0>[ROLE]</color></b> <b><color=#FF0000>RIGHT CLICK TO SELECT</color></b>";
        public string ClassD { get; set; } = "<color=#bdafe4>[JANITOR]</color> = <b><color=#FF8E00>Class-D</color></b>";
        public string Scientist { get; set; } = "<color=#e7d77b>[SCIENTIST]</color> = <b><color=#FFFF7C>Scientist</color></b>";
        public string Guard { get; set; } = "<color=#5B6370>[GUARD]</color> = <b><color=#5B6370>Facility Guard</color></b>";
        public string Private { get; set; } = "<color=#accfe1>[PRIVATE]</color> = <b><color=#accfe1>NTF Private</color></b>";
        public string Sergeant { get; set; } = "<color=#177dde>[SERGEANT]</color> = <b><color=#177dde>NTF Sergeant</color></b>";
        public string Captain { get; set; } = "<color=#1b43cb>[CAPTAIN]</color> = <b><color=#1b43cb>NTF Captain</color></b>";
        public string Chaos { get; set; } = "<color=#008f1c>[CHAOS]</color> = <b><color=#008f1c>Chaos</color></b>";
        public string RoleSelected { get; set; } = "{role} selected as role";

        [Description("preferences menu")]
        public string PreferencesMenu { get; set; } = "<b><color=#43BFF0>[PREFERENCES]</color></b> - <b><color=#FF0000>RIGHT CLICK TO SELECT</color></b>";
        public string Stats { get; set; } = "<color=#eb0d47>[SCIENTIST]</color> = <b><color=#43BFF0>[STATS]</color></b>";
        public string Spectator { get; set; } = "<color=#eb0d47>[FLASH LIGHT]</color> = <b>Enable spectator mode</b>";
        public string EnableRage { get; set; } = "<color=#eb0d47>[COIN]</color> = <b>Enable [DATA EXPUNGED]</b>";
        public string DeleteData { get; set; } = "<color=#eb0d47>[JANITOR]</color> = <b>Delete Data stats/configs/ranks/xp/preferences (can not be undone)</b>";
        public string LeaderBoard { get; set; } = "<color=#eb0d47>[SCP1576]</color> = <b><color=#43BFF0>[LEADER BOARD]</color></b>";

        [Description("stats menu")]
        public string StatsMenu { get; set; } = "<b><color=#43BFF0>[STATS]</color></b> - <b><color=#FF0000>RIGHT CLICK TO SELECT</color></b>";
        public string BackToPreferences { get; set; } = "<color=#5d318c>[O5]</color> = <b>Back to <color=#43BFF0>[PREFERENCES]</color></b>";

        [Description("confirm delete data menu")]
        public string DeleteDataMenu { get; set; } = "<b><color=#43BFF0>[DATA DELETION CONFIRMATION]</color></b> - <b><color=#FF0000>RIGHT CLICK TO SELECT</color></b>";
        public string AreYouSure { get; set; } = "<b>Are you sure? You must have 'Do Not Track' on for this to work. <color=#FF0000>deletion can not be undone.</color> <color=#43BFF0>If sure, right click on the janitor keycard to delete your data</color></b>";
        public string FailedToDeleteData { get; set; } = "<b>failed to delete data, you must have 'Do Not Track' on in your settings</b>";
        public string DeletedData { get; set; } = "data deleted";

        [Description("leader board menu")]
        public string LeaderBoardMenu { get; set; } = "<b><color=#43BFF0>[LEADER BOARD]</color></b> - <b><color=#FF0000>RIGHT CLICK TO SELECT</color></b>";

        [Description("rooms")]
        public string SecondPlayerJoined { get; set; } = "Player <color=#43BFF0>{name}</color> joined, waiting for them to select a loadout";
        public string SecondPlayerHelp { get; set; } = "<color=#43BFF0>{name}</color> is struggling to set his loadout please help him with your radio";
        public string Decontaminating { get; set; } = "<color=#FF0000>DECONTAMINATNG!</color>";
        public string Caution { get; set; } = "<color=#FFFF00>Caution! Room decontamination in {time}</color>";
        public string Warning { get; set; } = "<color=#FF8000>Warning! Room decontamination in {time}</color>";
        public string Danger { get; set; } = "<color=#FF0000>DANGER! DECONTAMINATION IMMINENT! {time}</color>";

        [Description("stats")]
        public string DeathMsgKiller { get; set; } = "\n\n\n{killer} <color=#43BFF0>HP: {health}</color>";
        public string DeathMsgAhp { get; set; } = " <color=#008f1c>AH: {ahp}</color>";
        public string DeathMsgDamageReduction { get; set; } = " <color=#5900ff> DR: {reduction}%</color>";
        public string DeathMsgBodyshotReduction { get; set; } = " <color=#e7d77b> BSR: {reduction}%</color>";
        public string DeathMsgDamageDelt { get; set; } = "\n<color=#43BFF0>Delt - DMG: {damage}</color> <color=#FF0000>HS: {head_shots}</color> <color=#36a832>BS: {body_shots}</color> <color=#43BFF0>LS: {limb_shots}</color>";
        public string DeathMsgDamageTaken { get; set; } = "\n<color=#43BFF0>Taken - DMG: {damage}</color> <color=#FF0000>HS: {head_shots}</color> <color=#36a832>BS: {body_shots}</color> <color=#43BFF0>LS: {limb_shots}</color>";
        public string DeathMsgDamageOther { get; set; } = " Other: {other_hits}";
        public string PlayerStatsLine1 { get; set; } = "<color=#76b8b5>Kills:</color> <color=#FF0000>{kills}</color>    <color=#76b8b5>Deaths:</color> <color=#FF0000>{deaths}</color>    <color=#76b8b5>K/D:</color> <color=#FF0000>{kd}</color>    <color=#76b8b5>Highest Killstreak:</color> <color=#FF0000>{top_ks}</color>" + "</color>    <color=#76b8b5>Score:</color> <color=#FF0000>{score}</color>";
        public string PlayerStatsLine2 { get; set; } = "<color=#76b8b5>Hs Kills:</color> <color=#FF0000>{hsk}%</color>    <color=#76b8b5>Hs:</color> <color=#FF0000>{hs}%</color>    <color=#76b8b5>Accuracy:</color> <color=#FF0000>{accuracy}%</color>    <color=#76b8b5>Dmg Delt:</color> <color=#FF0000>{dmg_delt}</color>    <color=#76b8b5>Dmg Taken:</color> <color=#FF0000>{dmg_taken}</color>";
        public string HighestKillstreak { get; set; } = "<b><color=#43BFF0>{name}</color></b> <color=#d4af37>had the highest killstreak of</color> <b><color=#FF0000>{streak}</color></b>";
        public string HighestKills { get; set; } = "<b><color=#43BFF0>{name}</color></b> <color=#c0c0c0>had the most kills</color> <b><color=#FF0000>{kills}</color></b>";
        public string HighestScore { get; set; } = "<b><color=#43BFF0>{name}</color></b> <color=#a97142>was the best player with a score of </color> <b><color=#FF0000>{score}</color></b>";

        [Description("experience")]
        public string RewardXpKill { get; set; } = "gained {xp} Xp for killing player";
        public string RewardXpMinute { get; set; } = "gained {xp} Xp for playing for {time} minutes";
        public string RewardXp100Damage { get; set; } = "gained {xp} Xp for dealing 100 damage";
        public string RewardXp500Damage { get; set; } = "gained {xp} Xp for dealing 500 damage";
        public string RewardXp2500Damage { get; set; } = "gained {xp} Xp for dealing 2500 damage";
        public string RewardXp10000Damage { get; set; } = "gained {xp} Xp for dealing 10000 damage";
        public string RewardXp5Killstreak { get; set; } = "gained {xp} Xp for reaching a 5 killstreak";
        public string RewardXp10Killstreak { get; set; } = "gained {xp} Xp for reaching a 10 killstreak";
        public string RewardXp15Killstreak { get; set; } = "gained {xp} Xp for reaching a 15 killstreak";
        public string RewardXp20Killstreak { get; set; } = "gained {xp} Xp for reaching a 20 killstreak";
        public string RewardXp25Killstreak { get; set; } = "gained {xp} Xp for reaching a 25 killstreak";
        public string RewardXpRoundStart { get; set; } = "gained {xp} Xp from round start bonus";
        public string RewardXpItemUsed { get; set; } = "gained {xp} Xp for using {item}";
        public string RewardXpItemThrown { get; set; } = "gained {xp} Xp for throwing {item}";
        public string XpMsg { get; set; } = "<align=center><voffset=2em><b><size=48>{xp}</size></b>\n";
        public string XpGainedMsg { get; set; } = "<align=center><voffset=2em><b><size=48>YOU GAINED {xp} XP THIS ROUND!</size></b>\n";

        [Description("rank")]
        public string RankMsg { get; set; } = "<align=center><voffset=1.5em><b><size=72><color={color}>{rank}</color></size></b>\n";

        [Description("attachment blacklist")]
        public string AttachmentBanned { get; set; } = "<color=#FF0000>attachment {attachment} banned</color>";

        [Description("voice chat")]
        public string GlobalTalkGlobalReceive { get; set; } = "<color=#43BFF0>Voice Chat: Global Talk Global Receive</color>";
        public string ProximityTalkGlobalReceive { get; set; } = "<color=#43BFF0>Voice Chat:</color> <color=#FF0000>Proximity Talk</color> <color=#43BFF0>Global Receive</color>";
        public string ProximityTalkProximityReceive { get; set; } = "<color=#43BFF0>Voice Chat:</color> <color=#FF0000>Proximity Talk Proximity Receive</color>";

        [Description("leader board")]
        public string LeaderBoardTitle { get; set; } = "<color=#d4af37><b><size=128>Leader Board</size></b></color>";

        public string LeaderBoardControl { get; set; } = "\n<size=24><b><color=#FF0000>Controls: uses player movement for page/menu scrolling Left/Right = Type, Forward/Backward = Page. Note, wont work when up against a wall</color></b></size>";

        public string LedgendPos { get; set; } = "Pos";
        public string LedgendName { get; set; } = "Name";
        public string LedgendRank { get; set; } = "Rank";
        public string LedgendExperience { get; set; } = "Experience";
        public string LedgendKillstreak { get; set; } = "Killstreak";
        public string LedgendKills { get; set; } = "Kills";
        public string LedgendTime { get; set; } = "Time";

        public string PageAndLine { get; set; } = "Page {page} of {page_max} [{line_start} - {line_end}]/{line_max}";

        [Description("round")]
        public string RoundEnd5Minutes { get; set; } = "<color=#43BFF0>Round Ends in 5 minutes</color>";
        public string RoundEnd1Minute { get; set; } = "<color=#43BFF0>Round Ends in 1 minute</color>";
    }

    public static class Translation
    {
        public static TranslationConfig translation = null;
    }


    //[CommandHandler(typeof(RemoteAdminCommandHandler))]
    //public class H : ICommand
    //{
    //    public string Command { get; } = "h";

    //    public string[] Aliases { get; } = new string[] { };

    //    public string Description { get; } = "send hint";

    //    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    //    {
    //        Player player;
    //        if (Player.TryGet(sender, out player))
    //        {
    //            string h = "";
    //            foreach (var a in arguments)
    //                h += a;
    //            player.ReceiveHint(h, 300);
    //            response = "success";
    //            return true;
    //        }
    //        response = "failed";
    //        return false;
    //    }
    //}
}
