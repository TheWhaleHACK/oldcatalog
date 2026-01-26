using System.Collections.Generic;
using Unigine;
using System;

[Component(PropertyGuid = "693f9dc76e32250902181146dc4488be41095613")]
public class MakeOtherObjectsTransparent : Component
{
    public Node build;
    [ShowInEditor] private Material transparentMaterial;
    [ShowInEditor] private Node persecutorTarget;
    private IntersectionFinder intersectionFinder;
    private test test;
    private List<Node> children = new List<Node>();
    public List<Node> allNodes = new List<Node>();
    private TreeGui treeGui;
    public Node chosenSubBuild;
    private Stack<Node> hierarchyStack = new Stack<Node>();

    public int HierarchyLevelCount => hierarchyStack.Count;

    private void Init()
    {
        treeGui = FindComponentInWorld<TreeGui>();
        intersectionFinder = FindComponentInWorld<IntersectionFinder>();
        test = FindComponentInWorld<test>();
    }

    private void Update()
    {
        //конструкция для логирования списка деталей в hierarchyStack
        string st = "";
        foreach (Node e1 in hierarchyStack)
        {
            st += e1.Name;
            st += " ";
        }
        if (Input.IsKeyDown(Input.KEY.BACKSPACE))
        {
            BackspaceMode();
        }
        if (Input.IsKeyPressed(Input.KEY.ESC))
        {
            EscapeMode();
        }
        if (Input.IsKeyPressed(Input.KEY.SPACE) && hierarchyStack.Count > 1)
        {
            ExitIsolation();
            treeGui.SelectItemFromModel(allNodes.IndexOf(chosenSubBuild));
            treeGui.UnfoldHierarhy(allNodes.IndexOf(chosenSubBuild));
        }
    }


    public void BackspaceMode()
    {
        if (hierarchyStack.Count >= 1)
        {
            treeGui.doubleClicked = false;
            Node previousLevel = hierarchyStack.Peek();
            Log.MessageLine(previousLevel.Name);
            int ddd = allNodes.IndexOf(previousLevel);
            ChooseSubBuild(allNodes.IndexOf(previousLevel)); // баг сто проц в этой строчке
            treeGui.SelectItemFromModel(allNodes.IndexOf(previousLevel));
            if (hierarchyStack.Count > 1)
                hierarchyStack.Pop();
        }
    }

    public void EscapeMode()
    {
        hierarchyStack.Clear();
        hierarchyStack.Push(build);
        ChooseSubBuild(0);
        treeGui.SelectItemFromModel(0); // Выбираем корень в дереве
        //MakeAllOpaque();
        intersectionFinder.doubleClicked = false;
        treeGui.doubleClicked = false;
    }

    public void UntransparentAll()
    {
        hierarchyStack.Clear();
        hierarchyStack.Push(build);
        ChooseSubBuild(0);
        treeGui.SelectItemFromModel(0); // Выбираем корень в дереве
    }

    public void MakeOtherTransparent(Unigine.Object obj) // при backspace не это
    {
        foreach (Node node in allNodes)
        {
            if (node is not NodeDummy && node != obj)
            {
                Unigine.Object nodeObj = node as Unigine.Object;
                if (nodeObj != null)
                {
                    for (int i = 0; i < nodeObj.NumSurfaces; i++)
                    {
                        if (!intersectionFinder.doubleClicked && !treeGui.doubleClicked)// вот тут то и ошибка. Надо еще сделать так, чтобы да
                            nodeObj.SetMaterial(transparentMaterial, i);
                        else
                            nodeObj.Enabled = false;
                    }
                }
            }
        }
        if (obj != null && obj.GetComponent<Part>())
        {
            for (int i = 0; i < obj.NumSurfaces; i++)
            {
                if (!intersectionFinder.doubleClicked && !treeGui.doubleClicked)
                    obj.SetMaterial(obj.GetComponent<Part>().materials[i], i);
                else
                    obj.Enabled = true;
            }
        }
    }

