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

	private bool enteredTrigger = false;

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

			// Применяем пульсирующую подсветку для текущего шага (если объект видимый)
			if ((step.isRotatableItem || step.isButton) && step.currentNode != null)
			{
				ApplyHighlight(step);
			}

			if(step.isTrigger && step.triggerNode!=null)
			{
				ApplyHighlight(step);
			}

			// Проверка выполнения шага
			if (step.isRotatableItem)
			{
				Log.MessageLine( MathLib.DecomposeRotationXYZ(step.currentNode.GetRotation().Mat3) );
				switch (step.rotationAxis)
				{
					case CurentStep.AxisToRotate.x:
						if (MathLib.DecomposeRotationXYZ(step.currentNode.GetRotation().Mat3).x > step.rotationAngle - step.angle &&
							MathLib.DecomposeRotationXYZ(step.currentNode.GetRotation().Mat3).x <= step.rotationAngle + step.angle)
						{
							CompleteStep(step);
						}
						break;
					case CurentStep.AxisToRotate.y:
						if (MathLib.DecomposeRotationXYZ(step.currentNode.GetRotation().Mat3).y > step.rotationAngle - step.angle &&
							MathLib.DecomposeRotationXYZ(step.currentNode.GetRotation().Mat3).y <= step.rotationAngle + step.angle)
						{
							CompleteStep(step);
						}
						break;
					case CurentStep.AxisToRotate.z:
						if (MathLib.DecomposeRotationXYZ(step.currentNode.GetRotation().Mat3).z > step.rotationAngle - step.angle &&
							MathLib.DecomposeRotationXYZ(step.currentNode.GetRotation().Mat3).z <= step.rotationAngle + step.angle)
						{
							CompleteStep(step);
						}
						break;
				}
				return;
			}

			if (step.isButton)
			{
				switch (step.thresholdAxis)
				{
					case CurentStep.AxisToRotate.x:
						if (step.currentNode.Position.x <= step.threshold)
						{
							CompleteStep(step);
						}
						break;
					case CurentStep.AxisToRotate.y:
						if (step.currentNode.Position.y <= step.threshold)
						{
							CompleteStep(step);
						}
						break;
					case CurentStep.AxisToRotate.z:
						if (step.currentNode.Position.z <= step.threshold)
						{
							CompleteStep(step);
						}
						break;
				}
				return;
			}

			if (step.isTrigger)
			{
				WorldTrigger thisTrigger = step.currentNode as WorldTrigger;
				thisTrigger.EventEnter.Connect(() => triggerEnter(step));
				thisTrigger.EventLeave.Connect(triggerLeave);
				return;
			}
		}
	}

	private void triggerEnter(CurentStep step)
	{
		if(enteredTrigger == false)
		{
			CompleteStep(step);
			enteredTrigger = true;
		}
	}

	private void triggerLeave()
	{
		if(enteredTrigger == true)
		{
			enteredTrigger = false;
		}
	}

	private void GoToStep(int index)
	{
		// Восстанавливаем исходный цвет предыдущего шага (если был)
		if (currentStepIndex >= 0 && currentStepIndex < Steps.Length)
		{
			RestoreOriginalColors(Steps[currentStepIndex]);
		}

		currentStepIndex = index;

		if (currentStepIndex >= Steps.Length)
		{
			detailInfoLabel.Text = "Сборка завершена";
			// Скрыть интерфейс или выполнить завершающие действия
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

		if(step.isTrigger && step.triggerNode!=null)
		{
			SaveInitialColors(step);
			ApplyHighlight(step);
		}
	}

	private void CompleteStep(CurentStep step)
	{
		// Возвращаем исходный цвет объекта
		RestoreOriginalColors(step);
		// Переходим к следующему шагу
		GoToStep(currentStepIndex + 1);
	}

	private void SaveInitialColors(CurentStep step)
	{
		// Сохраняем цвета только один раз
		if (step.initialColors.Count > 0) return;

		Unigine.Object obj = null;
		if(step.isTrigger)
			obj = step.triggerNode as Unigine.Object;
		else
			obj = step.currentNode as Unigine.Object;

		if (obj == null) return;

		for (int i = 0; i < obj.NumSurfaces; i++)
		{
			step.initialColors.Add(obj.GetMaterialParameterFloat4("albedo_color", i));
		}
	}

	private void ApplyHighlight(CurentStep step)
	{
		Unigine.Object obj = null;
		if(step.isTrigger)
			obj = step.triggerNode as Unigine.Object;
		else
			obj = step.currentNode as Unigine.Object;

		if (obj == null) return;

		// Пульсирующий цвет: от красного (1,0,0) к жёлтому (1,1,0)
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
		Unigine.Object obj = null;
		if(step.isTrigger)
			obj = step.triggerNode as Unigine.Object;
		else
			obj = step.currentNode as Unigine.Object;
		if (obj == null || step.initialColors.Count == 0) return;

		for (int i = 0; i < obj.NumSurfaces; i++)
		{
			if (i < step.initialColors.Count)
			{
				obj.SetMaterialParameterFloat4("albedo_color", step.initialColors[i], i);
				obj.SetMaterialState("auxiliary", 0, i); // отключаем auxiliary state
			}
		}
	}
}
