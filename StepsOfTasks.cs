
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

	void Init()
	{
		// write here code to be called on component initialization
		objectGui = node as ObjectGui;
		gui = objectGui.GetGui();
		ui = new UserInterface(gui,UIPath);
		vBox = ui.GetWidget(ui.FindWidget("vbox")) as WidgetVBox;
		detailInfoLabel = ui.GetWidget(ui.FindWidget("detailInfoLabel")) as WidgetLabel;
		detailInfoLabel.FontSize = 100;
		gui.AddChild(vBox, Gui.ALIGN_EXPAND);
	}
	
	void Update()
	{
		// write here code to be called before updating each render frame
		if(isFirstUpdate)
		{
			isFirstUpdate = false;
			GoToStep(0);
			return;
		}

		if(currentStepIndex>=0 && currentStepIndex<Steps.Length)
		{
			var step = Steps[currentStepIndex];
			if (step.isRotatableItem)
			{
				switch (step.rotationAxis)
				{

					case step.AxisToRotate.x:
						if (MathLib.DecomposeRotationXYZ(step.currentNode.GetRotation().Mat3).x > step.rotationAngle - step.angle && MathLib.DecomposeRotationXYZ(step.currentNode.GetRotation().Mat3).x <= step.rotationAngle + step.angle)
						{
							GoToStep(currentStepIndex+1);
						}
						break;
					case step.AxisToRotate.y:
						if (MathLib.DecomposeRotationXYZ(step.currentNode.GetRotation().Mat3).y > step.rotationAngle - step.angle && MathLib.DecomposeRotationXYZ(step.currentNode.GetRotation().Mat3).y <= step.rotationAngle + step.angle)
						{
							GoToStep(currentStepIndex+1);
						}
						break;
					case step.AxisToRotate.z:
						if (MathLib.DecomposeRotationXYZ(step.currentNode.GetRotation().Mat3).z > step.rotationAngle - step.angle && MathLib.DecomposeRotationXYZ(step.currentNode.GetRotation().Mat3).z <= step.rotationAngle + step.angle)
						{
							GoToStep(currentStepIndex+1);
						}
						break;
				}
				return;				
			}
		}
	}

	private void GoToStep(int index)
	{
		currentStepIndex = index;
		var step = Steps[currentStepIndex];
		detailInfoLabel.Text = step.Task;
		
	}
}