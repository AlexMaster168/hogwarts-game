using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

[Serializable] public class SaveEntry { public string id; public int count; }
[Serializable] public class GameSave
{
    public int version=1,house,pages,gold,earned,defeated,herbs,seals;
    public string step;
    public Vector3 position;
    public float health,playTime,harvestRemaining,mealRemaining;
    public bool broom,ancient,protego;
    public bool[] trials;
    public string[] spells,owned,visited;
    public Vector3[] openedChests;
    public int[] collectedPages;
    public SaveEntry[] potions,equipped,side;
}

public class SaveGame : MonoBehaviour
{
    public static SaveGame I;
    // Tests explicitly use a workspace path and never touch the user's slot.
    public static string TestPath;
    public static string FilePath => TestPath??Path.Combine(Application.persistentDataPath,"progress-v1.json");
    public static bool Exists => File.Exists(FilePath);
    float nextAuto=60;
    bool loading;
    void Awake(){I=this;}
    void Update()
    {
        if(GameState.Mode==GameMode.Title&&Input.GetKeyDown(KeyCode.F9))Load();
        if(SmokeTest.Requested||!GameState.Playing||Time.time<nextAuto)return;
        nextAuto=Time.time+60;if(!RoomServices.Threatened)Save();
    }
    void OnApplicationQuit(){if(!SmokeTest.Requested&&GameState.Playing&&!RoomServices.Threatened)Save(false);}
    public static GameSave Capture()
    {
        var p=GameManager.I.player;var inv=p.inventory;var q=QuestManager.I;
        return new GameSave
        {
            step=q.step.ToString(),house=q.house,pages=q.pages,gold=inv.galleons,earned=inv.earnedTotal,defeated=q.enemiesDefeated,
            herbs=RoomServices.I.herbs,seals=q.seals,position=p.transform.position,health=p.health.current,playTime=q.playTime,
            harvestRemaining=Mathf.Max(0,RoomServices.I.harvestReady-Time.time),mealRemaining=Mathf.Max(0,RoomServices.I.mealReady-Time.time),
            broom=p.hasBroom,ancient=p.ancientUnlocked,protego=p.caster.protegoLearned,trials=Trials.I.done.ToArray(),
            spells=p.caster.learned.Select(s=>s.type.ToString()).ToArray(),owned=inv.owned.ToArray(),visited=RoomServices.I.visited.ToArray(),
            potions=inv.potions.Select(x=>new SaveEntry{id=x.Key,count=x.Value}).ToArray(),
            equipped=inv.equipped.Select(x=>new SaveEntry{id=x.Value,count=(int)x.Key}).ToArray(),
            side=SideQuests.I.all.Where(x=>x.state==SQState.Done).Select(x=>new SaveEntry{id=x.id,count=x.have}).ToArray(),
            openedChests=Chest.All.Where(c=>c.opened).Select(c=>c.transform.position).ToArray(),collectedPages=FieldGuidePage.Collected.ToArray()
        };
    }
    public static bool Save(bool notify=true)
    {
        if(I==null||I.loading||RoomServices.Threatened){if(notify)HUD.ShowToast("Сохраняться можно вне боя и испытаний");return false;}
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            string temp=FilePath+".tmp";
            File.WriteAllText(temp,JsonUtility.ToJson(Capture(),true));
            if(File.Exists(FilePath))File.Copy(FilePath,FilePath+".bak",true);
            File.Copy(temp,FilePath,true);File.Delete(temp);
            if(notify)HUD.ShowToast("Прогресс сохранён [F5]");return true;
        }
        catch(Exception e){Debug.LogWarning("Save failed: "+e.Message);if(notify)HUD.ShowToast("Не удалось записать сохранение");return false;}
    }
    public static bool Load()
    {
        if(I.loading||RoomServices.Threatened){HUD.ShowToast("Загрузка недоступна во время боя");return false;}
        try
        {
            if(!Exists){HUD.ShowToast("Сохранения пока нет");return false;}
            var data=JsonUtility.FromJson<GameSave>(File.ReadAllText(FilePath));
            if(data==null||data.version!=1||!Enum.TryParse(data.step,out QuestManager.Step parsed)||!Enum.IsDefined(typeof(QuestManager.Step),parsed)
                ||data.potions==null||data.owned==null||data.spells==null||data.trials==null||data.trials.Length!=3||data.house < -1||data.house>3)
                throw new InvalidDataException("Invalid save format");
            I.StartCoroutine(I.Restore(data,parsed));return true;
        }
        catch(Exception e){Debug.LogWarning("Load failed: "+e.Message);HUD.ShowToast("Сохранение повреждено или несовместимо");return false;}
    }
    IEnumerator Restore(GameSave d,QuestManager.Step step)
    {
        loading=true;GameState.Set(GameMode.Paused);Dialogue.Reset();
        foreach(var e in Enemy.All.ToArray())if(e!=null)Destroy(e.gameObject);
        foreach(var r in FindObjectsByType<BroomRing>(FindObjectsSortMode.None))Destroy(r.gameObject);
        foreach(var t in FindObjectsByType<TrainingDummy>(FindObjectsSortMode.None))Destroy(t.gameObject);
        foreach(var t in FieldGuidePage.All.ToArray())if(t!=null)Destroy(t.gameObject);
        yield return null;
        var p=GameManager.I.player;var inv=p.inventory;var q=QuestManager.I;
        q.house=d.house;if(d.house>=0)p.SetHouse(d.house);q.pages=d.pages;q.enemiesDefeated=d.defeated;q.playTime=d.playTime;
        p.hasBroom=d.broom;p.ancientUnlocked=d.ancient;p.caster.protegoLearned=d.protego;
        p.caster.learned.Clear();foreach(var s in p.caster.catalog)s.learned=false;
        foreach(var s in d.spells)if(Enum.TryParse(s,out SpellType t))p.caster.Learn(t);
        p.caster.page=0;inv.owned.Clear();foreach(var id in d.owned)if(ItemDB.Get(id)!=null)inv.owned.Add(id);
        inv.equipped.Clear();p.health.max=150;
        foreach(var x in d.equipped??Array.Empty<SaveEntry>())if(inv.owned.Contains(x.id))inv.Equip(ItemDB.Get(x.id));
        inv.ApplyVisuals();
        inv.galleons=Mathf.Max(0,d.gold);inv.earnedTotal=d.earned;
        foreach(var key in inv.potions.Keys.ToArray())inv.potions[key]=0;
        foreach(var x in d.potions)if(inv.potions.ContainsKey(x.id))inv.potions[x.id]=Mathf.Clamp(x.count,0,Inventory.MaxStack);
        for(int i=0;i<3;i++)Trials.I.done[i]=d.trials[i];Trials.I.active=-1;
        SideQuests.I.RestoreCompleted(d.side??Array.Empty<SaveEntry>());
        RoomServices.I.herbs=d.herbs;RoomServices.I.visited.Clear();foreach(var v in d.visited??Array.Empty<string>())RoomServices.I.visited.Add(v);
        RoomServices.I.harvestReady=Time.time+d.harvestRemaining;RoomServices.I.mealReady=Time.time+d.mealRemaining;
        inv.BreakInvisibility();inv.maximaT=inv.edurusT=inv.focusT=inv.regenT=inv.felixT=0;
        FieldGuidePage.Collected.Clear();foreach(var id in d.collectedPages??Array.Empty<int>())FieldGuidePage.Collected.Add(id);
        for(int i=0;i<WorldBuilder.PageSpots.Length;i++)if(!FieldGuidePage.Collected.Contains(i))FieldGuidePage.Spawn(WorldBuilder.PageSpots[i]);
        foreach(var c in Chest.All)c.RestoreOpened((d.openedChests??Array.Empty<Vector3>()).Any(v=>Vector3.Distance(v,c.transform.position)<.1f));
        if(step>=QuestManager.Step.FigReveal)q.Npc("fig").transform.position=WorldBuilder.Courtyard+new Vector3(3,0,6);
        q.SetStep(step,false);q.RestoreSeals(d.seals);
        // Partial objectives restart at their step; completed quests never pay again.
        p.Teleport(d.position);p.health.current=Mathf.Clamp(d.health,1,p.health.max);
        if(p.onBroom)p.ToggleBroom();if(q.relic!=null)q.relic.gameObject.SetActive(step<=QuestManager.Step.TakeRelic);
        GameManager.I.camRig.SnapBehind();loading=false;GameState.Set(GameMode.Play);HUD.ShowToast("Прогресс загружен [F9]");
    }
}
