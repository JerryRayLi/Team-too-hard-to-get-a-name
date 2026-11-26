using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

[Serializable]
public sealed class JerryHelpTarget
{
    public Transform target;
    public Vector3 localOffset;
    public string caption;
    public Color color = new Color(1f,.83f,.40f,1f);
    public Vector3 WorldPosition => target != null ? target.TransformPoint(localOffset) : Vector3.zero;
}
[Serializable]
public sealed class JerryHelpStep
{
    public string title;
    [TextArea(2,5)] public string instructions;
    public JerryHelpTarget[] targets = new JerryHelpTarget[0];
}

// Optional, read-only walkthroughs for the four Jerry scenes. The only input
// consumers are UI buttons; no camera, physics, timer or player state is written.
public sealed class JerryLevelHelp : MonoBehaviour
{
    [Header("Offer help after either threshold")]
    [Min(1f)] public float stalledSeconds = 45f;
    [Min(1)] public int deathsBeforeHelp = 5;
    public PlayerRespawnJ player;
    public Button[] puzzleButtons = new Button[0];
    public JerryCheckpoint checkpoint;
    public Transform[] progressLandings = new Transform[0];
    public JerryHelpStep[] steps = new JerryHelpStep[0];

    private static string sessionScene;
    private static JerryHelpSession retained;
    private JerryHelpSession session;
    private Canvas canvas;
    private RectTransform canvasRect, panel;
    private UnityEngine.UI.Button helpButton, previousButton, nextButton;
    private Text helpLabel, heading, body, counter;
    private Font font;
    private Camera view;
    private float nextProgressPoll;
    private RectTransform[] markerRoots;
    private Text[] markerCaptions;
    private int displayedStep = -1;
    private int layoutStep = -1;
    private Vector2 layoutSize;
    private bool panelOnRight;
    private const float PanelHeight=378f;
    public bool HelpAvailable => session != null && session.Unlocked;
    public bool GuideOpen => session != null && session.GuideOpen;
    public static bool IsShowingIn(Scene scene) => retained != null && retained.Unlocked && retained.GuideOpen && sessionScene == scene.path;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() { retained=null;sessionScene=null; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded-=SceneChanged;
        SceneManager.sceneLoaded+=SceneChanged;
    }
    private static void SceneChanged(Scene scene,LoadSceneMode mode)
    {
        if(mode==LoadSceneMode.Single&&scene.path!=sessionScene)ResetSession();
    }
    private void Awake()
    {
        string n=gameObject.scene.name;
        if(n!="level_j1"&&n!="level_j1.5"&&n!="level_j2"&&n!="level_j3"){enabled=false;return;}
        if(retained==null||sessionScene!=gameObject.scene.path)
        {sessionScene=gameObject.scene.path;retained=new JerryHelpSession(0);}
        session=retained;
    }
    private void Start()
    {
        if(session==null||steps.Length==0)return;
        view=Camera.main;BuildUI();RefreshDisplay();
    }
    private void Update()
    {
        if(canvas==null||session==null)return;
        bool playing=Time.timeScale>0f;
        canvas.enabled=playing;
        if(!playing)return;
        int deaths=PlaySessionStats.CurrentLevelDeaths;
        if(Time.time>=nextProgressPoll){nextProgressPoll=Time.time+.25f;ObserveProgress(deaths);}
        session.Observe(player==null||player.IsRespawning?0f:Time.deltaTime,deaths,stalledSeconds,deathsBeforeHelp);
        RefreshDisplay();
    }
    private void LateUpdate(){if(canvas!=null&&canvas.enabled&&GuideOpen)LayoutMarkers();}
    private void ObserveProgress(int deaths)
    {
        if((session.ProgressFlags&1)==0&&FindFirstObjectByType<Corpse>()!=null)session.MarkProgress(1,deaths);
        for(int i=0;i<puzzleButtons.Length&&i<8;i++)
            if(puzzleButtons[i]!=null&&puzzleButtons[i].IsPressed)session.MarkProgress(1<<(i+1),deaths);
        if(checkpoint!=null&&checkpoint.IsSaved)session.MarkProgress(1<<10,deaths);
        if(player==null||player.IsRespawning)return;
        for(int i=0;i<progressLandings.Length&&i<8;i++)
            if(progressLandings[i]!=null&&Vector2.Distance(player.transform.position,progressLandings[i].position)<1.5f)
                session.MarkProgress(1<<(i+12),deaths);
    }
    private RectTransform Box(string name,Transform parent,Vector2 anchor,Vector2 pivot,Vector2 pos,Vector2 size)
    {
        var go=new GameObject(name,typeof(RectTransform));var r=(RectTransform)go.transform;r.SetParent(parent,false);r.anchorMin=r.anchorMax=anchor;r.pivot=pivot;r.anchoredPosition=pos;r.sizeDelta=size;return r;
    }
    private Text Label(string name,Transform parent,Vector2 anchor,Vector2 pivot,Vector2 pos,Vector2 size,int fontSize,string text,TextAnchor alignment)
    {
        var r=Box(name,parent,anchor,pivot,pos,size);var t=r.gameObject.AddComponent<Text>();t.font=font;t.fontSize=fontSize;t.color=new Color(.94f,.96f,.93f);t.text=text;t.alignment=alignment;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;t.raycastTarget=false;return t;
    }
    private UnityEngine.UI.Button MakeButton(string name,Transform parent,Vector2 anchor,Vector2 pivot,Vector2 pos,Vector2 size,string text,UnityEngine.Events.UnityAction action,out Text label)
    {
        var r=Box(name,parent,anchor,pivot,pos,size);var image=r.gameObject.AddComponent<Image>();image.color=new Color(.16f,.23f,.26f,.98f);var b=r.gameObject.AddComponent<UnityEngine.UI.Button>();b.targetGraphic=image;b.navigation=new Navigation{mode=Navigation.Mode.None};b.onClick.AddListener(()=>{action();if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(null);});label=Label("Label",r,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,size-new Vector2(12,4),23,text,TextAnchor.MiddleCenter);return b;
    }
    private void BuildUI()
    {
        font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var go=new GameObject("Jerry Walkthrough UI",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));go.transform.SetParent(transform,false);canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=80;canvasRect=(RectTransform)go.transform;
        var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
        helpButton=MakeButton("Need a hint",go.transform,Vector2.one,Vector2.one,new Vector2(-16,-174),new Vector2(224,46),"NEED A HINT?",ToggleGuide,out helpLabel);
        helpButton.GetComponent<Image>().color=new Color(.96f,.76f,.34f,1);helpLabel.color=new Color(.08f,.12f,.14f);helpLabel.fontStyle=FontStyle.Bold;
        panel=Box("Walkthrough",go.transform,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,12),new Vector2(440,PanelHeight));var bg=panel.gameObject.AddComponent<Image>();bg.color=new Color(.025f,.055f,.07f,.96f);bg.raycastTarget=false;
        heading=Label("Step heading",panel,new Vector2(0,1),new Vector2(0,1),new Vector2(18,-12),new Vector2(316,68),26,"",TextAnchor.UpperLeft);heading.fontStyle=FontStyle.Bold;heading.color=new Color(1,.86f,.48f);
        body=Label("Instructions",panel,new Vector2(0,1),new Vector2(0,1),new Vector2(18,-92),new Vector2(404,210),23,"",TextAnchor.UpperLeft);
        counter=Label("Step count",panel,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,13),new Vector2(136,40),20,"",TextAnchor.MiddleCenter);
        Text ignored;
        previousButton=MakeButton("Previous step",panel,Vector2.zero,Vector2.zero,new Vector2(18,13),new Vector2(104,40),"BACK",()=>ChangeStep(-1),out ignored);
        nextButton=MakeButton("Next step",panel,new Vector2(1,0),new Vector2(1,0),new Vector2(-18,13),new Vector2(104,40),"NEXT",()=>ChangeStep(1),out ignored);
        MakeButton("Close guide",panel,Vector2.one,Vector2.one,new Vector2(-12,-10),new Vector2(76,36),"HIDE",ToggleGuide,out ignored);
        markerRoots=new RectTransform[3];markerCaptions=new Text[3];
        for(int i=0;i<3;i++)
        {
            var r=Box("Target marker "+(i+1),go.transform,Vector2.zero,new Vector2(.5f,.5f),Vector2.zero,new Vector2(80,80));var graphic=r.gameObject.AddComponent<JerryHelpMarkerGraphic>();graphic.raycastTarget=false;
            var label=Box("Target label",r,new Vector2(.5f,.5f),new Vector2(.5f,0),new Vector2(0,64),new Vector2(256,36));var image=label.gameObject.AddComponent<Image>();image.color=new Color(.025f,.05f,.06f,.94f);image.raycastTarget=false;
            markerCaptions[i]=Label("Caption",label,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(248,34),20,"",TextAnchor.MiddleCenter);markerCaptions[i].fontStyle=FontStyle.Bold;markerRoots[i]=r;
        }
        // Reuse the scene's UI input system; only create one when none exists.
        if(EventSystem.current==null&&FindFirstObjectByType<EventSystem>()==null)
        {
            var events=new GameObject("Jerry Help UI Input",typeof(EventSystem),typeof(InputSystemUIInputModule));events.transform.SetParent(transform,false);events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
    }
    public void ToggleGuide()
    {
        if(!HelpAvailable)return;
        session.GuideOpen=!session.GuideOpen;RefreshDisplay();
    }
    public void ChangeStep(int direction)
    {
        if(!GuideOpen)return;session.Step=Mathf.Clamp(session.Step+direction,0,steps.Length-1);RefreshDisplay();
    }
    private void RefreshDisplay()
    {
        if(canvas==null)return;
        helpButton.gameObject.SetActive(HelpAvailable);panel.gameObject.SetActive(HelpAvailable&&GuideOpen);
        helpLabel.text=GuideOpen?"HIDE GUIDE":"NEED A HINT?";
        // Same scaled menu/skip stack, including the skip button's 124px minimum.
        float top=Mathf.Max(142f,124f/Mathf.Max(.1f,canvas.scaleFactor)+44f)+8f;
        ((RectTransform)helpButton.transform).anchoredPosition=new Vector2(-16,-top);
        if(!GuideOpen){foreach(var r in markerRoots)r.gameObject.SetActive(false);return;}
        session.Step=Mathf.Clamp(session.Step,0,steps.Length-1);var step=steps[session.Step];
        if(displayedStep!=session.Step){displayedStep=session.Step;heading.text=step.title;body.text=step.instructions;counter.text="STEP "+(session.Step+1)+" / "+steps.Length;}
        previousButton.interactable=session.Step>0;nextButton.interactable=session.Step<steps.Length-1;
        Canvas.ForceUpdateCanvases();LayoutMarkers();
    }
    private void LayoutMarkers()
    {
        if(view==null)view=Camera.main;if(view==null||steps.Length==0)return;
        var step=steps[session.Step];Vector2 size=canvasRect.rect.size;if(size.x<1||size.y<1)return;
        float width=Mathf.Min(440,size.x-32);panel.sizeDelta=new Vector2(width,PanelHeight);
        heading.rectTransform.sizeDelta=new Vector2(width-112,68);body.rectTransform.sizeDelta=new Vector2(width-36,210);
        float panelBottom=Mathf.Max(12,size.y-232-PanelHeight);
        Rect left=new Rect(16,panelBottom,width,PanelHeight),right=new Rect(size.x-width-16,panelBottom,width,PanelHeight);
        if(layoutStep!=session.Step||layoutSize!=size)
        {layoutStep=session.Step;layoutSize=size;panelOnRight=PanelCost(right,step,size)<PanelCost(left,step,size);}
        Rect chosen=panelOnRight?right:left;
        panel.anchoredPosition=new Vector2(chosen.center.x-size.x*.5f,panelBottom);
        for(int i=0;i<markerRoots.Length;i++)
        {
            bool show=i<step.targets.Length&&step.targets[i].target!=null;markerRoots[i].gameObject.SetActive(show);if(!show)continue;
            var target=step.targets[i];Vector3 v=view.WorldToViewportPoint(target.WorldPosition);if(v.z<=0){markerRoots[i].gameObject.SetActive(false);continue;}
            float x=Mathf.Clamp(v.x*size.x,28,size.x-28),y=Mathf.Clamp(v.y*size.y,28,size.y-28);
            // The ring stays on the actual target. Only its label moves to avoid HUDs.
            markerRoots[i].anchoredPosition=new Vector2(x,y);var graphic=markerRoots[i].GetComponent<JerryHelpMarkerGraphic>();graphic.color=target.color;
            bool below=y>size.y-285;
            var label=(RectTransform)markerCaptions[i].transform.parent;
            float labelY=below?-100:64;
            float labelX=Mathf.Clamp(x,142,size.x-142)-x;
            var labelRect=new Rect(x+labelX-128,y+labelY,256,36);
            if(labelRect.Overlaps(chosen))
                labelX=Mathf.Clamp(chosen.center.x<size.x*.5f?chosen.xMax+136:chosen.xMin-136,142,size.x-142)-x;
            for(int j=0;j<i;j++)
            {
                if(!markerRoots[j].gameObject.activeSelf)continue;
                var otherLabel=(RectTransform)markerCaptions[j].transform.parent;
                Vector2 other=markerRoots[j].anchoredPosition+otherLabel.anchoredPosition;
                if(Mathf.Abs(other.x-(x+labelX))<260&&Mathf.Abs(other.y-(y+labelY))<42)
                    labelY+=below?-44:44;
            }
            label.anchoredPosition=new Vector2(labelX,Mathf.Clamp(y+labelY,8,size.y-44)-y);
            Vector2 labelCenter=label.anchoredPosition+Vector2.up*18;
            if(graphic.labelCenter!=labelCenter){graphic.labelCenter=labelCenter;graphic.SetVerticesDirty();}
            markerCaptions[i].text=(i+1)+". "+target.caption+(v.x<0||v.x>1||v.y<0||v.y>1?" >":"");markerCaptions[i].color=target.color;
        }
    }
    private float PanelCost(Rect rect,JerryHelpStep step,Vector2 size)
    {
        float cost=0;
        foreach(var target in step.targets)
        {
            if(target.target==null)continue;
            var p=view.WorldToViewportPoint(target.WorldPosition);
            if(rect.Overlaps(new Rect(p.x*size.x-32,p.y*size.y-32,64,64)))cost+=10;
        }
        if(player!=null)
        {
            var p=view.WorldToViewportPoint(player.transform.position);
            if(rect.Contains(new Vector2(p.x*size.x,p.y*size.y)))cost+=1;
        }
        return cost;
    }
    private void OnDisable(){if(canvas!=null)canvas.enabled=false;}
}
