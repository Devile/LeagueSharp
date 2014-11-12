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

namespace SpeechUdyr
{
    class Program
    {
        static SpeechSynthesizer sSynth = new SpeechSynthesizer();
        static SpeechRecognitionEngine sRecognize = new SpeechRecognitionEngine();
        static Choices speechList = new Choices();
        static Grammar gr;

        static Menu menu;

        static Spell Q = new Spell(SpellSlot.Q);
        static Spell W = new Spell(SpellSlot.W);
        static Spell E = new Spell(SpellSlot.E);
        static Spell R = new Spell(SpellSlot.R);

        static Timer speechTimer;
        static bool wantSpeech;
        static int oldInterval;

        static void Main(string[] args)
        {
            if (ObjectManager.Player.ChampionName != "Udyr") { return; };

            Game.PrintChat("<font color='#FFFFCC'>UdyrSpeech</font> - by Maufeat");

            speechList.Add(new string[] { "tiger", "teiger", "turtle", "bear", "phoenix", "beer"});
            gr = new Grammar(new GrammarBuilder(speechList));
            
            menu = new Menu("Udyr Speech", "udyr", true);
            menu.AddSubMenu(new Menu("Enable", "enable"));
            menu.SubMenu("enable").AddItem(new MenuItem("speech", "enable").SetValue(true));
            menu.SubMenu("enable").AddItem(new MenuItem("speechinterval", "delay").SetValue(new Slider(1000, 250, 5000)));
            menu.AddToMainMenu();

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
        
        static void sRecognize_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            switch (e.Result.Text)
            {
                case "tiger":
                case "teiger":
                    Q.Cast();
                    break;
                case "turtle":
                    W.Cast();
                    break;
                case "bear":
                case "beer":
                    E.Cast();
                    break;
                case "phoenix":
                    R.Cast();
                    break;
                default:
                    Game.PrintChat(e.Result.Text);
                    break;
            }
        }
        static void Message(string msg)
        {
            Game.PrintChat("<font color='#00FF00'>" + ObjectManager.Player.Name + ":</font> " + msg);
        }
    }
}
