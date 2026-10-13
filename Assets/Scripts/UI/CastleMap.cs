using UnityEngine;

// V is free: M remains the thunder potion key.
// Карта: этажи замка, окрестности и Хогсмид. Клик по помещению или точке — выбор; открытые точки — перемещение.
public class CastleMap : MonoBehaviour
{
    public static CastleMap I;
    public static int ChangedFrame = -1;
    public int level;
    public int selected = -1, selectedWp = -1;
    public Vector3? selectedPoint;
    public Vector3? target;
    Vector2 scroll;
    float zoom = 1;
    Vector2 pan;
    GUIStyle label, small, head;
    Rect canvas;
    public static readonly Rect Bounds = new Rect(-55,-30,110,215);
    public static readonly Rect VillageArea = new Rect(110,-200,120,90);
    const int Outskirts = 3, Village = 4;
    static readonly Color WpOn = new(0.45f,0.9f,1f), WpOff = new(0.45f,0.45f,0.5f), Dark = new(0.45f,1f,0.4f);

    void Awake(){I=this;}
    public static bool InVillage(Vector3 p)=>VillageArea.Contains(new Vector2(p.x,p.z));
    public void Open()
    {
        var p=GameManager.I.player.transform.position;
        level=CastleLocations.Current(p)=="Окрестности Хогвартса"?(InVillage(p)?Village:Outskirts):CastleLocations.Floor(p);
        zoom=1; pan=Vector2.zero; ChangedFrame=Time.frameCount; GameState.Set(GameMode.Map);
    }
    public void Close(){ChangedFrame=Time.frameCount;GameState.Set(GameMode.Play);}
    public void Select(int index){selected=index;selectedWp=-1;selectedPoint=null;target=CastleLocations.All[index].spawn;}
    public void SelectWaypoint(int index){selectedWp=index;selected=-1;selectedPoint=null;target=RoomServices.Waypoints[index].pos;}
    public void SelectPoint(Vector3 p){selectedPoint=RoomServices.SafeOutdoor(p);selected=-1;selectedWp=-1;target=selectedPoint;}
    public static Vector3 Unproject(Vector2 v,Rect area,Rect world)
        => new Vector3(world.xMin+(v.x-area.x)/area.width*world.width,0,world.yMin+(area.yMax-v.y)/area.height*world.height);
    public static Vector2 Project(Vector3 p, Rect area, Rect world)
        => new Vector2(area.x+(p.x-world.xMin)/world.width*area.width,area.yMax-(p.z-world.yMin)/world.height*area.height);
    void Update()
    {
        if(GameState.Playing && Input.GetKeyDown(KeyCode.V)) Open();
        else if(GameState.Mode==GameMode.Map && (Input.GetKeyDown(KeyCode.V)||Input.GetKeyDown(KeyCode.Escape)))Close();
    }
    static void Box(Rect r,Color c){GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;}

    Rect BaseWorld()
    {
        if(level==Village)
        {
            float h=VillageArea.width*canvas.height/Mathf.Max(1,canvas.width);
            return new Rect(VillageArea.x,VillageArea.center.y-h/2,VillageArea.width,h);
        }
        return level==Outskirts?new Rect(-300,-250,600,600):Bounds;
    }

