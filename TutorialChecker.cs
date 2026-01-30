
using System.Collections.Generic;
using System.Drawing;
using Unigine;

[Component(PropertyGuid = "ce1ffa5c8b6aaecf718d621c4cfec78145b64920")]
public class TutorialChecker : Component
{
	[ShowInEditor]
	[ParameterSlider(Title = "Обучающие объекты", Group = "Ноды")]
	private List<Node> tutorialObjects;

	[ShowInEditor]
	[ParameterSlider(Title = "Обучающие тексты", Group = "Тексты")]
	private List<Node> tutorialTexsts;

	[ShowInEditor]
	[ParameterSlider(Title = "Объект класса движения поезда", Group = "Движение поезда")]
	private TrainMovement trainMovement;

	[ShowInEditor]
	[ParameterSlider(Title = "Тип обучения")]
	TutorialObject.Tutorials tutorialType = TutorialObject.Tutorials.START;

	private float lerpCoefficient = 0.5f;

	private vec4 initialColor = new vec4();

	private Object tutorialObjectMesh = null;

	private void Update()
	{
		lerpCoefficient += 0.01f;
		if (lerpCoefficient > 1)
		{
			lerpCoefficient = 0;
		}
		if (tutorialObjects.Count == 0)
		{
			trainMovement.SetCanMove(true);
		}
		else
		{
			foreach (TutorialObject tutorialObject in tutorialObjects[0].GetComponents<TutorialObject>())
			{
				if (tutorialObject.GetTutorialType() == tutorialType)
				{
					if (tutorialObject.CheckComplition() == false)
					{
						if (!tutorialObject.GetIsNodeExternal())
						{
							tutorialObjectMesh = tutorialObjects[0] as Object;
						}
						else
						{
							tutorialObjectMesh = tutorialObjects[0].GetComponent<TutorialObject>().GetExternalNode() as Object;
						}

						for (int i = 0; i < tutorialObjectMesh.NumSurfaces; i++)
						{
							tutorialObjectMesh.SetMaterialState("auxiliary", 1, i);
							tutorialObjectMesh.SetMaterialParameterFloat4("albedo_color", MathLib.Lerp(new vec4(1f, 0f, 0f, 1.0f), new vec4(1f, 1f, 0f, 1.0f),
							lerpCoefficient), i);
						}
					}
					if (tutorialObject.CheckComplition() == true)
					{
						for (int i = 0; i < tutorialObjectMesh.NumSurfaces; i++)
						{
							tutorialObjectMesh.SetMaterialParameterFloat4("albedo_color", tutorialObject.GetInitialColors()[i], i);
						}
						tutorialTexsts[0].Enabled = false;
						tutorialObjects.RemoveAt(0);
						tutorialTexsts.RemoveAt(0);
						if (tutorialTexsts.Count > 0)
							tutorialTexsts[0].Enabled = true;
					}
				}


			}
		}

	}


}