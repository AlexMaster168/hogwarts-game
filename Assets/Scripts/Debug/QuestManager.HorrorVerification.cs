using System.Collections;
using UnityEngine;
public partial class QuestManager
{
    IEnumerator VerifyHorror()
    {
        yield return null;
        SaveGame.TestPath=System.IO.Path.Combine(Application.temporaryCachePath,"horror-verification.json");
        int errors=0;
        var args=System.Environment.GetCommandLineArgs();
        int arg=System.Array.IndexOf(args,"-hmDifficulty");
        if(arg>=0 && arg+1<args.Length && int.TryParse(args[arg+1],out var difficulty))Difficulty.Set(difficulty);
        System.Action<bool,string> check=(ok,label)=>{if(!ok){errors++;Debug.LogError("HM_HORROR_CHECK "+label);}};
        P.health.invulnerable=true;
        SetStep(Step.Ch4Call,false);
        int stages=0;
        while(HorrorActive && stages<30)
        {
            var before=step;
            GameState.Set(GameMode.Play);
            P.Teleport(markerPos+Vector3.back*2);
            yield return null;
            var captured=JsonUtility.FromJson<GameSave>(JsonUtility.ToJson(SaveGame.Capture()));
            check(captured.step==step.ToString()&&captured.difficulty==Difficulty.Index,"saved step and difficulty");
            if(horrorBattle)
            {
                var snapshot=horrorEnemies.ToArray();
                foreach(var e in snapshot)
                {
                    check(e.targetable.displayName.Length>0,"name");
                    e.aggro=true;
                    if(e.IsBossKind&&e.GetComponent<HorrorEnemy>()!=null)
                    {
                        e.health.current=e.health.max*.25f;
                        yield return null; yield return null;
                        check(horrorEnemies.Count>snapshot.Length,"boss summons");
                    }
                }
                foreach(var e in horrorEnemies.ToArray()) if(e!=null&&!e.IsDead)e.Kill();
                yield return null;
            }
            else if(talkTarget!=null) TalkHorror(Npc(talkTarget));
            else check(InteractHorror(),"interaction "+step);
            // Exercise the real dialogue completion callback.
            for(int i=0;i<12 && GameState.Mode==GameMode.Dialogue;i++)
            {
                Dialogue.Advance();
                yield return null;
            }
            check(step!=before,"transition "+before);
            if(step==before)break;
            stages++;
        }
        check(stages==25 && step==Step.Free,"25 stages reach Free");
        yield return null;
        check(horrorEnemies.Count==0,"owned enemies cleaned");
        check(horrorComplete,"completion flag");
        P.Teleport(WorldBuilder.Courtyard);
        check(SaveGame.Save(false),"save completed story");
        horrorComplete=false;
        check(SaveGame.Load(),"load completed story");
        for(int i=0;i<8;i++)yield return null;
        check(horrorComplete&&step==Step.Free,"completion restored");
        GameState.Set(GameMode.Paused);
        Debug.Log($"HM_HORROR_RESULT errors={errors} stages={stages} step={step} difficulty={Difficulty.Index}");
        Application.Quit(errors==0?0:1);
    }
}