    public void MakeAllOpaque()
    {
        foreach (Node node in allNodes)
        {
            if (node is not NodeDummy)
            {
                Unigine.Object nodeObj = node as Unigine.Object;
                if (nodeObj != null && nodeObj.GetComponent<Part>())
                {
                    for (int i = 0; i < nodeObj.NumSurfaces; i++)
                    {
                        if (!intersectionFinder.doubleClicked && !treeGui.doubleClicked)//делает непрозрачными всех дочерних
                            nodeObj.SetMaterial(nodeObj.GetComponent<Part>().materials[i], i);
                        else
                            nodeObj.Enabled = true;
                    }
                }
            }
        }
    }

    public void Highlight(Unigine.Object obj, int index)
    {
        // Убрано
    }

    public bool IfObjInBuild(Unigine.Object obj)
    {
        return allNodes.Contains(obj);
    }

    public Node FindItem(int index)
    {
        if (index > allNodes.Count - 1 || index < 0)
            return allNodes[0];
        return allNodes[index];
    }

    public void ChooseSubBuild(int index)
    {
        if (index != 0)
        {
            Node item = FindItem(index);
            Unigine.Object uo = item as Unigine.Object;
            if (item.NumChildren > 0)
            {
                Node nullItrem = FindItem(0);
                MakeAllOpaque();
                DisableAllParts();//вместо нее надо сделать строчку, которая делает прозрачными
                item.Enabled = true;
                for (int i = 0; i < item.NumChildren; i++)
                {
                    item.GetChild(i).Enabled = true;

                }

                // for (int i = 0; i < nullItrem.NumChildren; i++)
                // {
                //     nullItrem.GetChild(i).Enabled = true;
                //     Unigine.Object dd = nullItrem.GetChild(i) as Unigine.Object;
                //     if (nullItrem.GetChild(i) != item)
                //     {
                //         for (int j = 0; j < dd.NumSurfaces; j++)
                //         {
                //             dd.SetMaterial(transparentMaterial, j);
                //         }
                //     }
                // }//сработало, только теперь если я нажимаю backspace, у меня не телепортируется на родителя, и проблема менно в этом форе
                chosenSubBuild = item;

                //здесь сделать MakeOtherTransparent(Если надо будет)
            }
            else
            {
                MakeAllOpaque();
                DisableAllParts();
                item.Enabled = true;
                chosenSubBuild = item;
            }
        }
        else
        {
            MakeAllOpaque();
            for (int i = 0; i < build.NumChildren; i++)
            {
                build.GetChild(i).Enabled = true;
            }
            chosenSubBuild = persecutorTarget;
        }
    }

    public void DisableAllParts()
    {
        for (int i = 0; i < build.NumChildren; i++)
        {
            build.GetChild(i).Enabled = false;
        }
    }

    public void InitBuild()
    {
        if (build)
        {
            build.GetHierarchy(allNodes);
            foreach (Node node in allNodes)
            {
                Log.MessageLine($"Node in hierarchy: {node.Name}, Type: {node.Type}, NumChildren: {node.NumChildren}");
            }
            if (treeGui == null)
            {
                Log.Error("TreeGui not found in world!");
            }
            else
            {
                if (!intersectionFinder.doubleClicked && !treeGui.doubleClicked)
                    treeGui.CreateTree(allNodes);
            }
            hierarchyStack.Clear();
            hierarchyStack.Push(build);
            chosenSubBuild = build;
        }
        else
        {
            Log.Error("Build node is not assigned!");
        }
    }

