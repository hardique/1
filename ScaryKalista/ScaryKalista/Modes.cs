﻿using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Events;
using SharpDX;

namespace ScaryKalista
{
    class Modes
    {
        private static AIHeroClient _soulBound;

        private static Vector2 _baron = new Vector2(5007.124f, 10471.45f);
        private static Vector2 _dragon = new Vector2(9866.148f, 4414.014f);

        public static void Combo()
        {
            var target = TargetSelector.GetTarget(Spells.Q.Range, DamageType.Physical);

            if (target == null || !target.IsValidTarget()) return;

            if (Config.ComboMenu.IsChecked("combo.useQ")
                && Spells.Q.IsReady()
                && !Player.Instance.IsDashing())
            {
                if (!Config.ComboMenu.IsChecked("combo.saveMana") || (Config.ComboMenu.IsChecked("combo.saveMana") && (Player.Instance.Mana - 70) > 40))
                {
                    Spells.Q.Cast(target);
                }
            }

            if (Config.ComboMenu.IsChecked("combo.useE") 
                && !Config.MiscMenu.IsChecked("misc.killstealE")
                && Spells.E.IsReady())
            {
                if (EntityManager.Heroes.Enemies.Any(x => x.IsRendKillable()))
                {
                    Spells.E.Cast();
                }
            }
        }

        public static void Harass()
        {
            var target = TargetSelector.GetTarget(Spells.Q.Range, DamageType.Physical);

            if (target == null || !target.IsValidTarget()) return;

            if (Config.HarassMenu.IsChecked("harass.useQ") 
                && Spells.Q.IsReady() 
                && !Player.Instance.IsDashing()
                && !(Player.Instance.ManaPercent < Config.HarassMenu.GetValue("harass.minManaQ")))
            {
                Spells.Q.Cast(target);
            }
        }

        public static void LaneClear()
        {
            if (Player.HasBuff("summonerexhaust")
                || Player.Instance.ManaPercent < Config.LaneMenu.GetValue("laneclear.minMana"))
            {
                return;
            }

            if (Config.LaneMenu.IsChecked("laneclear.useE") && Spells.E.IsReady())
            {
                var minions = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, Player.Instance.Position, Spells.E.Range);
                var count = minions.Count(x => x.IsRendKillable() && x.Health > 10);

                if (count >= Config.LaneMenu.GetValue("laneclear.minE"))
                {
                    Spells.E.Cast();
                }
            }
        }

        public static void JungleClear()
        {
            if (Config.JungleMenu.IsChecked("jungleclear.useE")
                && !Config.MiscMenu.IsChecked("misc.junglestealE")
                && Spells.E.IsReady())
            {
                var minions = EntityManager.MinionsAndMonsters.GetJungleMonsters(Player.Instance.Position, Spells.E.Range);

                if (!Config.JungleMenu.IsChecked("jungleclear.miniE"))
                {
                    if (minions.Any(x => x.IsRendKillable() && !x.Name.Contains("Mini")))
                    {
                        Spells.E.Cast();
                    }
                }

                else
                {
                    if (minions.Any(x => x.IsRendKillable()))
                    {
                        Spells.E.Cast();
                    }
                }
            }
        }

        public static void Flee()
        {
            var spot = WallJump.GetJumpSpot();

            if (Spells.Q.IsReady() && spot != null)
            {
                Orbwalker.DisableAttacking = true;
                Orbwalker.DisableMovement = true;

                WallJump.JumpWall();
            }

            else
            {
                Orbwalker.DisableAttacking = false;
                Orbwalker.DisableMovement = false;

                var target =
                EntityManager.MinionsAndMonsters.CombinedAttackable
                .FirstOrDefault(x => x.IsValidTarget(Player.Instance.AttackRange));

                if (target != null)
                {
                    Orbwalker.ForcedTarget = target;
                }
            }
        }

        public static void PermaActive()
        {
            if (Config.MiscMenu.IsChecked("misc.killstealE") && Spells.E.IsReady())
            {
                if (EntityManager.Heroes.Enemies.Any(x => x.IsRendKillable()))
                {
                    Spells.E.Cast();
                }
            }

            if (Config.MiscMenu.IsChecked("misc.junglestealE") && Spells.E.IsReady())
            {
                var minions = EntityManager.MinionsAndMonsters.GetJungleMonsters(Player.Instance.Position, Spells.E.Range);

                if (!Config.JungleMenu.IsChecked("jungleclear.miniE"))
                {
                    if (minions.Any(x => x.IsRendKillable() && !x.Name.Contains("Mini")))
                    {
                        Spells.E.Cast();
                    }
                }

                else
                {
                    if (minions.Any(x => x.IsRendKillable()))
                    {
                        Spells.E.Cast();
                    }
                }
            }

            if (Config.MiscMenu.IsChecked("misc.harassEnemyE") && Spells.E.IsReady())
            {
                if (Player.HasBuff("summonerexhaust")) return;

                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass)
                    || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
                {
                    var minion =
                        EntityManager.MinionsAndMonsters.GetLaneMinions(
                            EntityManager.UnitTeam.Enemy, Player.Instance.Position, Spells.E.Range)
                            .Any(x => x.IsRendKillable() && x.Health > 10);

                    var hero =
                        EntityManager.Heroes.Enemies
                        .Any(x =>
                                x.HasRendBuff()
                                && !x.HasSpellShield()
                                && !x.HasUndyingBuff());

                    if (minion && hero)
                    {
                        Spells.E.Cast();
                    }
                }
            }

            if (Config.MiscMenu.IsChecked("misc.useR") && Spells.R.IsReady())
            {
                _soulBound = EntityManager.Heroes.Allies.FirstOrDefault(hero => hero.HasBuff("kalistacoopstrikeally"));

                if (_soulBound == null) return;

                if (_soulBound.HealthPercent <= Config.MiscMenu.GetValue("misc.healthR")
                    && _soulBound.CountEnemiesInRange(500) > 0)
                {
                    Spells.R.Cast();
                }
            }

            if (Config.MiscMenu.IsActive("misc.castDragonW") && Spells.W.IsReady())
            {
                if (Player.Instance.Distance(_dragon) <= Spells.W.Range)
                {
                    Spells.W.Cast(_dragon.To3DWorld());
                }
            }

            if (Config.MiscMenu.IsActive("misc.castBaronW") && Spells.W.IsReady())
            {
                if (Player.Instance.Distance(_baron) <= Spells.W.Range)
                {
                    Spells.W.Cast(_baron.To3DWorld());
                }
            }
        }

        public static void OnUnkillableMinion(Obj_AI_Base unit, Orbwalker.UnkillableMinionArgs args)
        {
            if (Player.HasBuff("summonerexhaust") 
                || (Player.Instance.Mana - 40) < 40 
                || !Spells.E.IsReady())
            {
                return;
            }

            if (Config.MiscMenu.IsChecked("misc.unkillableE") && unit.IsRendKillable())
            {
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass)
                    || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
                {
                    Spells.E.Cast();
                }
            }
        }
    }
}