using System.Collections;
using System.Collections.Generic;
using Unigine;
[Component(PropertyGuid = "f91e5a02b9cea2a6cdf2ada59a9014c3fd89ceb2")]
public class buttonSound : Component
{
    [ShowInEditor][Parameter(Title = "Sound Source Node")] private SoundSource soundSource;
    [ShowInEditor][ParameterSlider(Title = "Button Open Voice")] private Node voiceOpenButton;
    [ShowInEditor][ParameterSlider(Title = "Button Open Doors")] private Node openDoorsButton;
    [ShowInEditor][ParameterSlider(Title = "Button Close Doors")] private Node closeDoorsButton;
    [ShowInEditor][ParameterSlider(Title = "Button Stop Kran")] private Node stopKrannButton;
    [ShowInEditor][ParameterSlider(Title = "Button Horn")] private Node hornButton;
    
    private bool isReadySoundClose = true;
    private bool wasOtherButtonsPressed = false; // Для отслеживания предыдущего состояния
    
    public float ThresholdZ = -3.58037f;

    private bool IsButtonPressed(Node button)
    {
        return button != null && button.Position.y <= ThresholdZ;
    }

    private bool IsTumblerPressed(Node tumbler)
    {
        return tumbler != null && MathLib.DecomposeRotationXYZ(tumbler.GetRotation().Mat3).x < -100;
    }

    void Update()
    {
        bool closeDoorsPressed = IsTumblerPressed(closeDoorsButton);
        bool stopKrannPressed = IsButtonPressed(stopKrannButton);

        bool otherButtonsPressed =
            IsButtonPressed(voiceOpenButton) ||
            IsButtonPressed(openDoorsButton) ||
            IsButtonPressed(hornButton);

        // Обработка нажимных кнопок (которые нужно удерживать)
        if (closeDoorsPressed || stopKrannPressed)
        {
            if (soundSource != null && !soundSource.IsPlaying && isReadySoundClose)
            {
                soundSource.Play();
            }
            isReadySoundClose = false;
        }
        else
        {
            isReadySoundClose = true;
        }

        // Обработка ненажимных кнопок (должны срабатывать однократно при нажатии)
        if (otherButtonsPressed && !wasOtherButtonsPressed)
        {
            if (soundSource != null)
            {
                soundSource.Play();
            }
        }
        
        wasOtherButtonsPressed = otherButtonsPressed;
        
        // Убрали автоматическую остановку звука - 
        // ненажимные кнопки должны проиграть звук полностью
    }
}
