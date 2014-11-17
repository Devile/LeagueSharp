using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace AutoFollowScript
{
    class Program
    {
        public static Menu menu;
        private static Obj_AI_Hero myHero = ObjectManager.Player;
        private static Obj_AI_Hero following = null;
        private static Obj_AI_Hero temp_following = null;
        private static Obj_AI_Hero[] AllAllies;

        static Obj_AI_Base minion;

        private static Obj_SpawnPoint allySpawn = null;
        private static Obj_SpawnPoint enemySpawn = null;

        private static bool isRecalling = true;
        private static int myState = 0;
        private static int _followDistance = 500; // minimum 400

        static void Main(string[] args)
        {
            Game.PrintChat("<font color='#66FFFF'>AutoFollow</font> - by Maufeat");

            menu = new Menu("Auto Follow", "AutoFollow", true);
            //Make the submenu
            menu.AddSubMenu(new Menu("Debug Message", "Debug"));
            menu.AddSubMenu(new Menu("Follow Config", "Follow"));
            //Add the configs
            menu.SubMenu("Debug").AddItem(new MenuItem("Debug", "Show").SetValue(true));
            //Make the menu available and visible
            menu.AddToMainMenu();

            GetTowers(myHero.Team);
            GetAllies(myHero.Team);
            Vector3 _enemySpawnPos = ObjectManager.Get<GameObject>().First(x => x.Type == GameObjectType.obj_SpawnPoint && x.Team != ObjectManager.Player.Team).Position;
            Game.PrintChat(_enemySpawnPos.To2D().ToString());
            following = AllAllies[1];
            try
            {
                debug(following.BaseSkinName);
            }
            catch (Exception e)
            {
                Game.PrintChat(e.Message);
            }
            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
        }
        private static void Game_OnGameUpdate(EventArgs args)
        {
            if(menu.Item("Debug").GetValue<bool>())
            {
                Game.PrintChat(myState+"");
            }
            if (myHero.IsDead && following == null)
            {
                following = AllAllies[1];
                myState = 0;
            }
            if (following != null && myHero.IsDead == false)
            {

                if (myState == 0) // FOLLOW
                {
                    RunAndFollow(following);
                } else if (myState == 1) // TEMP_FOLLOW
                {                
                    if (!following.IsDead)
                    {
                        myState = 0;
                    }
                    RunAndFollow(temp_following);
                } else if (myState == 2) // GOING_TO_TOWER
                {
                    RunAndFollow(GetCloseTower(myHero, myHero.Team));
                } else if (myState == 3) // WAITING_FOLOW_RESP
                {
                    if (!following.IsDead)
                    {
                        RunAndFollow(following);
                    }
                } else if (myState == 8) // IN_TOWER_RADIUS
                {
                    myState = 0;
                }
            }
        }
        private static void Drawing_OnDraw(EventArgs args)
        {
            Utility.DrawCircle(ObjectManager.Player.Position, myHero.AttackRange, Color.Red);
        }
        private static void RunAndFollow(Obj_AI_Base target)
        {
            var HealthPercentage = myHero.MaxHealth / myHero.Health;
            if (target.Type == GameObjectType.obj_AI_Hero)
            {
                if (HealthPercentage > 3)
                {
                    myState = 2;
                }
                if (target.IsDead)
                {
                    Random rnd = new Random();
                    int rndIndex = rnd.Next(1,4);
                    temp_following = AllAllies[rndIndex];
                    myState = 1;
                }
                if (inHeroRadius(myHero.ServerPosition))
                {
                    if(!myHero.IsAutoAttacking)
                    {
                        foreach (Obj_AI_Base enemy in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsEnemy))
                        if (!IsInsideEnemyTower(myHero.Position))
                        {
                                ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, enemy);
                                return;
                        }
                        foreach (Obj_AI_Base turret in ObjectManager.Get<Obj_AI_Turret>().Where(turret => turret.IsValidTarget(GetAutoAttackRange(myHero, turret))))
                            if (turret.IsEnemy)
                            {
                                ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, turret);
                                return;
                            }
                        minion = MinionManager.GetMinions(myHero.ServerPosition, myHero.AttackRange, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health).FirstOrDefault();
                        if (minion == null)
                            minion = MinionManager.GetMinions(myHero.ServerPosition, myHero.AttackRange).FirstOrDefault();
                        if (minion.Health < myHero.GetAutoAttackDamage(minion) || minion.Health == minion.MaxHealth)
                        {
                            ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, minion);
                        }
                    }
                    return;
                }
                if (myHero.Distance(target) > _followDistance || myHero.Distance(target) < 275 || myHero.Distance(allySpawn.Position.To2D()) + 275 > target.Distance(allySpawn.Position.To2D()))
                {
                    float followX;
                    float followY;
                    if(allySpawn == null)
                    {
                        followX = target.Position.X;
                        followY = target.Position.Y;    
                    }else
                    {                  
                        Game.PrintChat(allySpawn.ToString());
                        //Game.PrintChat("KOMISCH");
                        followX = ((allySpawn.Position.X - target.ServerPosition.X) / (target.Distance(allySpawn.Position.To2D()))) * ((_followDistance - 300 / 2 + 300) + target.ServerPosition.X + random(-(_followDistance - 300 / 3), (_followDistance - 300 / 3)));
                        followY = ((allySpawn.Position.Y - target.ServerPosition.Y) / (target.Distance(allySpawn.Position.To2D()))) * ((_followDistance - 300 / 2 + 300) + target.ServerPosition.Y + random(-(_followDistance - 300 / 3), (_followDistance - 300 / 3)));
                    
                    }
                    //Game.PrintChat("Follow>>" + target.BaseSkinName + " X: " + followX + " Y: " + followY);
                    moveTo(new Vector2(followX, followY));
                    //followX = ((allySpawn.x - partner.x)/(partner:GetDistance(allySpawn)) * ((config.followChamp.followDist - 300) / 2 + 300) + partner.x + math.random(-((config.followChamp.followDist-300)/3),((config.followChamp.followDist-300)/3)))
                    }
            }
            if (target.Type == GameObjectType.obj_AI_Turret)
            {
                if (myHero.Distance(target.Position) > 300)
                {
                    int rndX = random(-150, 150);
                    int rndZ = random(-150, 150);
                    Vector2 position = new Vector2(target.Position.X + rndX, target.Position.Z + rndZ);
                    moveTo(target.Position.To2D());
                }
                else if (HealthPercentage > 3)
                {
                    if (isRecall(myHero))
                    {
                        return;
                    }
                    Spell recall = new Spell(SpellSlot.Recall);
                    recall.Cast();
                    return;
                } else if (inTowerRange(myHero.ServerPosition))
                {
                    myState = 8;
                }
            }
        }
        private static Obj_AI_Turret GetCloseTower(Obj_AI_Hero target, GameObjectTeam team)
        {
            List<Obj_AI_Turret> towerList = GetTowers(myHero.Team);
            Obj_AI_Turret[] towers = towerList.ToArray<Obj_AI_Turret>();
            if (towers != null)
            {
                Obj_AI_Turret candidate = towerList[1];
                int count = towers.Length;
                for (var i = 2; i < count; i++)
                {
                    if (towers[i].Health / towers[i].MaxHealth > 0.1 && myHero.Distance(candidate) > myHero.Distance(towers[i]))
                    {
                        candidate = towers[i];
                    }
                }
                return candidate;
            }
            return null;
        }
        private static List<Obj_AI_Turret> GetTowers(GameObjectTeam team)
        {
            List<Obj_AI_Turret> towersList = new List<Obj_AI_Turret>();
            foreach( Obj_AI_Turret turret in ObjectManager.Get<Obj_AI_Turret>().Where(tur => tur.IsAlly && tur.Health > 0))
            {
                towersList.Add(turret);
            }
            return towersList;
        }
        private static void GetAllies(GameObjectTeam team)
        {
            List<Obj_AI_Hero> temp = new List<Obj_AI_Hero>();
            foreach (Obj_AI_Hero hero in ObjectManager.Get<Obj_AI_Hero>())
            {
                temp.Add(hero);
            }
            AllAllies = temp.ToArray<Obj_AI_Hero>();
        }
        public static bool inHeroRadius(Vector3 pos)
        {
            return ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.BaseSkinName == following.BaseSkinName).Any(hero => pos.Distance(hero.Position) < _followDistance);
        }
        public static bool inTowerRange(Vector3 pos)
        {
            return ObjectManager.Get<Obj_AI_Turret>().Where(tur => tur.IsAlly && tur.Health > 0).Any(tur => pos.Distance(tur.Position) < (500 + myHero.BoundingRadius));
        }
        public static bool IsInsideEnemyTower(Vector3 pos)
        {
            return ObjectManager.Get<Obj_AI_Turret>()
                                    .Any(tower => tower.IsEnemy && tower.Health > 0 && tower.Position.Distance(pos) < 775);
        }
        public static float GetAutoAttackRange(Obj_AI_Base source = null, Obj_AI_Base target = null)
        {
            if (source == null)
                source = myHero;
            var ret = source.AttackRange + myHero.BoundingRadius;
            if (target != null)
                ret += target.BoundingRadius;
            return ret;
        }
        private static bool isRecall(Obj_AI_Base player)
        {
            return isRecalling;
        }
        public static void moveTo(Vector2 Pos)
        {
            myHero.IssueOrder(GameObjectOrder.MoveTo, Pos.To3D());
        }
        public static int random(int minValue, int maxValue)
        {
            Random random = new Random();
            return random.Next(minValue, maxValue);
        }
        public static void debug(string message)
        {
            if (menu.Item("Debug").GetValue<bool>())
            {
                Game.PrintChat("<font color='#ff0000'>[DEBUG]</font> " + message);
            }
        }
    }
}
