using System.Collections.Generic;
using UnityEngine;

public class CastleLocation
{
    public string name, detail;
    public Rect area;
    public int level;
    public Vector3 spawn;
    public CastleLocation(string n, string d, Rect r, int l, Vector3 p)
    { name = n; detail = d; area = r; level = l; spawn = p; }
}

public static class CastleLocations
{
    public static readonly List<CastleLocation> All = new()
    {
        new("Двор", "Главный вход и фонтан", new Rect(-25,-25,50,50),0,new Vector3(0,.2f,-15)),
        new("Большой зал", "Распределение и пиры",new Rect(-14,25,28,60),0,new Vector3(0,.2f,36)),
        new("Библиотека", "Книги, читальный зал и лестница на галерею",new Rect(-38,30,24,24),0,new Vector3(-20,.2f,43)),
        new("ЗОТИ", "Класс защиты от тёмных искусств",new Rect(-38,54,24,24),0,new Vector3(-17,.2f,60)),
        new("Гостиная", "Гриффиндор: камин и диваны",new Rect(14,30,24,24),0,new Vector3(20,.2f,39)),
        new("Спальня", "Кровати с балдахинами",new Rect(14,54,24,24),0,new Vector3(18,.2f,60)),
        new("Трофеи", "Очки факультетов; проход в северное крыло",new Rect(-9,85,18,18),0,new Vector3(0,.2f,94)),
        new("Кухня", "Под Большим залом. Вход: картина с фруктами во дворе [E]",new Rect(-12,32,24,42),-1,new Vector3(0,-6.7f,36)),
        new("Запретная секция", "Редкие книги на верхней галерее",new Rect(-38,30,18,24),1,new Vector3(-26,6.25f,40)),
        new("Галерея", "Читальные столы; мост к астрономической башне",new Rect(-38,54,24,24),1,new Vector3(-23,6.25f,60)),
        new("Башня", "Винтовая лестница начинается у северной стены",new Rect(-32,84,12,12),1,new Vector3(-29,6.25f,87)),
        new("Обсерватория", "Смотровая площадка на высоте 24 м",new Rect(-32,84,12,12),2,new Vector3(-26,24.25f,91)),
        new("Лазарет", "Кровати, лекарства и место отдыха",new Rect(-23,110,18,18),0,new Vector3(-14,.2f,119)),
        new("Зельеварение", "Котлы, ингредиенты и рабочие столы",new Rect(5,110,18,18),0,new Vector3(14,.2f,119)),
        new("Кабинет чар", "Парты, свечи и тренировочные предметы",new Rect(-23,128,18,18),0,new Vector3(-14,.2f,137)),
        new("Оранжерея", "Растения и столы травологии",new Rect(5,128,18,18),0,new Vector3(14,.2f,137)),
        new("Северный коридор", "Соединяет новые классы и трофейный зал",new Rect(-5,103,10,43),0,new Vector3(0,.2f,124)),
    };
    public static int Floor(Vector3 p) => p.y < -2 ? -1 : p.y > 18 ? 2 : p.y > 4 ? 1 : 0;
    public static bool Contains(CastleLocation l, Vector3 p) => l.level == Floor(p) && l.area.Contains(new Vector2(p.x,p.z));
    public static string Current(Vector3 p)
    {
        foreach (var l in All) if (Contains(l,p)) return l.name;
        return "Окрестности Хогвартса";
    }
}

public static partial class WorldBuilder
{
    static void MoreLocations()
    {
        Kitchen();
        // Walkable central corridor connects all four rooms to the trophy hall.
        Prims.Box(Root,new Vector3(0,-.1f,124.5f),new Vector3(10,.3f,43),floor);
        foreach (var l in CastleLocations.All)
        {
            if (l.level != 0 || l.area.yMin < 110 || l.name == "Северный коридор") continue;
            AnnexRoom(l);
        }
        Prims.Box(Root,new Vector3(0,3,146),new Vector3(10,6,.4f),stone);
        Prims.Box(Root,new Vector3(0,6.2f,124.5f),new Vector3(10,.3f,43),woodDark);
        KLight(new Vector3(0,4,116),22,1.2f);
        KLight(new Vector3(0,4,136),22,1.2f);
    }

