using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

[Serializable]
public class CurentStep
{
	public enum AxisToRotate
	{
		x = 0,
		y = 1,
		z = 2,
	}

	public string Task = "lll";
	public Node currentNode;
	public List<vec4> initialColors = new List<vec4>(); // для хранения исходных цветов
	public bool isButton = false;
	public bool isTrigger = false;
	public bool isRotatableItem = false;

	[ShowInEditor]
	[ParameterSlider(Title = "Угол активации", Group = "Вектор вращения")]
	[ParameterCondition(nameof(isRotatableItem), 1)]
	public float rotationAngle;

	[ShowInEditor]
	[ParameterSlider(Title = "Погрешность вращения", Group = "Вектор вращения")]
	[ParameterCondition(nameof(isRotatableItem), 1)]
	public float angle = 5;

	[ShowInEditor]
	[ParameterSlider(Title = "Ось вращения", Group = "Вектор вращения")]
	[ParameterCondition(nameof(isRotatableItem), 1)]
	public AxisToRotate rotationAxis = AxisToRotate.z;

	[ShowInEditor]
	[ParameterSlider(Title = "Позиция активации", Group = "Вектор вращения")]
	[ParameterCondition(nameof(isButton), 1)]
	public float threshold;

	[ShowInEditor]
	[ParameterSlider(Title = "Ось позиции", Group = "Вектор вращения")]
	[ParameterCondition(nameof(isButton), 1)]
	public AxisToRotate thresholdAxis = AxisToRotate.z;

	[ShowInEditor]
	[ParameterSlider(Title = "Нода триггера", Group = "Вектор вращения")]
	[ParameterCondition(nameof(isTrigger), 1)]
	public Node triggerNode;
}

[Component(PropertyGuid = "1a1f908f110275755a9826702d3de29738c603cc")]
public class StepsOfTasks : Component
{
	[ShowInEditor] public CurentStep[] Steps = new CurentStep[0];

	[ShowInEditor]
	[ParameterFile(Filter = ".ui")]
	private string UIPath;

	private ObjectGui objectGui;
	private UserInterface ui = null;
	private Gui gui = null;
	private WidgetLabel detailInfoLabel;
	private WidgetVBox vBox = null;
	public int currentStepIndex = -1;
	private bool isFirstUpdate = true;
	private float lerpCoefficient = 0f; // для пульсации цвета

	// === ИСПРАВЛЕНИЯ ДЛЯ ТРИГГЕРОВ ===
	// Флаг активации ТОЛЬКО для текущего шага с триггером
	private bool currentStepTriggerActivated = false;
	// Храним ссылки на делегаты для безопасного отключения (правильный тип!)
	private Unigine.EventDelegate currentTriggerEnterHandler = null;
	private Unigine.EventDelegate currentTriggerLeaveHandler = null;
	// Индекс шага, для которого подключены события
	private int currentTriggerStepIndex = -1;

	void Init()
	{
		objectGui = node as ObjectGui;
		gui = objectGui.GetGui();
		ui = new UserInterface(gui, UIPath);
		vBox = ui.GetWidget(ui.FindWidget("vbox")) as WidgetVBox;
		detailInfoLabel = ui.GetWidget(ui.FindWidget("detailInfoLabel")) as WidgetLabel;
		detailInfoLabel.FontSize = 75;
		gui.AddChild(vBox, Gui.ALIGN_EXPAND);
	}

	void Update()
	{
		// Обновляем коэффициент пульсации
		lerpCoefficient += 0.01f;
		if (lerpCoefficient > 1.0f)
		{
			lerpCoefficient = 0.0f;
		}

		if (isFirstUpdate)
		{
			isFirstUpdate = false;
			GoToStep(0);
			return;
		}

		if (currentStepIndex >= 0 && currentStepIndex < Steps.Length)
		{
			var step = Steps[currentStepIndex];

			// Применяем пульсирующую подсветку для текущего шага
			if ((step.isRotatableItem || step.isButton) && step.currentNode != null)
			{
				ApplyHighlight(step);
			}
			else if (step.isTrigger && step.triggerNode != null)
			{
				ApplyHighlight(step);
			}

			// Проверка выполнения шага для вращаемых объектов
			if (step.isRotatableItem)
			{
				var rotation = MathLib.DecomposeRotationXYZ(step.currentNode.GetRotation().Mat3);
				switch (step.rotationAxis)
				{
					case CurentStep.AxisToRotate.x:
						if (rotation.x > step.rotationAngle - step.angle && rotation.x <= step.rotationAngle + step.angle)
							CompleteStep(step);
						break;
					case CurentStep.AxisToRotate.y:
						if (rotation.y > step.rotationAngle - step.angle && rotation.y <= step.rotationAngle + step.angle)
							CompleteStep(step);
						break;
					case CurentStep.AxisToRotate.z:
						if (rotation.z > step.rotationAngle - step.angle && rotation.z <= step.rotationAngle + step.angle)
							CompleteStep(step);
						break;
				}
				return;
			}

			// Проверка выполнения шага для кнопок
			if (step.isButton)
			{
				switch (step.thresholdAxis)
				{
					case CurentStep.AxisToRotate.x:
						if (step.currentNode.Position.x <= step.threshold)
							CompleteStep(step);
						break;
					case CurentStep.AxisToRotate.y:
						if (step.currentNode.Position.y <= step.threshold)
							CompleteStep(step);
						break;
					case CurentStep.AxisToRotate.z:
						if (step.currentNode.Position.z <= step.threshold)
							CompleteStep(step);
						break;
				}
				return;
			}

			// ТРИГГЕРЫ БОЛЬШЕ НЕ ОБРАБАТЫВАЮТСЯ В UPDATE!
			// Их логика полностью перенесена в событийную модель (GoToStep)
		}
	}

