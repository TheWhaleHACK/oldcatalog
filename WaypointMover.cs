using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "5d0b708e14083d56730fe87db6e8972264ecae5d")]
public class WaypointMover : Component
{
    [ShowInEditor] private Node controlledObject; // ОСНОВНОЙ ОБЪЕКТ: будет двигаться И поворачиваться
    [ShowInEditor] private List<Node> waypoints;  // Список точек маршрута
    [ShowInEditor] private Node speedLever;       // Управление скоростью
    [ShowInEditor] private float rotationSpeed = 0.05f;
    [ShowInEditor] private float waypointThreshold = 2.0f;
    [ShowInEditor] private bool loopRoute = false;

    private int currentWaypointIndex = 0;
    private bool isMoving = true;
    private quat currentRotation = quat.IDENTITY;
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

        // Инициализируем начальное вращение
        if (controlledObject != null)
        {
            currentRotation = controlledObject.GetWorldRotation();
        }
        else
        {
            currentRotation = quat.IDENTITY;
        }
    }

    private float GetSpeed() => globalSpeedComponent?.gSpeed ?? 0f;
    private bool IsEnd() => globalSpeedComponent?.gEnd ?? false;

    void Update()
    {
        if (!isMoving || controlledObject == null || waypoints == null || waypoints.Count == 0)
            return;

        // Проверка завершения маршрута
        if (currentWaypointIndex >= waypoints.Count)
        {
            if (loopRoute)
            {
                currentWaypointIndex = 0;
            }
            else
            {
                isMoving = false;
                return;
            }
        }

        Node targetWaypoint = waypoints[currentWaypointIndex];
        if (targetWaypoint == null)
            return;

        // Текущая и целевая позиции (БЕЗ МИНУСА - объект движется сам)
        vec3 targetPosition = targetWaypoint.WorldPosition;
        vec3 currentPosition = controlledObject.WorldPosition;

        // Расстояние до цели
        vec3 directionToTarget = targetPosition - currentPosition;
        float distanceToTarget = MathLib.Length(directionToTarget);

        // Достигли точки - переходим к следующей
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
                    currentWaypointIndex = 0;
                }
                else
                {
                    isMoving = false;
                    return;
                }
            }
        }

        // === ДВИЖЕНИЕ ОБЪЕКТА ===
        float speed = GetSpeed() * Game.IFps;
        if (directionToTarget.Length < 0.001f) return;
        
        vec3 moveDirection = MathLib.Normalize(directionToTarget);
        float maxMoveDistance = MathLib.Min(speed, distanceToTarget);
        vec3 newPosition = currentPosition + moveDirection * maxMoveDistance;
        
        controlledObject.WorldPosition = newPosition;

        // === ПОВОРОТ ОБЪЕКТА (с использованием lookahead-точки) ===
        int nextIndex = (currentWaypointIndex + 1) % waypoints.Count;
        Node lookaheadWaypoint = waypoints[nextIndex];
        if (lookaheadWaypoint == null) return;

        vec3 lookaheadPosition = lookaheadWaypoint.WorldPosition;
        vec3 directionForRotation = lookaheadPosition - currentPosition;

        if (directionForRotation.Length < 0.001f) return;

        vec3 moveDirForRotation = MathLib.Normalize(directionForRotation);

        // Определение ориентации (вперёд = -Z в локальной системе координат)
        vec3 desiredForward = moveDirForRotation;
        vec3 desiredUp = vec3.UP;

        vec3 right = MathLib.Normalize(MathLib.Cross(desiredUp, desiredForward));
        if (right.Length < 0.001f)
        {
            desiredUp = vec3.RIGHT;
            right = MathLib.Normalize(MathLib.Cross(desiredUp, desiredForward));
        }

        vec3 up = MathLib.Normalize(MathLib.Cross(desiredForward, right));

        // Построение кватерниона из базиса
        quat targetRotation = CreateQuaternionFromBasis(right, up, desiredForward);

        // Компенсация ориентации модели (если нужно - 270° вокруг X)
        targetRotation = MathLib.Normalize(targetRotation * new quat(270, 0, 0));

        // Плавная интерполяция вращения
        currentRotation = MathLib.Slerp(currentRotation, targetRotation, rotationSpeed);
        controlledObject.SetWorldRotation(currentRotation);
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
        else if ((m00 > m11) && (m00 > m22))
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

    public void ResetRoute()
    {
        currentWaypointIndex = 0;
        isMoving = true;
        if (controlledObject != null)
        {
            currentRotation = controlledObject.GetWorldRotation();
        }
    }

    public bool IsRouteComplete() => !isMoving;

    public int GetCurrentWaypointIndex() => currentWaypointIndex;

    public void SetWaypoints(List<Node> newWaypoints)
    {
        waypoints = newWaypoints;
        ResetRoute();
    }
}
