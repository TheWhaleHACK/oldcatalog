using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "9ba489847ba3122815d7c772baf33eb05f5740ca")]
public class OpenNextWindow : Component
{
    private WorldTrigger trigger;
    [ShowInEditor] private ObjectMeshStatic thisScreen;
    [ShowInEditor] private ObjectMeshStatic nextScreen;

    void Init()
    {
        // write here code to be called on component initialization
        trigger = node as WorldTrigger;
        if (trigger != null)
        {
            trigger.EventEnter += OnTriggerEnter; // Используем += для подключения делегата
        }
        else
        {
            Log.Error("OpenNextWindow: Node is not a WorldTrigger! Node: {0}\n", node.Name);
        }
    }

    // Метод, который будет вызван при срабатывании триггера
    private void OnTriggerEnter(Node entered_node) // Имя параметра может зависеть от сигнатуры EventEnter в вашей версии Unigine
    {
        // Проверяем, активен ли наш "thisScreen"
        if (thisScreen != null && thisScreen.Enabled)
        {
            Log.Message("OpenNextWindow: Trigger '{0}' activated for screen '{1}'. Switching to '{2}'.\n",
                        node.Name, thisScreen.Name, nextScreen != null ? nextScreen.Name : "null");

            // Отключаем текущий экран и его детей
            thisScreen.Enabled = false;
            for(int i = 0; i < thisScreen.NumChildren; i++)
            {
                thisScreen.GetChild(i).Enabled = false;
            }

            // Включаем следующий экран и его детей
            if (nextScreen != null)
            {
                nextScreen.Enabled = true;
                for(int i = 0; i < nextScreen.NumChildren; i++)
                {
                    nextScreen.GetChild(i).Enabled = true;
                }
            }
            else
            {
                Log.Warning("OpenNextWindow: nextScreen is not assigned for trigger '{0}' on screen '{1}'.\n", node.Name, thisScreen.Name);
            }
        }
        else
        {
            // Логируем, если нужно отладить, что триггер сработал, но экран не был активен
            // Log.Debug("OpenNextWindow: Trigger '{0}' fired, but its screen '{1}' was not active.\n", node.Name, thisScreen != null ? thisScreen.Name : "null");
            // Не делаем ничего, если экран не активен
        }
    }

    void Update()
    {
        // foreach(var screen in thisScreen)
        // 	Log.MessageLine(screen.Name + " " + screen.Enabled);
    }
}
