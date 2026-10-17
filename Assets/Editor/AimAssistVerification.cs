#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
public static class AimAssistVerification
{
    public static void Run()
    {
        var root = new GameObject("Aim assist verification");
        try
        {
            Vector3 eye = new(1000,1001,1000);
            Targetable Make(Vector3 delta)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.transform.SetParent(root.transform);
                go.transform.position = eye + delta - Vector3.up;
                go.GetComponent<CapsuleCollider>().center = Vector3.up;
                var h = go.AddComponent<Health>();
                var t = go.AddComponent<Targetable>(); t.health = h; t.aimHeight = 1;
                if (!Targetable.All.Contains(t)) Targetable.All.Add(t);
                return t;
            }
            void Check(bool ok, string text) { if(!ok) throw new System.Exception(text); }
            var a = Make(new Vector3(1,0,10));
            Physics.SyncTransforms();
            Targetable Pick(Targetable current) => WandAim.SelectTarget(eye,Vector3.forward,eye,null,current);
            Check(Pick(null)==a,"Acquire near crosshair");
            a.transform.position += Vector3.right*3;
            Physics.SyncTransforms();
            Check(Pick(a)==a,"Retain running enemy outside acquisition cone");
            Check(Pick(null)==null,"No broad accidental acquisition");
            var b = Make(new Vector3(0,0,8));
            Physics.SyncTransforms();
            Check(Pick(a)==b,"Direct aim switches targets");
            b.health.current=0;
            Object.DestroyImmediate(b.gameObject);
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.SetParent(root.transform);
            wall.transform.position=eye+new Vector3(2,0,5);
            wall.transform.localScale=new Vector3(4,4,1);
            Physics.SyncTransforms();
            Check(Pick(a)==null,"Wall breaks lock");
            Object.DestroyImmediate(wall);
            a.health.current=0;
            Physics.SyncTransforms();
            Check(Pick(a)==null,"Dead targets released");
            a.health.current=100;
            a.transform.position=eye+new Vector3(0,-1,70);
            Physics.SyncTransforms();
            Check(Pick(a)==null,"Range breaks lock");
            Debug.Log("HM_AIM_RESULT checks=7 errors=0");
        }
        catch(System.Exception e) { Debug.LogException(e); EditorApplication.Exit(1); return; }
        finally { Targetable.All.RemoveAll(t => t == null || t.transform.IsChildOf(root.transform)); Object.DestroyImmediate(root); }
        ProjectSetup.Build();
    }
}
#endif
