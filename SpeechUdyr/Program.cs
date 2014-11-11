using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Speech.Synthesis;
using System.Speech.Recognition;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

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
        static Spell C = new Spell(SpellSlot.Recall);

        static void Main(string[] args)
        {
            if (ObjectManager.Player.ChampionName != "Udyr") { return; };
            Game.PrintChat("<font color='#FFFFCC'>UdyrSpeech</font> - by Maufeat");

            speechList.Add(new string[] { "tiger", "teiger", "turtle", "bear","phoenix", "recall", "beer"});
            gr = new Grammar(new GrammarBuilder(speechList));

            
            menu = new Menu("Udyr Speech", "udyr", true);
            menu.AddSubMenu(new Menu("Enable", "enable"));
            menu.SubMenu("enable").AddItem(new MenuItem("speech", "enable").SetValue(true));
            menu.AddToMainMenu();

            Game.OnGameUpdate += Game_OnGameUpdate;
        }

        static void Game_OnGameUpdate(EventArgs args)
        {
            bool wantSpeech = menu.Item("speech").GetValue<bool>();
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
                case "tiger":
                case "teiger":
                    Game.PrintChat("Stance: Tiger");
                    Q.Cast();
                    break;
                case "turtle":
                    Game.PrintChat("Stance: Turtle");
                    W.Cast();
                    break;
                case "bear":
                case "beer":
                    Game.PrintChat("Stance: Bear");
                    E.Cast();
                    break;
                case "phoenix":
                    Game.PrintChat("Stance: Phoenix");
                    R.Cast();
                    break;
                case "recall":
                    Game.PrintChat("Cast: Recall");
                    C.Cast();
                    break;
                }
            }
    }
}
