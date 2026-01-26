using System.Collections;
using System.Collections.Generic;
using Unigine;
using UnigineApp.data.Code;
using System.IO;

[Component(PropertyGuid = "28ae94f72c55431139f227742831b0be2e258d18")]
public class test : Component
{
    [ShowInEditor] private MakeOtherObjectsTransparent makeOtherObjectsTransparent = null;
    [ShowInEditor] private TreeGui treeGui = null;
    [ShowInEditor] private IntersectionFinder intersectionFinder = null;
    [ShowInEditor] private TreeWithLoadedModels treeWithLoaded;
    [ShowInEditor] private string importFolder = @"D:\Почта\Тест КОМПАС\";
    public List<Node> AddedModels = new List<Node>();

    private Gui gui;
    public bool isGuiVisible = true;
    private WidgetWindow window;
    private WidgetListBox listBox;
    private WidgetButton importButton,cancelButton;
    public Node lastImportedNode = null; // Храним последнюю импортированную ноду

    void Init()
    {
        // Загрузка плагинов
        Unigine.Console.Run("plugin_load KompasUnigineKompasUnigineImporter");
        Unigine.Console.Run("plugin_load UnigineFbxImporter");
        Unigine.Console.Run("plugin_load UnigineGeodetics");
        Unigine.Console.Run("plugin_load UnigineCadImporter");
        Unigine.Console.Run("plugin_load UnigineGLTFImporter");
        Unigine.Console.Run("plugin_load UnigineFbxExporter");

        gui = Gui.GetCurrent();
        CreateGui();
    }

    void CreateGui()
    {
        window = new WidgetWindow(gui, "Выбор модели для импорта");
        window.Height = 400;
        window.Width = 300;


        window.SetPosition((gui.Width - window.Width) / 2, (gui.Height - window.Height) / 2);
        window.Sizeable = true;

        listBox = new WidgetListBox(gui);
        listBox.Height = 350;
        listBox.Width = 200;
        listBox.SetPosition(25, 30);
        window.AddChild(listBox);

        //горизонтальный контейнер для кнопок
        WidgetHBox buttonLayout = new WidgetHBox(gui);
        buttonLayout.SetSpace(10,10); //расстояние между кнопками
        buttonLayout.SetPadding(10, 10, 10, 10);     

        importButton = new WidgetButton(gui, "Импортировать");
        importButton.Width = 100;
        importButton.SetPosition(150, 240);
        importButton.EventClicked.Connect(OnImportClicked);

        buttonLayout.AddChild(importButton);

        cancelButton = new WidgetButton(gui, "Отмена");
        cancelButton.Width = 100;
        cancelButton.EventClicked.Connect(OnCancelButtonClicked); 

        buttonLayout.AddChild(cancelButton);

        window.AddChild(buttonLayout, Gui.ALIGN_BOTTOM);

        PopulateFileList();

        gui.AddChild(window, Gui.ALIGN_OVERLAP | Gui.ALIGN_CENTER | Gui.ALIGN_FIXED);
    }

    void PopulateFileList()
    {
        if (!Directory.Exists(importFolder))
        {
            Log.Error($"Папка {importFolder} не найдена!\n");
            return;
        }

        // Получаем список файлов с поддерживаемыми расширениями
        string[] extensions = { "*.fbx", "*.stp", "*.ar" }; // Поддерживаемые форматы
        List<string> files = new List<string>();
        foreach (string ext in extensions)
        {
            files.AddRange(Directory.GetFiles(importFolder, ext, SearchOption.TopDirectoryOnly));
        }

        listBox.Clear();
        foreach (string file in files)
        {
            listBox.AddItem(System.IO.Path.GetFileName(file));
        }
    }