    void OnGUI()
    {
        if(GameManager.I==null || GameManager.I.player==null)return;
        if(label==null)
        {
            label=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,wordWrap=true,fontSize=14};
            small=new GUIStyle(GUI.skin.label){wordWrap=true,fontSize=14};
            head=new GUIStyle(GUI.skin.label){fontSize=15,fontStyle=FontStyle.Bold};
            head.normal.textColor=new Color(1f,.83f,.45f);
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
        GUI.Label(new Rect(25,12,Screen.width-50,35),"ХОГВАРТС — карта",new GUIStyle(label){fontSize=25});
        string[] tabs={"Подземелье","Первый этаж","Галерея","Башня","Окрестности","Хогсмид"};
        for(int i=0;i<tabs.Length;i++)
            if(GUI.Toggle(new Rect(25+i*140,55,135,30),level==i-1,tabs[i],GUI.skin.button) && level!=i-1){level=i-1;pan=Vector2.zero;zoom=1;}
        if(GUI.Button(new Rect(Screen.width-120,15,100,30),"Закрыть [V]"))Close();
        canvas=new Rect(25,100,Screen.width-345,Screen.height-155);
        Box(canvas,level==Village?new Color(.2f,.24f,.16f):new Color(.13f,.105f,.075f));
        var baseWorld=BaseWorld();
        var world=new Rect(baseWorld.center+pan-baseWorld.size/(zoom*2),baseWorld.size/zoom);
        var ev=Event.current;
        if(canvas.Contains(ev.mousePosition) && ev.type==EventType.ScrollWheel){zoom=Mathf.Clamp(zoom-ev.delta.y*.08f,1,3);ev.Use();}
        if(canvas.Contains(ev.mousePosition)&&ev.type==EventType.MouseDrag&&ev.button==1){pan+=new Vector2(-ev.delta.x/canvas.width*world.width,ev.delta.y/canvas.height*world.height);ev.Use();}
        GUI.BeginGroup(canvas);
        DrawPlan(new Rect(0,0,canvas.width,canvas.height),world,level,true);
        // Клик по пустому месту окрестностей или Хогсмида — своя точка перемещения.
        var e2=Event.current;
        if(level>=Outskirts&&e2.type==EventType.MouseDown&&e2.button==0&&e2.mousePosition.y>44&&new Rect(0,0,canvas.width,canvas.height).Contains(e2.mousePosition))
        {SelectPoint(Unproject(e2.mousePosition,new Rect(0,0,canvas.width,canvas.height),world));e2.Use();}
        GUI.EndGroup();
        GUI.Label(new Rect(canvas.x,canvas.y,55,25),"С ↑",label);
        // Баннер цели: где она и кнопка, чтобы открыть нужную вкладку.
        var qt=QuestTarget(out var qTitle,out var qCol);
        if(qt.HasValue)
        {
            int tab=TabOf(qt.Value);
            var br=new Rect(canvas.x+60,canvas.y+6,Mathf.Min(620,canvas.width-80),30);
            Box(br,new Color(0,0,0,.75f));Box(new Rect(br.x,br.y,4,br.height),qCol);
            var bs=new GUIStyle(small){fontSize=15,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleLeft};bs.normal.textColor=qCol;
            GUI.Label(new Rect(br.x+10,br.y,br.width-130,br.height),"Цель задания: "+qTitle+" — "+TabNames[tab+1],bs);
            if(tab!=level&&GUI.Button(new Rect(br.xMax-115,br.y+3,110,24),"Показать")){level=tab;pan=Vector2.zero;zoom=1;}
        }
        GUI.Label(new Rect(25,Screen.height-50,Screen.width-50,45),"ЛКМ по помещению, голубой точке или любому месту окрестностей — выбрать · Колесо: масштаб · Правая кнопка: сдвиг\nГолубые точки — места перемещения, розовая — своя точка, зелёные — тёмные маги с непростительными заклинаниями.",small);
        DrawList(pc);
    }

    void DrawList(PlayerController pc)
    {
        var listRect=new Rect(Screen.width-300,100,280,Screen.height-300);
        int rows=RoomServices.Waypoints.Length+CastleLocations.All.Count+2;
        scroll=GUI.BeginScrollView(listRect,scroll,new Rect(0,0,255,rows*40));
        float y=0;
        if(level>=Outskirts)
        {
            GUI.Label(new Rect(0,y,250,30),"Точки перемещения",head);y+=32;
            for(int i=0;i<RoomServices.Waypoints.Length;i++)
            {
                var w=RoomServices.Waypoints[i];
                if(level==Village&&!InVillage(w.pos))continue;
                GUI.color=WpOn;
                if(GUI.Button(new Rect(0,y,250,35),"◆ "+w.name))SelectWaypoint(i);
                GUI.color=Color.white;y+=40;
            }
            if(level==Outskirts){GUI.Label(new Rect(0,y,250,30),"Замок",head);y+=32;}
        }
        if(level!=Village)
            for(int i=0;i<CastleLocations.All.Count;i++)
            {
                var l=CastleLocations.All[i]; if(level!=Outskirts && l.level!=level)continue;
                if(GUI.Button(new Rect(0,y,250,35),l.name))Select(i);
                y+=40;
            }
        GUI.EndScrollView();

        string title=null,detail=null;System.Action go=null;
        if(selected>=0)
        {
            var l=CastleLocations.All[selected];title=l.name;detail=l.detail;
            int s=selected;go=()=>RoomServices.I.Travel(s);
        }
        else if(selectedWp>=0)
        {
            var w=RoomServices.Waypoints[selectedWp];title=w.name;detail=w.detail;
            int s=selectedWp;go=()=>RoomServices.I.TravelTo(s);
        }
        else if(selectedPoint.HasValue)
        {
            var pt=selectedPoint.Value;title="Выбранная точка";detail=$"x {pt.x:0}, z {pt.z:0}";
            go=()=>RoomServices.I.TravelToPoint(pt);
        }
        if(title==null)return;
        bool can=!RoomServices.Threatened&&pc.inventory.galleons>=25;
        GUI.Label(new Rect(Screen.width-300,Screen.height-190,275,90),title+"\n"+detail,small);
        GUI.enabled=can;
        if(GUI.Button(new Rect(Screen.width-300,Screen.height-100,275,40),RoomServices.Threatened?"Нельзя: "+RoomServices.ThreatReason:"Переместиться — 25 г."))go();
        GUI.enabled=true;
    }