    public void IsolatePart(Unigine.Object obj)
    {
        if (!allNodes.Contains(obj) || obj == null)
        {
            Log.Message("IsolatePart: Invalid object or not in allNodes");
            return;
        }

        try
        {
            Log.Message($"IsolatePart: Processing object {obj.Name}, Type: {obj.Type}, NumChildren: {obj.NumChildren}");

            // Если у объекта есть дети, изолируем их
            if (obj.NumChildren > 0)
            {
                List<Unigine.Object> childObjects = new List<Unigine.Object>();
                CollectChildObjects(obj, childObjects);
                Log.Message($"Found {childObjects.Count} Unigine.Object children");

                if (childObjects.Count > 0)
                {
                    Log.Message($"Isolating {childObjects.Count} child objects");
                    IsolateParts(childObjects);
                    chosenSubBuild = obj;

                    // Синхронизация с деревом
                    int objIndex = allNodes.IndexOf(obj);
                    if (treeGui != null && objIndex >= 0)
                    {
                        treeGui.SelectItemFromModel(objIndex);
                        treeGui.UnfoldHierarhy(objIndex);
                    }
                }
                else
                {
                    Log.Message("No Unigine.Object children found, isolating parent object");
                    MakeOtherTransparent(obj);
                    chosenSubBuild = obj;

                    // Синхронизация с деревом
                    int objIndex = allNodes.IndexOf(obj);
                    if (treeGui != null && objIndex >= 0)
                    {
                        treeGui.SelectItemFromModel(objIndex);
                        treeGui.UnfoldHierarhy(objIndex);
                    }
                }
            }
            else
            {
                // Если у объекта нет детей, изолируем его самого
                Log.Message($"Isolating object without children: {obj.Name}");
                MakeOtherTransparent(obj);
                chosenSubBuild = obj;

                // Синхронизация с деревом
                int objIndex = allNodes.IndexOf(obj);
                if (treeGui != null && objIndex >= 0)
                {
                    treeGui.SelectItemFromModel(objIndex);
                    if (string.IsNullOrWhiteSpace(treeGui.searchField.Text))
                        treeGui.UnfoldHierarhy(objIndex);//вот на этом месте краш Я ЕГО ЗАКОММЕНТИЛ И ВСЕ ЗАРАБОТАЛО АХАХХАХАХАХА
                }

                if (hierarchyStack.Count == 0 || !hierarchyStack.Contains(obj.Parent))
                {
                    hierarchyStack.Push(obj.Parent);
                }
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error in IsolatePart: {e.Message}\n{e.StackTrace}");
        }
    }

    public void IsolateParts(List<Unigine.Object> objects)
    {
        if (objects == null || objects.Count == 0) // еще здесь точка остановы наверное
        {
            Log.Message("IsolateParts: Invalid or empty object list");
            return;
        }

        try
        {
            // Если правильно то сюда заходит
            Log.Message($"IsolateParts: Processing {objects.Count} objects");
            MakeAllOpaque();

            // Делаем переданные объекты непрозрачными
            foreach (Unigine.Object obj in objects)
            {
                if (obj != null && obj.GetComponent<Part>())
                {
                    for (int i = 0; i < obj.NumSurfaces; i++)
                    {
                        if (!intersectionFinder.doubleClicked && !treeGui.doubleClicked)
                            obj.SetMaterial(obj.GetComponent<Part>().materials[i], i);
                        else
                            obj.Enabled = true;
                        Log.Message($"Set material for object: {obj.Name}");
                    }
                }

            }

            // Делаем все остальные объекты прозрачными
            foreach (Node node in allNodes)
            {
                if (node is Unigine.Object nodeObj && !objects.Contains(nodeObj))
                {
                    for (int i = 0; i < nodeObj.NumSurfaces; i++)
                    {
                        if (!intersectionFinder.doubleClicked && !treeGui.doubleClicked)
                            nodeObj.SetMaterial(transparentMaterial, i);
                        else
                            nodeObj.Enabled = false;
                        Log.Message($"Set transparent material for object: {nodeObj.Name}");
                    }
                }
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error in IsolateParts: {e.Message}\n{e.StackTrace}");
        }
    }

    public void ExitIsolation()
    {
        if (hierarchyStack.Count > 1)
        {
            hierarchyStack.Pop();
            Node previousLevel = hierarchyStack.Peek();
            ChooseSubBuild(allNodes.IndexOf(previousLevel));
        }
    }

    private void CollectChildObjects(Node node, List<Unigine.Object> childObjects)
    {
        for (int i = 0; i < node.NumChildren; i++)
        {
            Node child = node.GetChild(i);
            if (child is Unigine.Object obj)
            {
                childObjects.Add(obj);
                Log.Message($"Collected child object: {obj.Name}, Type: {obj.Type}");
            }
            CollectChildObjects(child, childObjects);
        }
    }

	public void Highliht(Unigine.Object obj, int index)
	{
		if (allNodes.Contains(obj))
		{
			for (int i = 0; i < obj.NumSurfaces; i++)
				obj.SetMaterialState("auxiliary", index, i);
		}
	}    
}