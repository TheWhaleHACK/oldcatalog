using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "2db36687352535b425e14e447cb2a840e3b2ade3")]
public class IntersectionFinder : Component
{
    public PlayerPersecutor player;
    List<Node> nodes = new List<Node>();
    [ShowInEditor] private NodeAnimationPlayback nodeAnimationPlayback;
    [ShowInEditor] private TreeGui treeGui;

    [ShowInEditor] private MakeOtherObjectsTransparent makeOtherObjectsTransparent = null;
    [ShowInEditor] private bool debug = false;
    [ShowInEditor] private Node plane;
    private bool intersecting = false;
    [ShowInEditor] private Unigine.Object oldObj;
    [ShowInEditor] private bool isHighlighted = false;

    private float rotationSpeed = 0.5f;
    private ivec2 previousMousePosition;
    private Unigine.Object lastSelected = null;
    private float distanceFromObject = 3.0f;
    private float heightOffset = 1.0f;
    private double lastClickTime = 0.0;
    private const double doubleClickTime = 0.3f;
    public bool doubleClicked = false;

    // Публичное свойство для доступа к lastSelected
    public Unigine.Object LastSelected
    {
        get => lastSelected;
        set => lastSelected = value;
    }

    private void Init()
    {
        player = node as PlayerPersecutor;
        treeGui = FindComponentInWorld<TreeGui>();
        Input.MouseHandle = Input.MOUSE_HANDLE.SOFT;
        Input.EventMouseWheel.Connect(mousewheel_event_handler);
        previousMousePosition = Input.MousePosition;
    }

    void mousewheel_event_handler(int delta_vertical)
    {
        Log.MessageLine(delta_vertical);
        if (delta_vertical > 0)
        {
            player.Distance -= 1;
            player.MaxDistance -= 1;
        }
        else if (delta_vertical < 0)
        {
            player.Distance += 1;
            player.MaxDistance += 1;
        }
    }

    private void Update()
    {
        //Log.MessageLine(doubleClicked);
        ivec2 mouse = Input.MousePosition;
        dvec3 p0 = player.Position;
        dvec3 p1 = p0 + new dvec3(player.GetDirectionFromMainWindow(mouse.x, mouse.y)) * 100;

        WorldIntersection intersection = new WorldIntersection();
        Unigine.Object obj = World.GetIntersection(p0, p1, 1, intersection);

        if (obj)
        {
            if (makeOtherObjectsTransparent.IfObjInBuild(obj))
            {
                if (debug)
                {
                    Log.MessageLine(obj.Name);
                }
                IntersectedObject(obj);
                oldObj = obj;
            }
        }

        if (Input.IsMouseButtonDown(Input.MOUSE_BUTTON.LEFT) && Input.IsKeyPressed(Input.KEY.LEFT_SHIFT))
        {
            if (obj != null)
            {
                lastSelected = obj;
                MoveCameraToObject();
            }
        }

        if (Input.IsMouseButtonDown(Input.MOUSE_BUTTON.LEFT))
        {
            if (Game.Time - lastClickTime < doubleClickTime && obj != null)
            {
                Log.MessageLine("Double Click");
                makeOtherObjectsTransparent.IsolatePart(obj);
                player.Target = makeOtherObjectsTransparent.chosenSubBuild;
                doubleClicked = true;
            }
            else
            {
                if (obj != null)
                {
                    Log.MessageLine("Click");
                    makeOtherObjectsTransparent.IsolatePart(obj);
                    player.Target = makeOtherObjectsTransparent.chosenSubBuild;
                    doubleClicked = false;
                }

            }
            lastClickTime = Game.Time;
        }

        if (Input.IsKeyPressed(Input.KEY.SPACE) && makeOtherObjectsTransparent.HierarchyLevelCount > 1)
        {
            lastSelected = null;
        }
        

        if (Input.IsKeyDown(Input.KEY.C))
        {
            nodeAnimationPlayback.Play();
        }
    }

    private void IntersectedObject(Object obj)
    {
		makeOtherObjectsTransparent.Highliht(obj, 1);
		isHighlighted = true;
		if (oldObj != obj)
		{
			makeOtherObjectsTransparent.Highliht(oldObj, 0);
		}
    }

    private void RotateObject(ivec2 delta)
    {
        if (lastSelected == null)
        {
            Log.Error("Объект не найден\n");
            return;
        }
        float deltaYaw = -delta.x * rotationSpeed;
        float deltaPitch = -delta.y * rotationSpeed;
        lastSelected.Rotate(0, 0, deltaYaw);
        lastSelected.Rotate(deltaPitch, 0, 0);
    }

    private void MoveCameraToObject()
    {
        if (lastSelected == null) return;

        dvec3 targetPosition = lastSelected.Position;
        dvec3 direction = targetPosition - player.Position;
        direction.Normalize();

        dvec3 cameraPosition = targetPosition - direction * distanceFromObject;
        cameraPosition.z += heightOffset;

        player.Position = (vec3)cameraPosition;
        player.SetWorldDirection((vec3)(targetPosition - cameraPosition), vec3.UP);
    }

    public bool CheckIfHasMaterials(Object obj)
    {
        return obj is ObjectMeshStatic;
    }
}