    void DrawPlan(Rect area,Rect world,int floor,bool names)
    {
        queue.Clear();taken.Clear();
        if(floor==Village){DrawVillage(area,world,names);return;}
        if(floor==Outskirts)
        {
            WorldDot("Запретный лес",WorldBuilder.ForestCenter,area,world);
            WorldDot("Озеро",WorldBuilder.LakeCenter,area,world);
            for(int i=0;i<Activities.MerlinSites.Length;i++)
                WorldDot(Activities.I!=null&&Activities.I.merlin.Contains(i)?"Мерлин (пройдено)":"Мерлин",Activities.MerlinSites[i],area,world);
            WorldDot("Снитч",Activities.SnitchStand,area,world);
            WorldDot("Дементоры (ночью)",NightSpawns.DementorShore,area,world);
            Dot(WorldBuilder.ShadyPos,area,world,Dark);
            Dot(WorldBuilder.DarkCampPos,area,world,Dark);
        }
        foreach(var l in CastleLocations.All)
        {
            if(floor!=Outskirts && l.level!=floor)continue;
            if(floor==Outskirts && l.level!=0)continue;
            var a=Project(new Vector3(l.area.xMin,0,l.area.yMax),area,world);
            var b=Project(new Vector3(l.area.xMax,0,l.area.yMin),area,world);
            var r=Rect.MinMaxRect(a.x,a.y,b.x,b.y);
            bool sel=selected>=0&&CastleLocations.All[selected]==l;
            Box(r,sel?new Color(1f,.85f,.4f):new Color(.73f,.58f,.33f));
            Box(new Rect(r.x+2,r.y+2,Mathf.Max(0,r.width-4),Mathf.Max(0,r.height-4)),new Color(.3f,.24f,.16f));
            if(floor==Outskirts)taken.Add(r);
            if(names && floor!=Outskirts)
            {
                GUI.Label(r,l.name,label);
                if(GUI.Button(r,GUIContent.none,GUIStyle.none))Select(CastleLocations.All.IndexOf(l));
            }
        }
        if(names && floor==0)
        {
            foreach(var door in CastleDoor.All)
                if(door.transform.position.y>=0) WorldDot("Кухня [E]",door.transform.position,area,world);
            WorldDot("Лестница ↑",new Vector3(-17,0,32),area,world);
            foreach(var v in new[]{new Vector3(0,0,85),new Vector3(0,0,103),new Vector3(-5,0,119),new Vector3(5,0,119),new Vector3(-5,0,137),new Vector3(5,0,137)})
            {var point=Project(v,area,world);Box(new Rect(point.x-4,point.y-4,8,8),new Color(.65f,1,.65f));}
        }
        if(floor==Outskirts && names)Waypoints(area,world);
        FlushLabels(area);
        Markers(area,world,floor);
    }

