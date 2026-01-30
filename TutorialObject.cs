
using System.Collections.Generic;
using System.Diagnostics;
using Unigine;

[Component(PropertyGuid = "d0bf7473a7e72950ace547b3d41306c748d7ebb7")]
public class TutorialObject : VRBaseInteractable
{

	[ShowInEditor]
	[ParameterSlider(Title = "Вращающийся объект", Group = "Тип объекта")]
	private bool isObjectRotating = false;

	[ShowInEditor]
	[ParameterSlider(Title = "Угол активации", Group = "Вектор вращения")]
	[ParameterCondition(nameof(isObjectRotating), 1)]
	private float rotationAngle;

	[ShowInEditor]
	[ParameterSlider(Title = "Погрешность вращения", Group = "Вектор вращения")]
	[ParameterCondition(nameof(isObjectRotating), 1)]
	private float angle = 5;

	[ShowInEditor]
	[ParameterSlider(Title = "Кнопка", Group = "Тип объекта")]
	private bool isObjectButton = false;

	[ShowInEditor]
	[ParameterSlider(Title = "Если нода для подсвечивания внешняя", Group = "Тип объекта")]
	private bool isNodeExternal = false;

	[ShowInEditor]
	[ParameterSlider(Title = "Внешняя нода", Group = "Тип объекта")]
	[ParameterCondition(nameof(isNodeExternal), 1)]
	private Node externalNodeForHighlight = null;

	private ButtonBehavior buttonBehavior = null;

	private List<vec4> initialColors = new List<vec4>();

	private enum AxisToRotate
	{
		x = 0,
		y = 1,
		z = 2,
	}

	public enum Tutorials
	{
		START = 0,
		BREAK = 1,
		ENGAGE = 2,
	}

	[ShowInEditor]
	[ParameterSlider(Title = "Ось вращения", Group = "Вектор вращения")]
	[ParameterCondition(nameof(isObjectRotating), 1)]
	AxisToRotate rotationAxis = AxisToRotate.z;

	[ShowInEditor]
	[ParameterSlider(Title = "Тип обучения")]
	Tutorials tutorialType = Tutorials.START;

	protected override void OnReady()
	{
		if (isObjectButton)
		{
			buttonBehavior = node.GetComponent<ButtonBehavior>();
		}
		if (!externalNodeForHighlight)
			for (int i = 0; i < (node as Object).NumSurfaces; i++)
			{
				initialColors.Add((node as Object).GetMaterialParameterFloat4("albedo_color", i));
			}
		else
		{
			for (int i = 0; i < (externalNodeForHighlight as Object).NumSurfaces; i++)
			{
				initialColors.Add((externalNodeForHighlight as Object).GetMaterialParameterFloat4("albedo_color", i));
			}
		}

	}

	public bool CheckComplition()
	{
		if (isObjectRotating == true)
		{

			switch (rotationAxis)
			{

				case AxisToRotate.x:
					if (MathLib.DecomposeRotationXYZ(node.GetRotation().Mat3).x > rotationAngle - angle && MathLib.DecomposeRotationXYZ(node.GetRotation().Mat3).x <= rotationAngle + angle)
					{
						return true;
					}
					break;
				case AxisToRotate.y:
					if (MathLib.DecomposeRotationXYZ(node.GetRotation().Mat3).y > rotationAngle - angle && MathLib.DecomposeRotationXYZ(node.GetRotation().Mat3).y <= rotationAngle + angle)
					{
						return true;
					}
					break;
				case AxisToRotate.z:
					if (MathLib.DecomposeRotationXYZ(node.GetRotation().Mat3).z > rotationAngle - angle && MathLib.DecomposeRotationXYZ(node.GetRotation().Mat3).z <= rotationAngle + angle)
					{
						return true;
					}
					break;
			}
			return false;

		}
		else
		{
			if (buttonBehavior != null)
			{
				return buttonBehavior.GetIsPressed();
			}
			else return false;
		}
	}

	public bool GetIsNodeExternal()
	{
		return isNodeExternal;
	}

	public Node GetExternalNode()
	{
		return externalNodeForHighlight;
	}

	public Tutorials GetTutorialType()
	{
		return tutorialType;
	}

	public List<vec4> GetInitialColors()
	{
		return initialColors;
	}

}