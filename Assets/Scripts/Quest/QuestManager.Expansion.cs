using UnityEngine;

public partial class QuestManager
{
    public static readonly Vector3 ConvoyPos = new(230,0,70);
    public static readonly Vector3 RescuePos = new(-220,0,220);
    public static readonly Vector3 RiftPos = new(80,0,260);
    public static readonly Vector3[] SealPos = {new(-23,6.2f,60),new(-26,24.2f,91.5f),new(14,.2f,137)};
    public int seals;
    public bool ExtensionActive => step>=Step.Aftermath && step<=Step.FinalReport;
    public void RestoreSeals(int count)
    {
        if(step!=Step.Seals)return;
        seals=Mathf.Clamp(count,0,2);Marker(SealPos[seals]);MakeClue();
    }
    GameObject clue;
    public string InteractionHint()
    {
        if(!ExtensionActive)return null;
        if(step==Step.Archive||step==Step.KitchenLetter||step==Step.Seals)
            return Vector3.Distance(P.transform.position,markerPos)<3 ? "[E] "+(step==Step.Seals?"Восстановить печать":"Изучить улику") : null;
        return null;
    }
    void ConfigureExtension(Step s)
    {
        checkpoint=WorldBuilder.Courtyard;
        switch(s)
        {
            case Step.Aftermath:
                title="Пепел победы";objective="Поговори с Фигом: древняя магия снова нестабильна";
                Npc("fig").transform.position=WorldBuilder.Courtyard+new Vector3(3,0,6);
                talkTarget="fig";Marker(Npc("fig").transform.position);break;
            case Step.Archive:
                title="След в Запретной секции";objective="Поднимись в библиотеку и изучи хронику [E]";
                Marker(new Vector3(-26,6.2f,40));MakeClue();break;
            case Step.KitchenLetter:
                title="Сова без адреса";objective="На кухне найди записку, спрятанную среди припасов [E]";
                Marker(new Vector3(0,-6.7f,65));MakeClue();break;
            case Step.Convoy:
                title="Перехват каравана";objective="Победи перевозчиков осколков восточнее поля квиддича";
                Marker(ConvoyPos);checkpoint=ConvoyPos+Vector3.back*20;
                StartEncounter((EnemyKind.DarkWizard,ConvoyPos+Vector3.left*7),(EnemyKind.DarkWizard,ConvoyPos+Vector3.right*7),
                    (EnemyKind.Troll,ConvoyPos+Vector3.forward*8));break;
            case Step.Rescue:
                title="Пленник леса";objective="Освободи исследователя, победив стражу на северо-западе";
                Marker(RescuePos);checkpoint=RescuePos+Vector3.back*20;
                StartEncounter((EnemyKind.DarkWizard,RescuePos+Vector3.left*8),(EnemyKind.Spider,RescuePos+Vector3.right*7),
                    (EnemyKind.DarkWizard,RescuePos+Vector3.forward*8));break;
            case Step.Seals:
                title="Три печати Хогвартса";objective="Восстанови печати в библиотеке, обсерватории и оранжерее [E]";
                seals=0;Marker(SealPos[0]);MakeClue();break;
            case Step.Rift:
                title="Сердце разлома";objective="Победи Пожирателя эха. Протего и Древняя магия помогут!";
                Marker(RiftPos);checkpoint=RiftPos+Vector3.back*22;
                boss=Enemy.Spawn(EnemyKind.Boss,RiftPos+Vector3.up*.3f);
                boss.health.Init(2000);boss.targetable.displayName="Пожиратель эха";boss.goldDrop=2400;
                boss.Died+=_=>
                {
                    foreach(var e in Enemy.All.ToArray())if(e!=null&&!e.IsDead)e.Kill();
                    Dialogue.Show(()=>SetStep(Step.FinalReport),new Line("Пожиратель эха","Нет… голоса Хранителей… снова замолкают…"),
                        new Line(Fig,"Разлом закрыт. Возвращайся: теперь мы можем защитить Хогвартс надолго."));
                };break;
            case Step.FinalReport:
                title="Новый хранитель";objective="Вернись к Фигу и заверши расследование";
                talkTarget="fig";Marker(Npc("fig").transform.position);break;
        }
    }
    void MakeClue()
    {
        if(clue!=null)Destroy(clue);
        clue=Prims.Sphere(WorldBuilder.Dynamic,markerPos+Vector3.up*.8f,.45f,MatLib.Glow(new Color(.25f,.85f,1),2));
    }
    public bool InteractExtension()
    {
        if(InteractionHint()==null)return false;
        if(clue!=null)Destroy(clue);
        SFX.Play("ancient",.6f);
        if(step==Step.Archive)
            Dialogue.Show(()=>SetStep(Step.KitchenLetter),new Line("Хроника Хранителей","Осколки хранилища помнят страх своих владельцев. Собранные вместе, они способны обрести волю."),
                new Line(You,"Кто-то собирает осколки. Нужно проследить, как их вывозят из школы."));
        else if(step==Step.KitchenLetter)
            Dialogue.Show(()=>SetStep(Step.Convoy),new Line("Записка","Караван ждёт на восточной дороге. Отправьте осколки до заката. Исследователь в лесу знает последнюю печать."));
        else if(step==Step.Seals)
        {
            seals++;P.inventory.AddGold(150);
            if(seals<3){Marker(SealPos[seals]);MakeClue();}
            else Dialogue.Show(()=>SetStep(Step.Rift),new Line(Fig,"Школа защищена, но эхо уже обрело тело. Оно ждёт у северных руин. Останови его!"));
        }
        return true;
    }
    bool UpdateExtension()
    {
        if(!ExtensionActive)return false;
        if(step==Step.Convoy||step==Step.Rescue)
        {
            progress=$"Осталось врагов: {AliveCount()}";
            if(AliveCount()==0)
            {
                bool convoy=step==Step.Convoy;
                Dialogue.Show(()=>SetStep(convoy?Step.Rescue:Step.Seals),
                    new Line(convoy?You:"Исследователь",convoy?"Осколки отправлены к разлому. Но сначала нужно найти пленника.":"Печати в школе ослабли. Библиотека, башня и оранжерея — восстанови их, иначе разлом поглотит замок."));
            }
        }
        if(step==Step.Seals)progress=$"Печати: {seals}/3";
        return true;
    }
    bool TalkExtension(NPC npc)
    {
        if(!ExtensionActive||npc.id!=talkTarget)return false;
        if(step==Step.Aftermath)
            Dialogue.Show(()=>SetStep(Step.Archive),new Line(Fig,"Победа над Ранроком не уничтожила осколки хранилища. Кто-то похищает их."),
                new Line(Fig,"Начни с хроники на верхнем этаже библиотеки. Затем проверь письма на кухне: там видели незнакомую сову."));
        else if(step==Step.FinalReport)
            Dialogue.Show(()=>{SetStep(Step.Free);GameState.Set(GameMode.Victory);},new Line(Fig,"Ты спас не только хранилище, но и память Хранителей. Школа снова защищена."),
                new Line(Fig,"Теперь продолжай учёбу. Используй комнаты, путешествуй и помогай профессорам."));
        return true;
    }
}
