using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "9ba489847ba3122815d7c772baf33eb05f5740ca")]
public class OpenNextWindow : Component
{
    private WorldTrigger trigger;
    [ShowInEditor]
    private ObjectMeshStatic thisScreen;
    [ShowInEditor]
    private ObjectMeshStatic nextScreen;

    // Добавим переменную для отслеживания состояния
    private bool isCurrentScreenActive = false;

    void Init()
    {
        // Предположим, что при инициализации экран активен
        isCurrentScreenActive = true;

        trigger = node as WorldTrigger;
        if (trigger != null)
        {
            trigger.EventEnter.Connect(OnChangeScreen);
        }
        else
        {
             Log.Warning("OpenNextWindow: Trigger is not a WorldTrigger or is null on node {0}\n", node.Name);
        }
    }

    private void OnChangeScreen()
    {
        // Проверяем, является ли этот компонент ответственным за активный экран
        if (!isCurrentScreenActive)
        {
            // Если нет, игнорируем событие
            return;
        }

        Log.Message("OpenNextWindow: Trigger '{0}' activated for screen '{1}'. Switching to '{2}'.\n",
                    node.Name, thisScreen != null ? thisScreen.Name : "null", nextScreen != null ? nextScreen.Name : "null");

        // Отключаем текущий экран и его триггер
        if (thisScreen != null)
        {
            thisScreen.Enabled = false;
            // Отключаем детей текущего экрана
            for(int i = 0; i < thisScreen.NumChildren; i++)
            {
                var child = thisScreen.GetChild(i);
                // Убедитесь, что дочерний элемент не является самим триггером,
                // если он не должен быть отключен как часть "внешнего" объекта экрана.
                // Обычно триггеры находятся отдельно, но если он действительно дочерний - возможно, его нужно обрабатывать отдельно.
                // Для простоты предположим, что дети - это UI/Visuals, а не сами триггеры.
                // Если триггер находится внутри thisScreen, и вы хотите отключить его через Enabled родителя - этого может быть достаточно.
                // Но если триггер отдельно, то его нужно отключать явно здесь:
                if (child == trigger && trigger != null) continue; // Пропускаем сам триггер, если он дочерний и мы его отключаем ниже
                child.Enabled = false;
            }
        }

        // Отключаем сам триггер, чтобы он не реагировал больше
        if (trigger != null)
        {
            trigger.Enabled = false; // Это отключает физический триггер
        }

        // Помечаем, что этот компонент больше не активен
        isCurrentScreenActive = false;

        // Включаем следующий экран
        if (nextScreen != null)
        {
            nextScreen.Enabled = true;
            for(int i = 0; i < nextScreen.NumChildren; i++)
            {
                 nextScreen.GetChild(i).Enabled = true;
            }
            // Логично было бы проверить, есть ли на следующем экране триггеры,
            // которые должны стать активными, но это зависит от вашей архитектуры.
            // Если следующий экран имеет свои компоненты OpenNextWindow, они будут ждать своего триггера.
        }
    }

    // Если нужно будет вернуться к этому экрану позже, можно будет добавить метод Reset или Enable
    public void ActivateScreen()
    {
         if (thisScreen != null)
         {
             thisScreen.Enabled = true;
             for(int i = 0; i < thisScreen.NumChildren; i++)
             {
                  thisScreen.GetChild(i).Enabled = true;
             }
             if (trigger != null)
             {
                 trigger.Enabled = true; // Включаем триггер снова
             }
             isCurrentScreenActive = true; // Теперь он снова активен
         }
    }


    void Update()
    {
        // foreach(var screen in thisScreen)
        //     Log.MessageLine(screen.Name + " " + screen.Enabled);
    }
}
