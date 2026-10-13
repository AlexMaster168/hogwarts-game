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
    // Точки перемещения в окрестностях: открываются, когда подходишь ближе 35 м; перемещение — с карты [V].
    public static readonly (string name,Vector3 pos,string detail)[] Waypoints=
    {
        ("Хогсмид",WorldBuilder.HogsmeadeCenter+new Vector3(-4,0,0),"Деревня: зелья, мётлы, мантии, книги заклинаний."),
        ("Тёмный торговец",WorldBuilder.ShadyPos+new Vector3(0,0,7),"Переулок за Хогсмидом. Продаёт непростительные заклинания."),
        ("Логово тёмных магов",WorldBuilder.DarkCampPos+new Vector3(0,0,-10),"К северу от замка. Непростительные заклинания и тёмная экипировка."),
        ("Мост",new Vector3(0,0,-100),"Южный мост к воротам замка."),
        ("Квиддичное поле",Activities.SnitchStand+new Vector3(4,0,4),"Поле для полётов и стойка ловли снитча."),
        ("Берег озера",WorldBuilder.LakeCenter+new Vector3(84,0,-12),"Чёрное озеро. Ночью здесь бродят дементоры."),
        ("Опушка Запретного леса",WorldBuilder.ForestCenter+new Vector3(70,0,-50),"Вход в Запретный лес."),
        ("Склеп",SideQuests.CryptPos+new Vector3(0,0,-16),"Старый склеп к северу от замка."),
        ("Руины",WorldBuilder.RuinsCenter+new Vector3(0,0,-30),"Древние руины на севере."),
    };
    public static bool Discovered(int i)=>I!=null&&I.visited.Contains(Waypoints[i].name);
    void Awake(){I=this;visited.Add("Двор");}
    void Update()
    {
        if(!GameState.Playing)return;
        string here=CastleLocations.Current(GameManager.I.player.transform.position);
        if(here!="Окрестности Хогвартса")visited.Add(here);
        var pp=GameManager.I.player.transform.position;
        for(int i=0;i<Waypoints.Length;i++)
            if(!visited.Contains(Waypoints[i].name)&&(pp-Waypoints[i].pos).sqrMagnitude<35*35)
            {visited.Add(Waypoints[i].name);SFX.Play("pickup",0.5f);HUD.ShowToast("Открыта точка перемещения: "+Waypoints[i].name+" [V]",new Color(0.5f,0.9f,1f));}
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
    public bool TravelTo(int wp)
    {
        if(wp<0||wp>=Waypoints.Length||Threatened||!Discovered(wp))return false;
        var p=GameManager.I.player;
        if(p.inventory.galleons<25)return false;
        p.inventory.galleons-=25;if(p.onBroom)p.ToggleBroom();
        p.cc.enabled=false;
        var spot=Ground(Waypoints[wp].pos);
        p.cc.enabled=true;
        p.Teleport(spot);if(CastleMap.I!=null)CastleMap.I.Close();GameManager.I.camRig.SnapBehind();
        HUD.ShowToast("Перемещение: "+Waypoints[wp].name);return true;
    }

    // Свободная площадка на земле рядом с точкой: луч сверху + проверка, что капсула игрока ни во что не упирается.
    public static Vector3 Ground(Vector3 p)
    {
        for(float r=0;r<=14;r+=2)
            for(int a=0;a<(r==0?1:10);a++)
            {
                var c=p+Quaternion.Euler(0,a*36,0)*Vector3.forward*r;
                if(!Physics.Raycast(c+Vector3.up*60,Vector3.down,out var hit,140,~0,QueryTriggerInteraction.Ignore))continue;
                if(hit.normal.y<0.8f||hit.point.y>p.y+3)continue;
                var g=hit.point+Vector3.up*0.12f;
                if(!Physics.CheckCapsule(g+Vector3.up*0.5f,g+Vector3.up*1.5f,0.45f,~0,QueryTriggerInteraction.Ignore))return g;
            }
        return p+Vector3.up*0.2f;
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
    public const int StudyPrice=200;
    public bool Study()
    {
        var spell=GameManager.I.player.caster.catalog.Find(s=>!s.learned&&!s.unforgivable&&!s.special);
        return spell!=null&&Study(spell.type);
    }
    // Библиотека учит любое обычное заклинание (непростительные — только у тёмных магов, Патронус — в Омуте памяти).
    public bool Study(SpellType t)
    {
        var p=GameManager.I.player;var spell=p.caster.Slot(t);
        if(spell==null||spell.learned||spell.unforgivable||spell.special)return false;
        if(p.inventory.galleons<StudyPrice){HUD.ShowToast($"Не хватает галлеонов: нужно {StudyPrice}",new Color(1f,.5f,.4f));return false;}
        p.inventory.galleons-=StudyPrice;p.caster.Learn(t);SFX.Play("book",.6f);
        HUD.ShowToast("Изучено заклинание: "+spell.name,new Color(.6f,.9f,1f));return true;
    }
    Vector2 libScroll;
    void DrawLibrary(PlayerController p)
    {
        var list=p.caster.catalog.FindAll(s=>!s.unforgivable&&!s.special);
        var w=new Rect((Screen.width-620)/2f,(Screen.height-640)/2f,620,640);
        GUI.color=new Color(.04f,.05f,.09f,.97f);GUI.DrawTexture(w,Texture2D.whiteTexture);GUI.color=Color.white;
        GUI.Box(w,"Библиотека — изучение заклинаний");GUI.BeginGroup(w);
        GUI.Label(new Rect(25,30,570,45),$"Галлеоны: {p.inventory.galleons} · Цена одного заклинания: {StudyPrice} г.\nНепростительные — у тёмных магов (карта [V]), Патронус — в Омуте памяти.");
        libScroll=GUI.BeginScrollView(new Rect(20,80,580,470),libScroll,new Rect(0,0,555,list.Count*42));
        for(int i=0;i<list.Count;i++)
        {
            var s=list[i];var r=new Rect(0,i*42,555,38);
            GUI.Label(new Rect(r.x+8,r.y+9,300,26),s.name);
            if(s.learned)GUI.Label(new Rect(r.x+330,r.y+9,220,26),"изучено");
            else
            {
                GUI.enabled=p.inventory.galleons>=StudyPrice;
                if(GUI.Button(new Rect(r.x+330,r.y+2,220,34),p.inventory.galleons>=StudyPrice?$"Изучить — {StudyPrice} г.":$"Нужно {StudyPrice} г."))Study(s.type);
                GUI.enabled=true;
            }
        }
        GUI.EndScrollView();
        if(GUI.Button(new Rect(25,575,570,45),"Вернуться в игру"))GameState.Set(GameMode.Play);
        GUI.EndGroup();
        if(Event.current.type==EventType.KeyDown&&Event.current.keyCode==KeyCode.Escape){GameState.Set(GameMode.Play);Event.current.Use();}
    }
    void OnGUI()
    {
        if(GameState.Mode!=GameMode.Service)return;
        GUI.depth=-20;
        if(room=="Библиотека"){DrawLibrary(GameManager.I.player);return;}
        var w=new Rect((Screen.width-540)/2f,(Screen.height-390)/2f,540,390);
        GUI.Box(w,room+" — услуги");GUI.BeginGroup(w);
        var p=GameManager.I.player;
        GUI.Label(new Rect(25,35,490,45),$"Галлеоны: {p.inventory.galleons} · Травы: {herbs}\nF5 — сохранить · F9 — загрузить (после закрытия)");
        if(room=="Кухня"&&GUI.Button(new Rect(25,95,490,45),"Поесть: +40 здоровья (раз в минуту)"))Meal();
        if(room=="Лазарет"&&GUI.Button(new Rect(25,95,490,45),"Полное лечение — 50 галлеонов")&&p.inventory.galleons>=50)
        {p.inventory.galleons-=50;p.health.Heal(p.health.max);}
        if(room=="Оранжерея"&&GUI.Button(new Rect(25,95,490,45),"Собрать 6 трав (раз в 3 минуты)"))Harvest();
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
