using System.Collections;
using UnityEngine;

public partial class SmokeTest
{
    void CheckLocation(bool valid,string message)
    {
        if(valid)return;
        errors++; notes.Add("LOCATION_FAIL: "+message); Debug.LogWarning("LOCATION_FAIL: "+message);
    }
    IEnumerator LocationsTest()
    {
        Physics.SyncTransforms();
        foreach(var l in CastleLocations.All)
        {
            p.Teleport(l.spawn);
            Physics.SyncTransforms();
            bool floor=false;
            foreach(var h in Physics.RaycastAll(l.spawn+Vector3.up*.5f,Vector3.down,1.5f,~0,QueryTriggerInteraction.Ignore))
                if(!(h.collider is CharacterController))floor=true;
            CheckLocation(floor,"No floor: "+l.name);
            CheckLocation(CastleLocations.Contains(l,l.spawn),"Map position: "+l.name);
            if(l.name=="Кухня"||l.name=="Лазарет"||l.name=="Зельеварение"||l.name=="Оранжерея")
            {
                Aim(l.spawn+new Vector3(0,1,l.name=="Лазарет"?-8:8));yield return Shot("location_"+CastleLocations.All.IndexOf(l));
            }
        }
        var entry=CastleDoor.All.Find(d=>d.label.StartsWith("Кухня"));
        CheckLocation(entry!=null,"Kitchen entry missing");
        if(entry!=null)
        {
            entry.Use(p);CheckLocation(p.transform.position.y < -6,"Kitchen transition");
            CastleDoor.All.Find(d=>d.label.StartsWith("Вернуться")).Use(p);
            CheckLocation(p.transform.position.y > 0,"Kitchen return");
        }
        // Walk using the real CharacterController; no teleport between steps.
        p.enabled=false;
        p.Teleport(new Vector3(-17,.25f,31.6f));
        yield return WalkTo(new Vector3(-17,6.2f,51),15);
        CheckLocation(p.transform.position.y>5.8f,"Library stairs ascent");
        p.Teleport(new Vector3(-26,6.45f,93.9f));
        for(int i=1;i<120;i++)
        {
            var destination=new Vector3(-26,6.15f+(i+1)*.15f,90)+Quaternion.Euler(0,i*9f,0)*new Vector3(0,0,3.9f);
            yield return WalkTo(destination,2);
            if(p.transform.position.y < destination.y-.6f){CheckLocation(false,"Tower stair height at tread "+i);break;}
        }
        CheckLocation(p.transform.position.y>23.5f,"Tower stairs ascent");
        yield return WalkTo(new Vector3(-26,24.25f,91.5f),4);
        p.enabled=true;
        Aim(new Vector3(0,4,50));yield return Shot("location_observatory");
        // Walk through the new doorway into each room from the north corridor.
        p.enabled=false;
        foreach(var l in CastleLocations.All)
        {
            if(l.level!=0||l.area.yMin<110||l.name=="Северный коридор")continue;
            p.Teleport(new Vector3(0,.2f,l.spawn.z));
            yield return WalkTo(l.spawn,8);
        }
        p.Teleport(new Vector3(0,.2f,100));
        yield return WalkTo(new Vector3(0,.2f,111),8);
        p.enabled=true;
        Place(new Vector3(-23,6.25f,60),new Vector3(-35,8,64));yield return Shot("location_gallery");
        CastleMap.I.Open();yield return Shot("map_first_floor");
        CheckLocation(Time.timeScale==0&&GameState.Mode==GameMode.Map,"Map pause");
        CastleMap.I.level=0;yield return Shot("map_ground_floor");
        CastleMap.I.level=-1;yield return Shot("map_kitchen");
        CastleMap.I.Select(7);
        CheckLocation(CastleMap.I.target.HasValue && CastleMap.I.target.Value==CastleLocations.All[7].spawn,"Map destination selection");
        var center=CastleMap.Project(new Vector3(0,0,60),new Rect(0,0,100,100),new Rect(-50,10,100,100));
        CheckLocation(Vector2.Distance(center,new Vector2(50,50))<.01f,"Map projection");
        CastleMap.I.Close();CheckLocation(Time.timeScale==1,"Map resume");
        p.Teleport(WorldBuilder.Spawn);
        Debug.Log("HM_LOCATION_RESULT locations="+CastleLocations.All.Count+" errors="+errors);
    }
    IEnumerator WalkTo(Vector3 destination,float timeout)
    {
        float end=Time.realtimeSinceStartup+timeout;
        while(Time.realtimeSinceStartup<end)
        {
            var delta=destination-p.transform.position;delta.y=0;
            if(delta.magnitude<.18f)break;
            float dt=Mathf.Min(Time.deltaTime,.03f);
            p.cc.Move(delta.normalized*Mathf.Min(delta.magnitude,3*dt)+Vector3.down*2*dt);
            yield return null;
        }
        CheckLocation(new Vector2(destination.x-p.transform.position.x,destination.z-p.transform.position.z).magnitude<.3f,
            "Walk blocked at "+p.transform.position+" to "+destination);
    }
}