    void OnImportClicked()
    {
        treeWithLoaded.importButton.Enabled = true;
        treeWithLoaded.loadButton.Enabled = true;
        treeWithLoaded.clearButton.Enabled = true;
        treeWithLoaded.deleteButton.Enabled = true;    
        // Проверяем, выбран ли элемент
        int selectedIndex = listBox.CurrentItem;
        if (selectedIndex == -1)
        {
            Log.Warning("Выберите модель для импорта!\n");
            treeWithLoaded.importButton.Enabled = false;
            treeWithLoaded.loadButton.Enabled = false;
            treeWithLoaded.clearButton.Enabled = false;
            treeWithLoaded.deleteButton.Enabled = false;    
            return;
        }       

        // Получаем путь к выбранному файлу
        string selectedFile = System.IO.Path.Combine(importFolder, listBox.GetItemText(selectedIndex));
        
        string modelName = System.IO.Path.GetFileNameWithoutExtension(selectedFile); // например: "kondey"
        // Проверяем, была ли модель уже импортирована
        if (treeWithLoaded.ImportedModels.Contains(modelName))
        {
            Log.Warning($"Модель {modelName} уже была импортирована!\n");
            return; // Прерываем выполнение — не делаем import
        }
        // Удаляем предыдущую модель, если она существует
        RemoveLastImportedNode();
        // Импортируем модель
        Import_New import_New = new Import_New();
        //здесь он ее импортирует. Перед следующей строчкой нужен ИФ(если уже импортирована то не импортировать)
        Node myNode = import_New.import(selectedFile);

        if (!AddedModels.Contains(myNode))
            AddedModels.Add(myNode);

        if (myNode != null)
        {
            AfterImport(myNode);
        }
        else
        {
            Log.Error($"Не удалось импортировать модель: {selectedFile}\n");
        }
        intersectionFinder.player.Target = myNode; //
    }

    public void AfterImport(Node myNode)
    {
            myNode.WorldPosition = new dvec3(0, 0, 2);
            lastImportedNode = myNode; // Сохраняем ссылку на новую модель
            SetMaterialToNode(myNode);

            // Настройка makeOtherObjectsTransparent

            if (makeOtherObjectsTransparent != null)
            {
                makeOtherObjectsTransparent.build = myNode;
                makeOtherObjectsTransparent.InitBuild();
            }
            else
            {
                Log.Error("makeOtherObjectsTransparent не задан!\n");
            }
            // Обновление дерева в TreeGui
            if (treeGui != null)
            {
                // Собираем список нод для дерева
                List<Node> treeNodes = new List<Node>();
                CollectNodes(myNode, treeNodes);
                treeGui.CreateTree(treeNodes); // Пересоздаем дерево
                Log.Message("Дерево обновлено через GUI для ноды: {0}\n", myNode.Name);
            }
            else
            {
                Log.Error("TreeGui не задан!\n");
            }

            // Скрываем GUI после импорта
            if (!treeWithLoaded.ImportedModels.Contains(myNode.Name))
            {
                treeWithLoaded.PopulateFileList(myNode);           
            }

            ToggleGui(false);
    }


    void Update()
    {
        window.SetPosition((gui.Width - window.Width) / 2, (gui.Height - window.Height) / 2);

    }

    public void ToggleGui(bool visible)
    {
        isGuiVisible = visible;
        if (isGuiVisible)
        {
            gui.AddChild(window, Gui.ALIGN_OVERLAP | Gui.ALIGN_CENTER);
            PopulateFileList(); // Обновляем список файлов
        }
        else
        {
            gui.RemoveChild(window);
        }
    }

    private void CollectNodes(Node node, List<Node> nodes)
    {
        nodes.Add(node);
        for (int i = 0; i < node.NumChildren; i++)
        {
            CollectNodes(node.GetChild(i), nodes);
        }
    }

    private void SetMaterialToNode(Node myNodeT)
    {
        for (var i = 0; i < myNodeT.NumChildren; i++)
        {
            myNodeT.GetChild(i).AddComponent<Part>();
            SetMaterialToNode(myNodeT.GetChild(i));
        }

        var gameObject = myNodeT as Object;
        if (gameObject == null) return;

        for (var j = 0; j < gameObject.NumSurfaces; j++)
        {
            gameObject.SetIntersection(true, j);
        }
    }

    public void RemoveLastImportedNode()
    {
        if (lastImportedNode != null)
        {
            lastImportedNode.Enabled = false;
            lastImportedNode = null;
            Log.Message("Предыдущая импортированная модель скрыта\n");
        }
    }

    void OnCancelButtonClicked(Widget widget)
    {
        ToggleGui(!isGuiVisible);
        treeWithLoaded.importButton.Enabled = true;
        treeWithLoaded.loadButton.Enabled = true;
        treeWithLoaded.clearButton.Enabled = true;        
        treeWithLoaded.deleteButton.Enabled = true;        
    }

    void Shutdown()
    {
        // Очистка GUI при завершении
        if (window != null)
        {
            gui.RemoveChild(window);
            window.DeleteLater();
        }
    }
}