	// === КРИТИЧЕСКИ ВАЖНО: управление событиями ТОЛЬКО при смене шага ===
	private void GoToStep(int index)
	{
		// 1. Отключаем события ПРЕДЫДУЩЕГО триггер-шага
		if (currentStepIndex >= 0 && currentStepIndex < Steps.Length)
		{
			var oldStep = Steps[currentStepIndex];
			if (oldStep.isTrigger && oldStep.triggerNode != null)
			{
				if (oldStep.triggerNode is WorldTrigger oldTrigger)
				{
					if (currentTriggerEnterHandler != null)
					{
						oldTrigger.EventEnter.Disconnect(currentTriggerEnterHandler);
						currentTriggerEnterHandler = null;
					}
					if (currentTriggerLeaveHandler != null)
					{
						oldTrigger.EventLeave.Disconnect(currentTriggerLeaveHandler);
						currentTriggerLeaveHandler = null;
					}
				}
			}

			// Восстанавливаем цвет предыдущего шага
			RestoreOriginalColors(oldStep);
		}

		currentStepIndex = index;

		// Завершение тренировки
		if (currentStepIndex >= Steps.Length)
		{
			detailInfoLabel.Text = "Сборка завершена";
			// Доп. логика завершения (скрытие UI и т.д.)
			return;
		}

		var step = Steps[currentStepIndex];
		detailInfoLabel.Text = step.Task;

		// Сохраняем исходные цвета и применяем подсветку
		if ((step.isRotatableItem || step.isButton) && step.currentNode != null)
		{
			SaveInitialColors(step);
			ApplyHighlight(step);
		}
		else if (step.isTrigger && step.triggerNode != null)
		{
			SaveInitialColors(step);
			ApplyHighlight(step);

			// 2. Подключаем события ТОЛЬКО для НОВОГО триггер-шага
			if (step.triggerNode is WorldTrigger trigger)
			{
				// Сбрасываем флаг активации для НОВОГО шага
				currentStepTriggerActivated = false;
				currentTriggerStepIndex = currentStepIndex;

				// Создаем делегаты с правильной сигнатурой и защитой от гонок данных
				currentTriggerEnterHandler = (Node enteredNode) =>
				{
					// Защита: проверяем что мы всё ещё на том же шаге
					if (currentStepIndex != currentTriggerStepIndex) 
						return;
					
					// Главное условие: разрешаем активацию ТОЛЬКО один раз за шаг
					if (!currentStepTriggerActivated)
					{
						currentStepTriggerActivated = true; // Сразу блокируем повторную активацию
						CompleteStep(step);
					}
				};

				currentTriggerLeaveHandler = (Node leftNode) =>
				{
					// Ничего не делаем при выходе — флаг НЕ сбрасываем
					// (требование: триггер активируется один раз за шаг, даже если пользователь вышел и вернулся)
				};

				// Подключаем события
				trigger.EventEnter.Connect(currentTriggerEnterHandler);
				trigger.EventLeave.Connect(currentTriggerLeaveHandler);
			}
		}
	}

	private void CompleteStep(CurentStep step)
	{
		// Восстанавливаем цвет ДО перехода на следующий шаг
		RestoreOriginalColors(step);
		GoToStep(currentStepIndex + 1);
	}

	private void SaveInitialColors(CurentStep step)
	{
		if (step.initialColors.Count > 0) return;

		Unigine.Object obj = step.isTrigger ? 
			(step.triggerNode as Unigine.Object) : 
			(step.currentNode as Unigine.Object);

		if (obj == null) return;

		for (int i = 0; i < obj.NumSurfaces; i++)
		{
			step.initialColors.Add(obj.GetMaterialParameterFloat4("albedo_color", i));
		}
	}

	private void ApplyHighlight(CurentStep step)
	{
		Unigine.Object obj = step.isTrigger ? 
			(step.triggerNode as Unigine.Object) : 
			(step.currentNode as Unigine.Object);

		if (obj == null) return;

		vec4 highlightColor = MathLib.Lerp(
			new vec4(1f, 0f, 0f, 1.0f),
			new vec4(1f, 1f, 0f, 1.0f),
			lerpCoefficient
		);

		for (int i = 0; i < obj.NumSurfaces; i++)
		{
			obj.SetMaterialState("auxiliary", 1, i);
			obj.SetMaterialParameterFloat4("albedo_color", highlightColor, i);
		}
	}

	private void RestoreOriginalColors(CurentStep step)
	{
		Unigine.Object obj = step.isTrigger ? 
			(step.triggerNode as Unigine.Object) : 
			(step.currentNode as Unigine.Object);
		
		if (obj == null || step.initialColors.Count == 0) return;

		for (int i = 0; i < obj.NumSurfaces; i++)
		{
			if (i < step.initialColors.Count)
			{
				obj.SetMaterialParameterFloat4("albedo_color", step.initialColors[i], i);
				obj.SetMaterialState("auxiliary", 0, i);
			}
		}
	}
}