    static void AnnexRoom(CastleLocation l)
    {
        float x = l.area.xMin, z = l.area.yMin;
        KFloor(x,z,3,3,"floor_tile_large");
        KWalls(new Vector3(x,0,z),Vector3.right,Vector3.forward,"wow");
        KWalls(new Vector3(x,0,z+18),Vector3.right,Vector3.back,"wow");
        KWalls(new Vector3(x,0,z),Vector3.forward,Vector3.right,x < 0 ? "owo" : "wdw");
        KWalls(new Vector3(x+18,0,z),Vector3.forward,Vector3.left,x < 0 ? "wdw" : "owo");
        KRoof(new Vector3(x+9,0,z+9),18,18);
        KLight(new Vector3(x+9,4.5f,z+9),18,1.5f);
        Sign(l.name,new Vector3(x < 0 ? -4.1f : 4.1f,3,z+9),x < 0 ? 90 : -90);
        if (l.name == "Лазарет")
        {
            for(int i=0;i<4;i++)
            {
                Kit.Place(kitRoot,"bed_single_A",new Vector3(x+3+i*3.5f,FloorY,z+3),0,1,true,true);
                Kit.Place(kitRoot,"table_small_decorated_A",new Vector3(x+3+i*3.5f,FloorY,z+6),0,FS,true);
            }
            Kit.Place(kitRoot,"cabinet_medium_decorated",new Vector3(x+3,FloorY,z+15),0,FS,true);
        }
        else if(l.name == "Оранжерея")
        {
            var plant = MatLib.Solid(new Color(.2f,.55f,.16f));
            for(int i=0;i<4;i++) for(int j=0;j<2;j++)
            {
                var p = new Vector3(x+3+i*3.5f,FloorY,z+3+j*11);
                Kit.Place(kitRoot,"table_medium",p,0,FS,true);
                Prims.Cyl(Root,p+Vector3.up*.8f,.35f,.4f,wood,false);
                Prims.Sphere(Root,p+Vector3.up*1.6f,1,plant);
            }
        }
        else
        {
            for(int i=0;i<3;i++) for(int j=0;j<2;j++)
            {
                var p = new Vector3(x+4+i*5,FloorY,z+4+j*10);
                Kit.Place(kitRoot,"table_medium",p,0,FS,true);
                Kit.Place(kitRoot,"stool",p+Vector3.back*1.3f,0,FS);
                if(l.name == "Зельеварение")
                {
                    Prims.Sphere(Root,p+Vector3.up*1.05f,.65f,stoneDark);
                    Prims.Cyl(Root,p+Vector3.up*1.25f,.27f,.05f,MatLib.Glow(new Color(.4f,.8f,.25f)),false);
                    Kit.Place(kitRoot,"bottle_A_labeled_green",p+new Vector3(.6f,.8f,0),0,.55f);
                }
                else Kit.Place(kitRoot,"candle_triple",p+Vector3.up*.8f,0,FS);
            }
        }
    }

    static void Kitchen()
    {
        const float y = -6.85f;
        Prims.Box(Root,new Vector3(0,y-.15f,53),new Vector3(24,.3f,42),floor);
        foreach(int s in new[]{-1,1})
            Prims.Box(Root,new Vector3(s*12,y+2.8f,53),new Vector3(.4f,5.6f,42),stone);
        foreach(float z in new[]{32f,74f})
            Prims.Box(Root,new Vector3(0,y+2.8f,z),new Vector3(24,5.6f,.4f),stone);
        Prims.Box(Root,new Vector3(0,y+5.7f,53),new Vector3(24,.3f,42),stone);
        foreach(float x in new[]{-8f,-3f,3f,8f}) for(int j=0;j<5;j++)
            Kit.Place(kitRoot,"table_long_tablecloth_decorated_A",new Vector3(x,y,42+j*5),0,FS,true);
        for(int i=0;i<6;i++)
        {
            Kit.Place(kitRoot,"barrel_large",new Vector3(-10,y,36+i*6),0,FS,true);
            Kit.Place(kitRoot,"cabinet_medium_decorated",new Vector3(10,y,36+i*6),90,FS,true);
        }
        Prims.Box(Root,new Vector3(0,y+1.5f,72.5f),new Vector3(6,3,1.5f),stoneDark);
        FX.Loop(kitRoot,new Vector3(0,y+.5f,71.5f),new Color(1,.5f,.1f),25,.7f,1.3f);
        KLight(new Vector3(0,y+4,46),22,2);
        KLight(new Vector3(0,y+4,65),20,2);
        Sign("КУХНЯ",new Vector3(0,y+3,33),0);
        CastleDoor.Create("Кухня — картина с фруктами",new Vector3(12,.1f,20),new Vector3(0,y+.2f,36),true);
        CastleDoor.Create("Вернуться во двор",new Vector3(-9,y+.1f,34),new Vector3(11,.2f,18),false);
    }

    static void Sign(string text,Vector3 p,float yaw)
    {
        var g=new GameObject("Sign: "+text); g.transform.SetParent(kitRoot,false);
        g.transform.SetPositionAndRotation(p,Quaternion.Euler(0,yaw,0));
        var t=g.AddComponent<TextMesh>(); t.text=text; t.fontSize=42; t.characterSize=.07f;
        t.anchor=TextAnchor.MiddleCenter; t.color=new Color(1,.85f,.55f);
    }
}

public class CastleDoor : MonoBehaviour
{
    public static readonly List<CastleDoor> All = new();
    public string label;
    public Vector3 destination;
    public static void Create(string name,Vector3 p,Vector3 target,bool portrait)
    {
        var g=new GameObject(name); g.transform.SetParent(WorldBuilder.Dynamic,false); g.transform.position=p;
        var d=g.AddComponent<CastleDoor>(); d.label=name; d.destination=target;
        var wood=MatLib.Solid(new Color(.3f,.17f,.08f));
        Prims.Box(g.transform,new Vector3(0,1.4f,0),new Vector3(2.4f,2.8f,.15f),wood,false);
        if(portrait) for(int i=0;i<5;i++)
            Prims.Sphere(g.transform,new Vector3(-.7f+i*.35f,1.4f+(i%2)*.2f,-.2f),.28f,MatLib.Solid(i==2?Color.green:Color.yellow));
    }
    void OnEnable(){All.Add(this);} void OnDisable(){All.Remove(this);}
    public static CastleDoor Nearby(Vector3 p) => All.Find(d => (d.transform.position-p).sqrMagnitude < 7);
    public void Use(PlayerController p){p.Teleport(destination); GameManager.I.camRig.SnapBehind(); SFX.Play("chest",.5f);}
}
