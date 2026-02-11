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
	public List<vec4> initialColors = new List<vec4>();
	public bool isButton = false;
	public bool isTrigger = false;
	public bool isRotatableItem = false;
	public bool isEntered = false; // Флаг входа ДЛЯ КАЖДОГО шага отдельно

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
	private float lerpCoefficient = 0f;

	// УДАЛЕНО: глобальный флаг enteredTrigger — он больше не нужен

	void Init()
	{
		objectGui = node as ObjectGui;
		gui = objectGui.GetGui();
		ui = new UserInterface(gui, UIPath);
		vBox = ui.GetWidget(ui.FindWidget("vbox")) as WidgetVBox;
		detailInfoLabel = ui.GetWidget(ui.FindWidget("detailInfoLabel")) as WidgetLabel;
		detailInfoLabel.FontSize = 75;
		gui.AddChild(vBox, Gui.ALIGN_EXPAND);

		// Подписка на события триггеров ОДИН РАЗ при инициализации
		SubscribeTriggers();
	}

	// Новый метод: однократная подписка на все триггеры
	private void SubscribeTriggers()
	{
		for (int i = 0; i < Steps.Length; i++)
		{
			var step = Steps[i];
			if (step.isTrigger && step.triggerNode != null)
			{
				WorldTrigger trigger = step.triggerNode as WorldTrigger;
				if (trigger != null)
				{
					int stepIndex = i; // Захватываем индекс для замыкания
					trigger.EventEnter.Connect(() => 
					{
						// Выполняем шаг ТОЛЬКО если:
						// 1. Это текущий активный шаг
						// 2. Мы ещё не вошли в триггер для этого шага
						if (currentStepIndex == stepIndex && !step.isEntered)
						{
							CompleteStep(step);
							step.isEntered = true;
						}
					});
					
					trigger.EventLeave.Connect(() => 
					{
						// Сбрасываем флаг ТОЛЬКО для текущего шага
						if (currentStepIndex == stepIndex && step.isEntered)
						{
							step.isEntered = false;
						}
					});
				}
			}
		}
	}

	void Update()
	{
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

			// Применяем пульсирующую подсветку
			if ((step.isRotatableItem || step.isButton || step.isTrigger) && 
				(step.currentNode != null || (step.isTrigger && step.triggerNode != null)))
			{
				ApplyHighlight(step);
			}

			// Проверка выполнения шага для вращаемых объектов
			if (step.isRotatableItem && step.currentNode != null)
			{
				vec3 rotation = MathLib.DecomposeRotationXYZ(step.currentNode.GetRotation().Mat3);
				float currentAngle = 0f;
				
				switch (step.rotationAxis)
				{
					case CurentStep.AxisToRotate.x: currentAngle = rotation.x; break;
					case CurentStep.AxisToRotate.y: currentAngle = rotation.y; break;
					case CurentStep.AxisToRotate.z: currentAngle = rotation.z; break;
				}

				if (currentAngle > step.rotationAngle - step.angle && 
					currentAngle <= step.rotationAngle + step.angle)
				{
					CompleteStep(step);
				}
				return;
			}

			// Проверка выполнения шага для кнопок
			if (step.isButton && step.currentNode != null)
			{
				float currentPosition = 0f;
				switch (step.thresholdAxis)
				{
					case CurentStep.AxisToRotate.x: currentPosition = step.currentNode.Position.x; break;
					case CurentStep.AxisToRotate.y: currentPosition = step.currentNode.Position.y; break;
					case CurentStep.AxisToRotate.z: currentPosition = step.currentNode.Position.z; break;
				}

				if (currentPosition <= step.threshold)
				{
					CompleteStep(step);
				}
				return;
			}

			// ВАЖНО: УДАЛЕНА ПОДПИСКА НА СОБЫТИЯ ТРИГГЕРА ИЗ UPDATE!
			// Подписка происходит один раз в Init() через SubscribeTriggers()
		}
	}

	private void GoToStep(int index)
	{
		// Восстанавливаем цвет предыдущего шага
		if (currentStepIndex >= 0 && currentStepIndex < Steps.Length)
		{
			RestoreOriginalColors(Steps[currentStepIndex]);
			// Сбрасываем флаг входа для предыдущего шага с триггером
			if (Steps[currentStepIndex].isTrigger)
			{
				Steps[currentStepIndex].isEntered = false;
			}
		}

		currentStepIndex = index;

		if (currentStepIndex >= Steps.Length)
		{
			detailInfoLabel.Text = "Сборка завершена";
			// Опционально: скрыть интерфейс
			// gui.RemoveChild(vBox);
			return;
		}

		var step = Steps[currentStepIndex];
		detailInfoLabel.Text = step.Task;

		// Сохраняем исходные цвета и применяем подсветку
		if ((step.isRotatableItem || step.isButton || step.isTrigger) && 
			(step.currentNode != null || (step.isTrigger && step.triggerNode != null)))
		{
			SaveInitialColors(step);
			ApplyHighlight(step);
		}
	}

	private void CompleteStep(CurentStep step)
	{
		RestoreOriginalColors(step);
		GoToStep(currentStepIndex + 1);
	}

	private void SaveInitialColors(CurentStep step)
	{
		if (step.initialColors.Count > 0) return;

		Unigine.Object obj = step.isTrigger ? step.triggerNode as Unigine.Object : step.currentNode as Unigine.Object;
		if (obj == null) return;

		for (int i = 0; i < obj.NumSurfaces; i++)
		{
			step.initialColors.Add(obj.GetMaterialParameterFloat4("albedo_color", i));
		}
	}

	private void ApplyHighlight(CurentStep step)
	{
		Unigine.Object obj = step.isTrigger ? step.triggerNode as Unigine.Object : step.currentNode as Unigine.Object;
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
		Unigine.Object obj = step.isTrigger ? step.triggerNode as Unigine.Object : step.currentNode as Unigine.Object;
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