    // Хогсмид крупно: улица, дома, лавки с подписями, точки перемещения.
    void DrawVillage(Rect area,Rect world,bool names)
    {
        var c=WorldBuilder.HogsmeadeCenter;
        WorldRect(new Rect(c.x-46,c.z-8,92,16),area,world,new Color(.5f,.47f,.4f));
        foreach(var h in WorldBuilder.VillageHouses)
        {
            WorldRect(h,area,world,new Color(.55f,.42f,.3f));
            WorldRect(new Rect(h.x+.6f,h.y+.6f,h.width-1.2f,h.height-1.2f),area,world,new Color(.36f,.27f,.2f));
        }
        WorldRect(new Rect(WorldBuilder.ShadyPos.x-3,WorldBuilder.ShadyPos.z-5,6,6),area,world,new Color(.1f,.16f,.1f));
        foreach(var n in NPC.All)
        {
            if(n==null||n.shopId==null||!InVillage(n.transform.position))continue;
            var title=ItemDB.Shops.TryGetValue(n.shopId,out var sh)?sh.title:n.displayName;
            bool dark=n.shopId=="shady";
            if(dark){Dot(n.home,area,world,Dark);continue;}
            WorldDot(title,n.home,area,world,new Color(1f,.85f,.4f),2);
        }
        if(names)Waypoints(area,world);
        FlushLabels(area);
        Markers(area,world,Village);
    }

    void Waypoints(Rect area,Rect world)
    {
        for(int i=0;i<RoomServices.Waypoints.Length;i++)
        {
            var w=RoomServices.Waypoints[i];
            var v=Project(w.pos,area,world);
            if(!area.Contains(v))continue;
            bool on=true,sel=selectedWp==i;
            var r=new Rect(v.x-8,v.y-8,16,16);
            if(sel)Box(new Rect(r.x-3,r.y-3,22,22),Color.white);
            Box(r,on?WpOn:WpOff);
            Box(new Rect(r.x+4,r.y+4,8,8),new Color(.05f,.1f,.15f));
            taken.Add(new Rect(r.x-3,r.y-3,22,22));
            bool dark=i==1||i==2;
            queue.Add((dark?w.name+"\nнепростительные":w.name,v,dark?Dark:(on?WpOn:new Color(.75f,.75f,.8f)),0));
            if(GUI.Button(new Rect(v.x-14,v.y-14,28,28),GUIContent.none,GUIStyle.none))SelectWaypoint(i);
        }
    }

