using UnityEngine;

// V is free: M remains the thunder potion key.
public class CastleMap : MonoBehaviour
{
    public static CastleMap I;
    public static int ChangedFrame = -1;
    public int level;
    public int selected = -1;
    public Vector3? target;
    Vector2 scroll;
    float zoom = 1;
    Vector2 pan;
    GUIStyle label, small;
    Rect canvas;
    public static readonly Rect Bounds = new Rect(-55,-30,110,180);
    void Awake(){I=this;}
    public void Open()
    {
        level=CastleLocations.Floor(GameManager.I.player.transform.position);
        zoom=1; pan=Vector2.zero; ChangedFrame=Time.frameCount; GameState.Set(GameMode.Map);
    }
    public void Close(){ChangedFrame=Time.frameCount;GameState.Set(GameMode.Play);}
    public void Select(int index){selected=index;target=CastleLocations.All[index].spawn;}
    public static Vector2 Project(Vector3 p, Rect area, Rect world)
        => new Vector2(area.x+(p.x-world.xMin)/world.width*area.width,area.yMax-(p.z-world.yMin)/world.height*area.height);
    void Update()
    {
        if(GameState.Playing && Input.GetKeyDown(KeyCode.V)) Open();
        else if(GameState.Mode==GameMode.Map && (Input.GetKeyDown(KeyCode.V)||Input.GetKeyDown(KeyCode.Escape)))Close();
    }
    static void Box(Rect r,Color c){GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;}
    void OnGUI()
    {
        if(GameManager.I==null || GameManager.I.player==null)return;
        if(label==null)
        {
            label=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,wordWrap=true,fontSize=14};
            small=new GUIStyle(GUI.skin.label){wordWrap=true,fontSize=14};
        }
        var pc=GameManager.I.player;
        GUI.depth=-10;
        if(GameState.Mode!=GameMode.Map)
        {
            if(GameState.Playing)
            {
                var r=new Rect(16,16,200,160);
                Box(r,new Color(.05f,.08f,.12f,.85f));
                var miniWorld=new Rect(pc.transform.position.x-40,pc.transform.position.z-32,80,64);
                GUI.BeginGroup(r);
                DrawPlan(new Rect(0,0,r.width,r.height),miniWorld,CastleLocations.Floor(pc.transform.position),false);
                GUI.EndGroup();
                GUI.Label(new Rect(16,180,260,45),CastleLocations.Current(pc.transform.position)+"\n[V] Карта",small);
                if(target.HasValue)
                    GUI.Label(new Rect(16,225,270,45),"До выбранной цели: "+Vector3.Distance(pc.transform.position,target.Value).ToString("0")+" м",small);
                var door=CastleDoor.Nearby(pc.transform.position);
                if(door!=null)GUI.Label(new Rect(Screen.width*.25f,Screen.height*.58f,Screen.width*.5f,40),"[E] "+door.label,label);
                else
                {
                    string hint=QuestManager.I.InteractionHint()??RoomServices.Hint(pc);
                    if(hint!=null)GUI.Label(new Rect(Screen.width*.25f,Screen.height*.58f,Screen.width*.5f,40),hint,label);
                }
            }
            return;
        }
        Box(new Rect(0,0,Screen.width,Screen.height),new Color(.025f,.035f,.06f,.98f));
        GUI.Label(new Rect(25,12,Screen.width-50,35),"ХОГВАРТС — карта замка",new GUIStyle(label){fontSize=25});
        string[] tabs={"Подземелье","Первый этаж","Галерея","Башня","Окрестности"};
        for(int i=0;i<5;i++)
            if(GUI.Toggle(new Rect(25+i*150,55,145,30),level==i-1,tabs[i],GUI.skin.button) && level!=i-1){level=i-1;pan=Vector2.zero;zoom=1;}
        if(GUI.Button(new Rect(Screen.width-120,15,100,30),"Закрыть [V]"))Close();
        canvas=new Rect(25,100,Screen.width-345,Screen.height-155);
        Box(canvas,new Color(.13f,.105f,.075f));
        var baseWorld=level==3?new Rect(-300,-250,600,600):Bounds;
        var world=new Rect(baseWorld.center+pan-baseWorld.size/(zoom*2),baseWorld.size/zoom);
        var ev=Event.current;
        if(canvas.Contains(ev.mousePosition) && ev.type==EventType.ScrollWheel){zoom=Mathf.Clamp(zoom-ev.delta.y*.08f,1,3);ev.Use();}
        if(canvas.Contains(ev.mousePosition)&&ev.type==EventType.MouseDrag&&ev.button==1){pan+=new Vector2(-ev.delta.x/canvas.width*world.width,ev.delta.y/canvas.height*world.height);ev.Use();}
        GUI.BeginGroup(canvas);
        DrawPlan(new Rect(0,0,canvas.width,canvas.height),world,level,true);
        GUI.EndGroup();
        GUI.Label(new Rect(canvas.x,canvas.y,55,25),"С ↑",label);
        GUI.Label(new Rect(25,Screen.height-50,Screen.width-50,45),"Колесо: масштаб · Правая кнопка: сдвиг · Выберите помещение справа для метки.\nСхема нашей игры по мотивам Хогвартса; проходы и этажи соответствуют игровому замку.",small);
        scroll=GUI.BeginScrollView(new Rect(Screen.width-300,100,280,Screen.height-230),scroll,new Rect(0,0,255,CastleLocations.All.Count*40));
        int row=0;
        for(int i=0;i<CastleLocations.All.Count;i++)
        {
            var l=CastleLocations.All[i]; if(level!=3 && l.level!=level)continue;
            if(GUI.Button(new Rect(0,row++*40,250,35),l.name))Select(i);
        }
        GUI.EndScrollView();
        if(selected>=0)
        {
            var l=CastleLocations.All[selected];
            GUI.Label(new Rect(Screen.width-300,Screen.height-130,275,80),l.name+"\n"+l.detail,small);
            GUI.enabled=RoomServices.I.visited.Contains(l.name)&&!RoomServices.Threatened&&pc.inventory.galleons>=25;
            if(GUI.Button(new Rect(Screen.width-300,Screen.height-180,275,35),"Летучий порох — 25 г."))RoomServices.I.Travel(selected);
            GUI.enabled=true;
        }
    }
    void DrawPlan(Rect area,Rect world,int floor,bool names)
    {
        if(floor==3)
        {
            WorldDot("Хогсмид",WorldBuilder.HogsmeadeCenter,area,world);
            WorldDot("Запретный лес",WorldBuilder.ForestCenter,area,world);
            WorldDot("Озеро",WorldBuilder.LakeCenter,area,world);
            WorldDot("Квиддич",WorldBuilder.PitchCenter,area,world);
            WorldDot("Руины",WorldBuilder.RuinsCenter,area,world);
        }
        foreach(var l in CastleLocations.All)
        {
            if(floor!=3 && l.level!=floor)continue;
            if(floor==3 && l.level!=0)continue;
            var a=Project(new Vector3(l.area.xMin,0,l.area.yMax),area,world);
            var b=Project(new Vector3(l.area.xMax,0,l.area.yMin),area,world);
            var r=Rect.MinMaxRect(a.x,a.y,b.x,b.y);
            Box(r,new Color(.73f,.58f,.33f));
            Box(new Rect(r.x+2,r.y+2,Mathf.Max(0,r.width-4),Mathf.Max(0,r.height-4)),new Color(.3f,.24f,.16f));
            if(names && floor!=3)GUI.Label(r,l.name,label);
        }
        if(names && floor==0)
        {
            foreach(var door in CastleDoor.All)
                if(door.transform.position.y>=0) WorldDot("Кухня [E]",door.transform.position,area,world);
            WorldDot("Лестница ↑",new Vector3(-17,0,32),area,world);
            foreach(var v in new[]{new Vector3(0,0,85),new Vector3(0,0,103),new Vector3(-5,0,119),new Vector3(5,0,119),new Vector3(-5,0,137),new Vector3(5,0,137)})
            {var point=Project(v,area,world);Box(new Rect(point.x-4,point.y-4,8,8),new Color(.65f,1,.65f));}
        }
        var p=GameManager.I.player.transform.position;
        if(floor==3 || CastleLocations.Floor(p)==floor)
        {
            var v=Project(p,area,world); Box(new Rect(v.x-5,v.y-5,10,10),Color.cyan);
            var dir=GameManager.I.player.transform.forward;
            var end=Project(p+dir*4,area,world);Box(new Rect(end.x-2,end.y-2,4,4),Color.white);
        }
        if(target.HasValue && (floor==3 || CastleLocations.Floor(target.Value)==floor))
        {var v=Project(target.Value,area,world);Box(new Rect(v.x-6,v.y-6,12,12),Color.yellow);}
        if(QuestManager.I!=null && QuestManager.I.hasMarker && (floor==3||CastleLocations.Floor(QuestManager.I.markerPos)==floor))
        {var v=Project(QuestManager.I.markerPos,area,world);Box(new Rect(v.x-4,v.y-4,8,8),Color.green);}
    }
    void WorldDot(string n,Vector3 p,Rect area,Rect world)
    {var v=Project(p,area,world);Box(new Rect(v.x-4,v.y-4,8,8),Color.yellow);GUI.Label(new Rect(v.x-65,v.y+5,130,30),n,label);}
}
