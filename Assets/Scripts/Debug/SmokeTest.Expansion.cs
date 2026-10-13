using System.Collections;
using UnityEngine;

public partial class SmokeTest
{
    IEnumerator ExtensionSteps()
    {
        if(q.step==QuestManager.Step.Aftermath||q.step==QuestManager.Step.FinalReport)
        {yield return Talk("fig",Vector3.back*3);yield break;}
        if(q.step==QuestManager.Step.Archive||q.step==QuestManager.Step.KitchenLetter||q.step==QuestManager.Step.Seals)
        {
            if(p.onBroom)p.ToggleBroom();
            Place(q.markerPos,q.markerPos+Vector3.forward*3);
            if(q.step==QuestManager.Step.Archive)yield return Shot("story_archive");
            if(q.step==QuestManager.Step.Seals&&q.seals==1)yield return Shot("story_seal_tower");
            CheckLocation(q.InteractExtension(),"Story interaction "+q.step);yield break;
        }
        if(q.step==QuestManager.Step.Rift)
        {if(q.boss!=null)yield return FightTarget(q.boss.targetable,75,"story_final_boss");yield break;}
        foreach(var e in q.encounter.ToArray())if(e!=null&&!e.IsDead)
            yield return FightTarget(e.targetable,25,q.step==QuestManager.Step.Convoy?"story_convoy":null);
    }
    IEnumerator FeaturesTest()
    {
        GameState.Set(GameMode.Play);if(p.onBroom)p.ToggleBroom();p.Teleport(WorldBuilder.Spawn);
        var inv=p.inventory;var services=RoomServices.I;
        var e=Enemy.Spawn(EnemyKind.DarkWizard,WorldBuilder.Spawn+Vector3.forward*2,false);
        int drop=e.goldDrop,before=inv.galleons;
        CheckLocation(drop>=60&&drop<=104,"Increased wizard loot");
        e.health.TakeDamage(99999);yield return new WaitForSecondsRealtime(2);
        CheckLocation(inv.galleons>=before+drop,"Coin pickup value");
        services.harvestReady=0;services.herbs=0;
        CheckLocation(services.Harvest()&&services.herbs==6,"Herb harvest");
        CheckLocation(!services.Harvest(),"Harvest cooldown");
        inv.galleons=500;inv.potions["focus"]=0;
        CheckLocation(services.Craft("focus")&&inv.potions["focus"]==1&&services.herbs==4&&inv.galleons==470,"Potion crafting costs");
        services.herbs=0;CheckLocation(!services.Craft("focus")&&inv.galleons==470,"No crafting without ingredients");
        p.health.current=50;services.mealReady=0;
        CheckLocation(services.Meal()&&p.health.current==90&&!services.Meal(),"Meal heal and cooldown");
        services.visited.Add("Кухня");
        CheckLocation(services.Travel(7)&&p.transform.position.y < -6&&inv.galleons==445,"Fast travel fee and destination");
        var book=p.caster.catalog.Find(s=>!s.unforgivable);
        book.learned=false;p.caster.learned.Remove(book);inv.galleons=500;
        CheckLocation(services.Study()&&p.caster.Knows(book.type)&&inv.galleons==300,"Library study cost and spell");
        q.SetStep(QuestManager.Step.Convoy,false);
        var nearFoe=Enemy.Spawn(EnemyKind.DarkWizard,p.transform.position+new Vector3(8,.5f,0),true);
        CheckLocation(!SaveGame.Save()&&!services.Travel(7),"Combat blocks save and fast travel");
        if(nearFoe!=null)Destroy(nearFoe.gameObject);
        foreach(var guard in q.encounter.ToArray())if(guard!=null)guard.health.TakeDamage(99999);
        q.SetStep(QuestManager.Step.Free,false);
        p.Teleport(WorldBuilder.Spawn);
        CheckLocation(RoomServices.TryUse(p)==false,"No service outside rooms");
        p.Teleport(new Vector3(14,.2f,119));RoomServices.TryUse(p);yield return Shot("features_brewing");GameState.Set(GameMode.Play);
        SaveGame.TestPath=System.IO.Path.Combine(Application.dataPath,"..","Verification","test-progress.json");
        p.Teleport(WorldBuilder.Spawn);inv.galleons=1234;services.herbs=9;
        inv.equipped.Clear();p.health.max=Progression.BaseHp;
        CheckLocation(SaveGame.Save(),"Save write");
        inv.galleons=1;services.herbs=0;p.health.max=999;
        CheckLocation(SaveGame.Load(),"Load accepted");yield return Frames(5);
        CheckLocation(inv.galleons==1234&&services.herbs==9&&q.step==QuestManager.Step.Free&&Trials.I.AllDone,
            "Save/load round trip");
        CheckLocation(Mathf.Approximately(p.health.max,Progression.BaseHp),"Unequipped HP restored");
        CheckLocation(SideQuests.I.all.TrueForAll(s=>s.state==SQState.Done),"Completed quests restored");
        string valid=System.IO.File.ReadAllText(SaveGame.FilePath);
        System.IO.File.WriteAllText(SaveGame.FilePath,"{corrupted");
        CheckLocation(!SaveGame.Load()&&inv.galleons==1234,"Corrupt save rejected without changing state");
        System.IO.File.WriteAllText(SaveGame.FilePath,valid);SaveGame.TestPath=null;
        yield return Shot("features_save_loaded");
        Debug.Log("HM_FEATURE_RESULT errors="+errors+" coins="+drop+" saveRoundTrip=True");
    }
}
