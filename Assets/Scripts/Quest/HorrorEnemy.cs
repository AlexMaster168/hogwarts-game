using System.Collections.Generic;
using UnityEngine;

// Дополнительные способности вариантов Завесы; базовый Enemy сохраняет статусы,
// сопротивления боссов, награды и пересчёт выбранной сложности.
public class HorrorEnemy : MonoBehaviour
{
    Enemy enemy;
    bool leader, sovereign;
    int phase;
    float cooldown=5f, windup;
    Vector3 strike;
    GameObject warning;
    List<Enemy> group;
    Color tint;
    public void Initialize(bool boss, bool final, List<Enemy> owned)
    {
        enemy=GetComponent<Enemy>(); leader=boss; sovereign=final; group=owned;
        tint=leader?new Color(.9f,.1f,.35f):new Color(.25f,.65f,.75f);
        if(leader)return;
        var crown=Prims.Group(enemy.model,"Осколки Завесы");
        for(int i=0;i<5;i++)
            Prims.Cone(crown,new Vector3((i-2)*.16f,leader?2.1f:1.65f,0),.09f,.6f,MatLib.Glow(tint),false);
    }
    void Update()
    {
        if(enemy==null||enemy.IsDead){ClearWarning();return;}
        if(!GameState.Playing)return;
        var p=GameManager.I.player;
        float distance=Vector3.Distance(transform.position,p.transform.position);
        if(!enemy.aggro||enemy.Charmed||distance>30||enemy.stunT>0||enemy.frozenT>0||enemy.levitateT>0)
        {ClearWarning();return;}
        if(leader && phase<(sovereign?2:1) && enemy.health.Frac<(phase==0?.65f:.3f))
        {
            phase++;
            for(int i=0;i<2;i++)
            {
                // Звонарь зовёт гончих, привратник — плакальщиц, король — певчих, а затем гончих.
                var k=enemy.kind==EnemyKind.Gatekeeper?EnemyKind.Mourner:enemy.kind==EnemyKind.NamelessKing&&phase==1?(i==0?EnemyKind.Chorister:EnemyKind.Mourner):EnemyKind.GraveHound;
                var add=Enemy.Spawn(k,transform.position+new Vector3(i==0?-5:5,0,-4));
                add.Died+=_=>QuestManager.I.enemiesDefeated++;
                group.Add(add);
            }
            HUD.ShowToast("Завеса призывает стражу!",tint);
        }
        if(warning!=null)
        {
            windup-=Time.deltaTime;
            if(windup>0)return;
            float radius=leader?4f:2.5f;
            FX.Shockwave(strike,tint,radius);
            if(Vector3.Distance(p.transform.position,strike)<radius)
                p.ReceiveMelee((leader?24:12)*Difficulty.Cur.dmg,true,enemy,Vector3.up*2);
            ClearWarning();return;
        }
        cooldown-=Time.deltaTime*Difficulty.Cur.attack;
        if(cooldown>0)return;
        cooldown=leader?6:9;
        // Фиксируется позиция, а не сам игрок: из метки можно выйти или перекатиться.
        strike=p.transform.position;
        warning=Prims.Cyl(WorldBuilder.Dynamic,strike+Vector3.up*.04f,leader?4:2.5f,.04f,MatLib.Glow(tint),false);
        windup=1.8f/Difficulty.Cur.tele;
        HUD.ShowToast(leader?"Разлом под ногами — уходи из круга!":"Проклятая земля — отступи!",tint);
    }
    void ClearWarning(){ if(warning!=null)Destroy(warning); warning=null; windup=0; }
    void OnDisable()=>ClearWarning();
}
