using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Speech.Synthesis;
using System.Speech.Recognition;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using System.Globalization;

namespace SpeechSummoners
{
    class Program
    {
        static SpeechSynthesizer sSynth = new SpeechSynthesizer();
        static SpeechRecognitionEngine sRecognize = new SpeechRecognitionEngine();
        static Choices speechList = new Choices();
        static Grammar gr;

        static Menu menu;

        static Spell C = new Spell(SpellSlot.Recall);
        static SpellSlot F;
        static SpellSlot I;
        static SpellSlot S;
        static SpellSlot H;
        static SpellSlot G;
        static SpellSlot T;
        static SpellSlot E;

        static Timer speechTimer;
        static bool wantSpeech;
        static int oldInterval;

        static Obj_AI_Base _selectedSmiteTarget;
        static Obj_AI_Base _selectedIgniteTarget;
        static Obj_AI_Base _selectedTeleportTarget;
        static Obj_AI_Base _selectedExhaustTarget;

        static void Main(string[] args)
        {
            Game.PrintChat("<font color='#CCFFFF'>SpeechSummoners</font> - by Maufeat");
            
            speechList.Add(new string[] { "recall", "flash", "smite", "ghost", "ignite", "teleport", "heal"});
            gr = new Grammar(new GrammarBuilder(speechList));

            menu = new Menu("Summoner Speech", "udyr", true);
            menu.AddSubMenu(new Menu("Enable", "enable"));
            menu.SubMenu("enable").AddItem(new MenuItem("speech", "enable").SetValue(true));
            menu.SubMenu("enable").AddItem(new MenuItem("speechinterval", "delay").SetValue(new Slider(1000, 250, 5000)));
            menu.AddToMainMenu();

            F = ObjectManager.Player.GetSpellSlot("SummonerFlash");
            I = ObjectManager.Player.GetSpellSlot("SummonerIgnite");
            H = ObjectManager.Player.GetSpellSlot("SummonerHeal");
            G = ObjectManager.Player.GetSpellSlot("SummonerHaste");
            S = ObjectManager.Player.GetSpellSlot("SummonerSmite");
            T = ObjectManager.Player.GetSpellSlot("SummonerTeleport");
            E = ObjectManager.Player.GetSpellSlot("SummonerExhuast");

            speechTimer = new Timer(TimerCallBack, null, 0, menu.Item("speechinterval").GetValue<Slider>().Value);
            Game.OnGameUpdate += Game_OnGameUpdate;
        }

