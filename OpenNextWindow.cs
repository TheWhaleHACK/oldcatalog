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
		trigger.EventEnter.Connect(ChangeScreen);
	}
	private void ChangeScreen()
	{
		thisScreen.Enabled = false;
		for(int i=0;i<thisScreen.NumChildren;i++)
			thisScreen.GetChild(i).Enabled = false;

		nextScreen.Enabled = true;
		for(int i=0;i<nextScreen.NumChildren;i++)
			nextScreen.GetChild(i).Enabled = true;
	}

	void Update()
	{
		// foreach(var screen in thisScreen)
		// 	Log.MessageLine(screen.Name + " " + screen.Enabled);
	}
}