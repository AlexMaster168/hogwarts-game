using System.Collections.Generic;
using UnityEngine;

public class RoomServices : MonoBehaviour
{
    public static RoomServices I;
    public int herbs;
    public readonly HashSet<string> visited=new();
    public float harvestReady, mealReady;
    string room;
    public static bool Combat => QuestManager.I!=null && (QuestManager.I.step==QuestManager.Step.Duel||QuestManager.I.step==QuestManager.Step.Ambush||
        QuestManager.I.step==QuestManager.Step.ForestFight||QuestManager.I.step==QuestManager.Step.Boss||QuestManager.I.step==QuestManager.Step.Convoy||
        QuestManager.I.step==QuestManager.Step.Rescue||QuestManager.I.step==QuestManager.Step.Rift||Trials.I.active>=0);
    public static bool Threatened
    {
        get
        {
            if(Combat)return true;
            foreach(var e in Enemy.All)if(e!=null&&!e.IsDead&&!e.Charmed&&e.aggro&&Vector3.Distance(e.transform.position,GameManager.I.player.transform.position)<40)return true;
            return false;
        }
    }
    void Awake(){I=this;visited.Add("Двор");}
    void Update()
    {
        if(!GameState.Playing)return;
        string here=CastleLocations.Current(GameManager.I.player.transform.position);
        if(here!="Окрестности Хогвартса")visited.Add(here);
        if(Input.GetKeyDown(KeyCode.F5))SaveGame.Save();
        if(Input.GetKeyDown(KeyCode.F9))SaveGame.Load();
    }
    public static string Hint(PlayerController p)
    {
        if(Threatened)return null;
        string here=CastleLocations.Current(p.transform.position);
        return here=="Кухня"||here=="Лазарет"||here=="Зельеварение"||here=="Оранжерея"||here=="Библиотека"||here=="Кабинет чар" ? "[E] "+here+" — взаимодействия" : null;
    }
    public static bool TryUse(PlayerController p)
    {
        if(Hint(p)==null)return false;
        I.room=CastleLocations.Current(p.transform.position);GameState.Set(GameMode.Service);return true;
    }
    public bool Travel(int index)
    {
        if(index<0||index>=CastleLocations.All.Count||Threatened)return false;
        var l=CastleLocations.All[index];var p=GameManager.I.player;
        if(!visited.Contains(l.name)||p.inventory.galleons<25)return false;
        p.inventory.galleons-=25;if(p.onBroom)p.ToggleBroom();
        p.Teleport(l.spawn);CastleMap.I.Close();GameManager.I.camRig.SnapBehind();
        HUD.ShowToast("Летучий порох: "+l.name);return true;
    }
    public bool Craft(string id)
    {
        var inv=GameManager.I.player.inventory;
        if(herbs<2||inv.galleons<30||!inv.potions.ContainsKey(id)||inv.potions[id]>=Inventory.MaxStack)return false;
        herbs-=2;inv.galleons-=30;inv.potions[id]++;HUD.ShowToast("Сварено: "+ItemDB.Get(id).name);return true;
    }
    public bool Harvest()
    {
        if(Time.time<harvestReady||herbs>=99)return false;
        herbs=Mathf.Min(99,herbs+6);harvestReady=Time.time+180;HUD.ShowToast("Собрано 6 порций трав");return true;
    }
    public bool Meal()
    {
        if(Time.time<mealReady)return false;
        GameManager.I.player.health.Heal(40);mealReady=Time.time+60;HUD.ShowToast("Обед: здоровье +40");return true;
    }
    public bool Study()
    {
        var p=GameManager.I.player;var spell=p.caster.catalog.Find(s=>!s.learned&&!s.unforgivable&&!s.special);
        if(spell==null||p.inventory.galleons<200)return false;
        p.inventory.galleons-=200;p.caster.Learn(spell.type);return true;
    }
    void OnGUI()
    {
        if(GameState.Mode!=GameMode.Service)return;
        GUI.depth=-20;
        var w=new Rect((Screen.width-540)/2f,(Screen.height-390)/2f,540,390);
        GUI.Box(w,room+" — услуги");GUI.BeginGroup(w);
        var p=GameManager.I.player;
        GUI.Label(new Rect(25,35,490,45),$"Галлеоны: {p.inventory.galleons} · Травы: {herbs}\nF5 — сохранить · F9 — загрузить (после закрытия)");
        if(room=="Кухня"&&GUI.Button(new Rect(25,95,490,45),"Поесть: +40 здоровья (раз в минуту)"))Meal();
        if(room=="Лазарет"&&GUI.Button(new Rect(25,95,490,45),"Полное лечение — 50 галлеонов")&&p.inventory.galleons>=50)
        {p.inventory.galleons-=50;p.health.Heal(p.health.max);}
        if(room=="Оранжерея"&&GUI.Button(new Rect(25,95,490,45),"Собрать 6 трав (раз в 3 минуты)"))Harvest();
        if(room=="Библиотека"&&GUI.Button(new Rect(25,95,490,45),"Изучить следующее обычное заклинание — 200 г."))Study();
        if(room=="Зельеварение")
        {
            string[] ids={"wiggenweld","focus","thunder"};
            for(int i=0;i<ids.Length;i++)if(GUI.Button(new Rect(25,95+i*55,490,45),ItemDB.Get(ids[i]).name+" — 2 травы + 30 г."))Craft(ids[i]);
        }
        if(room=="Кабинет чар"&&GUI.Button(new Rect(25,95,490,45),"Создать тренировочный манекен"))
        {TrainingDummy.Spawn(new Vector3(-14,.2f,140));GameState.Set(GameMode.Play);}
        if(GUI.Button(new Rect(25,320,490,45),"Вернуться в игру"))GameState.Set(GameMode.Play);
        GUI.EndGroup();
        if(Event.current.type==EventType.KeyDown&&Event.current.keyCode==KeyCode.Escape){GameState.Set(GameMode.Play);Event.current.Use();}
    }
}