        private static void TimerCallBack(object state)
        {
            if (wantSpeech)
            {
                try
                {
                    sRecognize.RequestRecognizerUpdate();
                    sRecognize.LoadGrammar(gr);
                    sRecognize.SpeechRecognized += sRecognize_SpeechRecognized;
                    sRecognize.SetInputToDefaultAudioDevice();
                    sRecognize.RecognizeAsync(RecognizeMode.Multiple);
                    sRecognize.Recognize();
                }
                catch
                {
                    return;
                }
            }
        }
        static void sRecognize_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            switch (e.Result.Text)
            {
                case "recall":
                    C.Cast();
                    break;
                case "flash":
                    if (!IsReadyFlash())
                    {
                        Message("Flash not ready!");
                        return;
                    }
                    Vector2 maxRange = ObjectManager.Player.ServerPosition.To2D().Extend(Game.CursorPos.To2D(), 400);
                    Vector3 position = maxRange.To3D();
                    ObjectManager.Player.SummonerSpellbook.CastSpell(F, position);
                    break;
                case "smite":
                    if (!IsReadySmite())
                    {
                        Message("Smite not ready!");
                        return;
                    }
                    _selectedSmiteTarget = null;
                    foreach (var enemy in
                        ObjectManager.Get<Obj_AI_Minion>()
                            .Where(minion => minion.Team == GameObjectTeam.Neutral)
                            .OrderByDescending(m => m.Distance(Game.CursorPos))
                            .Where(target => target.Distance(Game.CursorPos) < 200))
                    {
                        _selectedSmiteTarget = enemy;
                    }
                    if (_selectedSmiteTarget != null)
                    {
                        ObjectManager.Player.SummonerSpellbook.CastSpell(S, _selectedSmiteTarget);
                        _selectedSmiteTarget = null;
                    }
                    else if (_selectedSmiteTarget == null)
                    {
                        Message("No Smite Target!");
                    }
                    break;
                case "ignite":
                    if (!IsReadyIgnite())
                    {
                        Message("Ignite not ready!");
                        return;
                    }
                    _selectedIgniteTarget = null;
                    foreach (var enemy in
                        ObjectManager.Get<Obj_AI_Hero>()
                            .Where(minion => minion.IsEnemy)
                            .OrderByDescending(m => m.Distance(Game.CursorPos))
                            .Where(target => target.Distance(Game.CursorPos) < 200))
                    {
                        _selectedIgniteTarget = enemy;
                    }
                    if (_selectedIgniteTarget != null)
                    {
                        ObjectManager.Player.SummonerSpellbook.CastSpell(I, _selectedIgniteTarget);
                        _selectedIgniteTarget = null;
                    }
                    else if (_selectedIgniteTarget == null)
                    {
                        Message("No Smite Target!");
                    }
                    break;
                case "ghost":
                    if (!IsReadyGhost())
                    {
                        Message("Ghost not ready!");
                        return;
                    }
                    ObjectManager.Player.SummonerSpellbook.CastSpell(G);
                    break;
                case "teleport":
                    if (!IsReadyTeleport())
                    {
                        Message("Teleport not ready!");
                        return;
                    }
                    _selectedTeleportTarget = null;
                    foreach (var turret in
                        ObjectManager.Get<Obj_AI_Turret>()
                            .Where(turret => turret.Health > 0.1)
                            .OrderByDescending(t => t.Distance(Game.CursorPos))
                            .Where(target => target.Distance(Game.CursorPos) < 200))
                    {
                        _selectedTeleportTarget = turret;
                    }
                    foreach (var minion in
                        ObjectManager.Get<Obj_AI_Minion>()
                            .Where(minion => minion.IsAlly)
                            .OrderByDescending(m => m.Distance(Game.CursorPos))
                            .Where(target => target.Distance(Game.CursorPos) < 200))
                    {
                        if (_selectedTeleportTarget == null)
                        {
                            _selectedTeleportTarget = minion;
                        }
                    }
                    if (_selectedTeleportTarget != null)
                    {
                        ObjectManager.Player.SummonerSpellbook.CastSpell(T, _selectedTeleportTarget);
                        _selectedTeleportTarget = null;
                    }
                    else if (_selectedTeleportTarget == null)
                    {
                        Message("No Teleport Target!");
                    }
                    break;
                case "heal":
                    if (!IsReadyHeal())
                    {
                        Message("Heal not ready!");
                        return;
                    }
                        ObjectManager.Player.SummonerSpellbook.CastSpell(H);
                    break;
                case "exhaust":
                    if (!IsReadyExhaust())
                    {
                        Message("Teleport not ready!");
                        return;
                    }
                    _selectedExhaustTarget = null;
                    foreach (var enemy in
                        ObjectManager.Get<Obj_AI_Hero>()
                            .Where(ene => ene.IsEnemy)
                            .OrderByDescending(m => m.Distance(Game.CursorPos))
                            .Where(target => target.Distance(Game.CursorPos) < 200))
                    {
                        _selectedExhaustTarget = enemy;
                    }
                    if (_selectedExhaustTarget != null)
                    {
                        ObjectManager.Player.SummonerSpellbook.CastSpell(E, _selectedExhaustTarget);
                        _selectedExhaustTarget = null;
                    }
                    else if (_selectedExhaustTarget == null)
                    {
                        Message("No Exhaust Target!");
                    }
                    break;
                default:
                    Game.PrintChat(e.Result.Text);
                    break;
            }
        }
        static void Game_OnGameUpdate(EventArgs args)
        {
            wantSpeech = menu.Item("speech").GetValue<bool>();
            if (wantSpeech)
            {
                int speechInterval = menu.Item("speechinterval").GetValue<Slider>().Value;
                if (oldInterval == speechInterval)
                {
                    return;
                }
                if (oldInterval != speechInterval)
                {
                    speechTimer.Change(0, speechInterval);
                    Message("New Delay: " + speechInterval / 1000 + " Seconds.");
                    oldInterval = speechInterval;
                }
            }
        }
        static void Message(string msg)
        {
            Game.PrintChat("<font color='#00FF00'>" + ObjectManager.Player.Name + ":</font> " + msg);
        }
        static bool IsReadyFlash()
        {
            return (F != SpellSlot.Unknown && ObjectManager.Player.SummonerSpellbook.CanUseSpell(F) == SpellState.Ready);
        }
        static bool IsReadySmite()
        {
            return (S != SpellSlot.Unknown && ObjectManager.Player.SummonerSpellbook.CanUseSpell(S) == SpellState.Ready);
        }
        static bool IsReadyGhost()
        {
            return (G != SpellSlot.Unknown && ObjectManager.Player.SummonerSpellbook.CanUseSpell(G) == SpellState.Ready);
        }
        static bool IsReadyIgnite()
        {
            return (I != SpellSlot.Unknown && ObjectManager.Player.SummonerSpellbook.CanUseSpell(I) == SpellState.Ready);
        }
        static bool IsReadyTeleport()
        {
            return (T != SpellSlot.Unknown && ObjectManager.Player.SummonerSpellbook.CanUseSpell(T) == SpellState.Ready);
        }
        static bool IsReadyHeal()
        {
            return (H != SpellSlot.Unknown && ObjectManager.Player.SummonerSpellbook.CanUseSpell(H) == SpellState.Ready);
        }
        static bool IsReadyExhaust()
        {
            return (E != SpellSlot.Unknown && ObjectManager.Player.SummonerSpellbook.CanUseSpell(E) == SpellState.Ready);
        }
    }
}
