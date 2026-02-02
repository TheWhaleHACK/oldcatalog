using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "5d0b708e14083d56730fe87db6e8972264ecae5d")]
public class WaypointMover : Component
{
    [ShowInEditor] private Node splineObject; // Объект, который будет перемещаться (ваш основной объект)
    [ShowInEditor] private List<Node> waypoints; // Список точек маршрута
    [ShowInEditor] private Node speedLever; // Управление скоростью
    [ShowInEditor] private List<Node> RotatingNodes; // Объекты, которые будут вращаться
    [ShowInEditor] private float rotationSpeed = 0.05f;
    [ShowInEditor] private float waypointThreshold = 2.0f; // Расстояние до точки, при котором считается, что достигли
    [ShowInEditor] private bool loopRoute = false; // Зацикливать маршрут или нет

    private int currentWaypointIndex = 0;
    private bool isMoving = true;
    private quat currentRotation = quat.IDENTITY;
    private List<vec3> originalScales = new List<vec3>();
    
    private globalSpeed globalSpeedComponent;

    void Init()
    {
        // Получаем компонент управления скоростью
        if (speedLever != null)
        {
            globalSpeedComponent = speedLever.GetComponent<globalSpeed>();
            if (globalSpeedComponent == null)
                Log.Error($"Node {speedLever.Name} does not have a globalSpeed component!");
        }

        // Инициализируем начальное вращение из первого объекта в списке вращающихся
        if (RotatingNodes != null && RotatingNodes.Count > 0 && RotatingNodes[0] != null)
        {
            currentRotation = RotatingNodes[0].GetWorldRotation();
        }
        else
        {
            currentRotation = quat.IDENTITY;
        }

        // Сохраняем исходные масштабы вращающихся объектов
        InitializeOriginalScales();
    }

    private void InitializeOriginalScales()
    {
        originalScales.Clear();
        if (RotatingNodes != null)
        {
            foreach (var obj in RotatingNodes)
            {
                if (obj != null)
                {
                    originalScales.Add(obj.WorldScale);
                }
                else
                {
                    originalScales.Add(vec3.ONE);
                }
            }
        }
    }

    private float GetSpeed() => globalSpeedComponent?.gSpeed ?? 0f;

    private bool IsEnd() => globalSpeedComponent?.gEnd ?? false;

    private void Update()
    {
        if (!isMoving || splineObject == null || waypoints == null || waypoints.Count == 0)
            return;

        if (currentWaypointIndex >= waypoints.Count)
        {
            if (loopRoute)
            {
                currentWaypointIndex = 0; // Зацикливаем маршрут
            }
            else
            {
                // Достигли последней точки
                isMoving = false;
                return;
            }
        }

        Node targetWaypoint = waypoints[currentWaypointIndex];
        if (targetWaypoint == null)
            return;

        // Получаем позицию текущей точки
        vec3 targetPosition = -targetWaypoint.WorldPosition;
        vec3 currentPosition = splineObject.WorldPosition;

        // Вычисляем направление к цели (для движения)
        vec3 directionToTarget = targetPosition - currentPosition;
        float distanceToTarget = MathLib.Length(directionToTarget);

        // Если близко к точке - переходим к следующей
        if (distanceToTarget < waypointThreshold)
        {
            if (currentWaypointIndex < waypoints.Count - 1)
            {
                currentWaypointIndex++;
            }
            else
            {
                if (loopRoute)
                {
                    currentWaypointIndex = 0; // Зацикливаем
                }
                else
                {
                    // Достигли последней точки
                    isMoving = false;
                    return;
                }
            }
        }

        // Перемещение объекта
        float speed = GetSpeed() * Game.IFps;
        if (directionToTarget.Length < 0.001f) return;
        vec3 moveDirection = MathLib.Normalize(directionToTarget);
        
        // Ограничиваем перемещение, чтобы не пролететь мимо точки
        float maxMoveDistance = MathLib.Min(speed, distanceToTarget);
        vec3 newPosition = currentPosition + moveDirection * maxMoveDistance;

        // Устанавливаем новую позицию
        splineObject.WorldPosition = newPosition;

        // === ИСПРАВЛЕНИЕ: плавный поворот через lookahead-точку ===
        // Вместо направления к текущей точке — смотрим на СЛЕДУЮЩУЮ
        int nextIndex = (currentWaypointIndex + 1) % waypoints.Count;
        Node lookaheadWaypoint = waypoints[nextIndex];
        if (lookaheadWaypoint == null) return;

        vec3 lookaheadPosition = -lookaheadWaypoint.WorldPosition;
        vec3 directionForRotation = lookaheadPosition - currentPosition;

        if (directionForRotation.Length < 0.001f) return;

        vec3 moveDirForRotation = MathLib.Normalize(directionForRotation);
        // ==========================================================

        //ROTATING
        // В Unigine "вперёд" = -Z → но у вас уже всё настроено под moveDirection без минуса
        vec3 desiredForward = moveDirForRotation; // ← БЫЛО: moveDirection, СТАЛО: moveDirForRotation
        vec3 desiredUp = vec3.UP;

        vec3 right = MathLib.Normalize(MathLib.Cross(desiredUp, desiredForward));
        if (right.Length < 0.001f)
        {
            desiredUp = vec3.RIGHT;
            right = MathLib.Normalize(MathLib.Cross(desiredUp, desiredForward));
        }

        vec3 up = MathLib.Normalize(MathLib.Cross(desiredForward, right));

        // Построение кватерниона из базиса
        float trace = right.x + up.y + desiredForward.z;
        quat targetRotation;

        if (trace > 0)
        {
            float s = 0.5f / MathLib.Sqrt(trace + 1.0f);
            targetRotation.w = 0.25f / s;
            targetRotation.x = (up.z - desiredForward.y) * s;
            targetRotation.y = (desiredForward.x - right.z) * s;
            targetRotation.z = (right.y - up.x) * s;
        }
        else
        {
            if (right.x > up.y && right.x > desiredForward.z)
            {
                float s = 2.0f * MathLib.Sqrt(1.0f + right.x - up.y - desiredForward.z);
                targetRotation.w = (up.z - desiredForward.y) / s;
                targetRotation.x = 0.25f * s;
                targetRotation.y = (right.y + up.x) / s;
                targetRotation.z = (desiredForward.x + right.z) / s;
            }
            else if (up.y > desiredForward.z)
            {
                float s = 2.0f * MathLib.Sqrt(1.0f + up.y - right.x - desiredForward.z);
                targetRotation.w = (desiredForward.x - right.z) / s;
                targetRotation.x = (right.y + up.x) / s;
                targetRotation.y = 0.25f * s;
                targetRotation.z = (up.z + desiredForward.y) / s;
            }
            else
            {
                float s = 2.0f * MathLib.Sqrt(1.0f + desiredForward.z - right.x - up.y);
                targetRotation.w = (right.y - up.x) / s;
                targetRotation.x = (desiredForward.x + right.z) / s;
                targetRotation.y = (up.z + desiredForward.y) / s;
                targetRotation.z = 0.25f * s;
            }
        }

        // Компенсация ориентации модели (как в SplineMover)
        targetRotation = MathLib.Normalize(targetRotation * new quat(270, 0, 0));

        // Плавная интерполяция
        currentRotation = MathLib.Slerp(currentRotation, targetRotation, rotationSpeed);

        if (RotatingNodes != null)
        {
            for (int i = 0; i < RotatingNodes.Count; i++)
            {
                if (RotatingNodes[i] != null)
                {
                    Node child = RotatingNodes[i];
                    // Сохраняем текущий scale и position — они могут сломаться при SetWorldTransform
                    vec3 savedScale = child.WorldScale;
                    vec3 savedPosition = child.WorldPosition;

                    // Устанавливаем целевое мировое вращение
                    child.SetWorldRotation(currentRotation);

                    // Восстанавливаем scale и position — это предотвращает искажения
                    child.WorldScale = originalScales[i];
                    child.WorldPosition = savedPosition;
                }
            }
        }
    }