    void Markers(Rect area,Rect world,int floor)
    {
        bool outside=floor>=Outskirts;
        var p=GameManager.I.player.transform.position;
        if(outside || CastleLocations.Floor(p)==floor)
        {
            var v=Project(p,area,world); Box(new Rect(v.x-5,v.y-5,10,10),Color.cyan);
            var dir=GameManager.I.player.transform.forward;
            var end=Project(p+dir*(floor==Outskirts?12:4),area,world);Box(new Rect(end.x-2,end.y-2,4,4),Color.white);
        }
        if(selectedPoint.HasValue&&outside)
        {var v=Project(selectedPoint.Value,area,world);Box(new Rect(v.x-7,v.y-7,14,14),Color.white);Box(new Rect(v.x-5,v.y-5,10,10),new Color(1f,.4f,.8f));}
        if(target.HasValue && !selectedPoint.HasValue && (outside || CastleLocations.Floor(target.Value)==floor))
        {var v=Project(target.Value,area,world);Box(new Rect(v.x-6,v.y-6,12,12),Color.yellow);}
        // Значок цели текущего задания (сюжет — золотой, поручение в фокусе — голубой).
        var qt=QuestTarget(out var qTitle,out var qCol);
        int view=area.width<300?TabOf(p):floor,tt=qt.HasValue?TabOf(qt.Value):-9;
        if(qt.HasValue && (tt==view || tt>=Outskirts&&view>=Outskirts))
        {
            var v=Project(qt.Value,area,world);
            bool edge=!area.Contains(v);
            v=new Vector2(Mathf.Clamp(v.x,area.xMin+10,area.xMax-10),Mathf.Clamp(v.y,area.yMin+10,area.yMax-10));
            float pulse=1f+.18f*Mathf.Sin(Time.unscaledTime*5f);
            float r=(area.width<300?7:11)*pulse;
            Box(new Rect(v.x-r-2,v.y-r-2,2*r+4,2*r+4),Color.black);
            Box(new Rect(v.x-r,v.y-r,2*r,2*r),qCol);
            if(qTag==null)qTag=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Bold,fontSize=area.width<300?11:16};
            qTag.normal.textColor=Color.black;
            GUI.Label(new Rect(v.x-r,v.y-r,2*r,2*r),"!",qTag);
            if(area.width>=300)
            {
                var lbl=(edge?"→ ":"")+"Задание: "+qTitle;
                var st=new GUIStyle(qTag){fontSize=14};st.normal.textColor=qCol;
                var sz=st.CalcSize(new GUIContent(lbl));
                var lr=new Rect(Mathf.Clamp(v.x-sz.x/2,area.xMin,area.xMax-sz.x-6),v.y-r-26,sz.x+6,22);
                if(lr.y<area.yMin)lr.y=v.y+r+4;
                Box(lr,new Color(0,0,0,.7f));GUI.Label(lr,lbl,st);
            }
        }
    }

    GUIStyle qTag;

    // Куда вести игрока сейчас: поручение в фокусе или сюжет.
    public static Vector3? QuestTarget(out string title,out Color col)
    {
        title=null;col=new Color(1f,.83f,.3f);
        var sq=SideQuests.I;
        if(sq!=null&&sq.tracked!=null)
        {
            var m=sq.TrackedMarker();
            if(m.HasValue){title=sq.tracked.title;col=new Color(.45f,.85f,1f);return m;}
        }
        var q=QuestManager.I;
        if(q!=null&&q.hasMarker){title=q.title;return q.markerPos;}
        return null;
    }

    // На какой вкладке карты лежит точка.
    public static int TabOf(Vector3 p)
        => CastleLocations.Current(p)=="Окрестности Хогвартса"?(InVillage(p)?Village:Outskirts):CastleLocations.Floor(p);

    static readonly string[] TabNames={"подземелье","первый этаж","галерея","башня","окрестности","Хогсмид"};

    static void WorldRect(Rect w,Rect area,Rect world,Color c)
    {
        var a=Project(new Vector3(w.xMin,0,w.yMax),area,world);
        var b=Project(new Vector3(w.xMax,0,w.yMin),area,world);
        Box(Rect.MinMaxRect(a.x,a.y,b.x,b.y),c);
    }

    // Подписи ставятся после всех точек: каждая ищет свободное место вокруг своей точки (снизу, сверху, справа, слева).
    readonly System.Collections.Generic.List<(string text,Vector2 at,Color col,int prio)> queue=new();
    readonly System.Collections.Generic.List<Rect> taken=new();
    GUIStyle tag;

    Vector2 Dot(Vector3 p,Rect area,Rect world,Color col)
    {var v=Project(p,area,world);var r=new Rect(v.x-4,v.y-4,8,8);Box(r,col);taken.Add(r);return v;}

    void WorldDot(string n,Vector3 p,Rect area,Rect world,Color? col=null,int prio=1)
    {var v=Dot(p,area,world,col??Color.yellow);queue.Add((n,v,Color.white,prio));}

    void FlushLabels(Rect area)
    {
        if(tag==null){tag=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,wordWrap=false,fontSize=14};}
        queue.Sort((a,b)=>a.prio.CompareTo(b.prio));
        foreach(var (text,v,col,_) in queue)
        {
            var size=tag.CalcSize(new GUIContent(text));float w=size.x+6,h=size.y;
            Vector2[] offs={new(-w/2,9),new(-w/2,-h-9),new(11,-h/2),new(-w-11,-h/2),new(-w/2,9+h),new(-w/2,-2*h-9),new(11,-h*1.5f),new(-w-11,h*0.5f)};
            Rect best=new Rect(v.x+offs[0].x,v.y+offs[0].y,w,h);float bestHit=float.MaxValue;
            foreach(var o in offs)
            {
                var r=new Rect(v.x+o.x,v.y+o.y,w,h);
                float hit=0;
                foreach(var t in taken)if(t.Overlaps(r)){var x=Rect.MinMaxRect(Mathf.Max(t.xMin,r.xMin),Mathf.Max(t.yMin,r.yMin),Mathf.Min(t.xMax,r.xMax),Mathf.Min(t.yMax,r.yMax));hit+=x.width*x.height;}
                if(r.xMin<area.xMin||r.xMax>area.xMax||r.yMin<area.yMin||r.yMax>area.yMax)hit+=w*h;
                if(hit<bestHit){bestHit=hit;best=r;if(hit<=0)break;}
            }
            taken.Add(best);
            Box(new Rect(best.x-2,best.y,best.width+4,best.height),new Color(0,0,0,.45f));
            tag.normal.textColor=col;GUI.Label(best,text,tag);
        }
        queue.Clear();
    }
}