    // --- Остальные методы без изменений ---
    private quat CalculateLookRotation(vec3 forward)
    {
        vec3 desiredForward = -MathLib.Normalize(forward);
        vec3 desiredUp = vec3.UP;
        vec3 right = MathLib.Normalize(MathLib.Cross(desiredUp, desiredForward));
        if (right.Length < 0.001f)
        {
            desiredUp = vec3.RIGHT;
            right = MathLib.Normalize(MathLib.Cross(desiredUp, desiredForward));
        }
        vec3 up = MathLib.Normalize(MathLib.Cross(desiredForward, right));
        return CreateQuaternionFromBasis(right, up, desiredForward);
    }

    private quat CreateQuaternionFromBasis(vec3 right, vec3 up, vec3 forward)
    {
        float m00 = right.x, m01 = right.y, m02 = right.z;
        float m10 = up.x, m11 = up.y, m12 = up.z;
        float m20 = forward.x, m21 = forward.y, m22 = forward.z;
        float trace = m00 + m11 + m22;
        float w, x, y, z;

        if (trace > 0)
        {
            float s = MathLib.Sqrt(trace + 1.0f) * 2f;
            w = 0.25f * s;
            x = (m21 - m12) / s;
            y = (m02 - m20) / s;
            z = (m10 - m01) / s;
        }
        else if ((m00 > m11) & (m00 > m22))
        {
            float s = MathLib.Sqrt(1.0f + m00 - m11 - m22) * 2f;
            w = (m21 - m12) / s;
            x = 0.25f * s;
            y = (m01 + m10) / s;
            z = (m02 + m20) / s;
        }
        else if (m11 > m22)
        {
            float s = MathLib.Sqrt(1.0f + m11 - m00 - m22) * 2f;
            w = (m02 - m20) / s;
            x = (m01 + m10) / s;
            y = 0.25f * s;
            z = (m12 + m21) / s;
        }
        else
        {
            float s = MathLib.Sqrt(1.0f + m22 - m00 - m11) * 2f;
            w = (m10 - m01) / s;
            x = (m02 + m20) / s;
            y = (m12 + m21) / s;
            z = 0.25f * s;
        }
        return new quat(x, y, z, w);
    }

    private void ApplyRotationToAllObjects()
    {
        if (RotatingNodes == null) return;
        for (int i = 0; i < RotatingNodes.Count; i++)
        {
            if (RotatingNodes[i] != null)
            {
                Node obj = RotatingNodes[i];
                vec3 savedPosition = obj.WorldPosition;
                obj.SetWorldRotation(currentRotation);
                obj.WorldPosition = savedPosition;
                obj.WorldScale = originalScales[i];
            }
        }
    }

    public void ResetRoute()
    {
        currentWaypointIndex = 0;
        isMoving = true;
    }

    public bool IsRouteComplete()
    {
        return !isMoving;
    }

    public int GetCurrentWaypointIndex()
    {
        return currentWaypointIndex;
    }

    public void SetWaypoints(List<Node> newWaypoints)
    {
        waypoints = newWaypoints;
        ResetRoute();
    }
}