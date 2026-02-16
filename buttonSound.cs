using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "f91e5a02b9cea2a6cdf2ada59a9014c3fd89ceb2")]
public class buttonSound : Component
{
    [ShowInEditor][Parameter(Title = "Sound Source Node")] private SoundSource soundSource;  // Нода типа SoundSource
    //[ShowInEditor][ParameterSlider(Title = "Button Close Voice")] private Node voiceCloseButton;
    [ShowInEditor][ParameterSlider(Title = "Button Open Voice")] private Node voiceOpenButton;
    [ShowInEditor][ParameterSlider(Title = "Button Open Doors")] private Node openDoorsButton;
    [ShowInEditor][ParameterSlider(Title = "Button Close Doors")] private Node closeDoorsButton;
    [ShowInEditor][ParameterSlider(Title = "Button Stop Kran")] private Node stopKrannButton;
    [ShowInEditor][ParameterSlider(Title = "Button Horn")] private Node hornButton;

    private bool isReadySoundClose = true;
    private bool isReadySoundOtherButtons = true; // Для остальных кнопок
    public float ThresholdZ = -3.58037f; // Пороговое значение по оси Z

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
        // Проверяем состояние нажимных кнопок отдельно
        bool closeDoorsPressed = IsTumblerPressed(closeDoorsButton);
        bool stopKrannPressed = IsButtonPressed(stopKrannButton);

        // Проверяем остальные ненажимные кнопки
        bool otherButtonsPressed =
            //IsButtonPressed(voiceCloseButton) ||
            IsButtonPressed(voiceOpenButton) ||
            IsButtonPressed(openDoorsButton) ||
            IsButtonPressed(hornButton);
            // IsButtonPressed(stopKrannButton);

        if (closeDoorsPressed || stopKrannPressed)
        {
            if (soundSource != null && !soundSource.IsPlaying && isReadySoundClose)
            {
                soundSource.Play(); // Воспроизвести звук
            }
            isReadySoundClose = false; // Заблокировать повторный вызов до отпускания кнопки
        }
        else
        {
            isReadySoundClose = true; // Сбрасываем готовность для кнопки Close Doors
        }

        // Для других кнопок
        if (otherButtonsPressed)
        {
            if (soundSource != null && !soundSource.IsPlaying && isReadySoundOtherButtons)
            {
                soundSource.Play(); // Воспроизвести звук
                
            }
            isReadySoundOtherButtons = false; // Заблокировать повторный вызов до отпускания кнопки
        }
        else
        {
            isReadySoundOtherButtons = true; // Сбрасываем готовность для остальных кнопок
        }

        // Остановка звука, если кнопки не нажаты
        if (!closeDoorsPressed && !otherButtonsPressed)
        {
            if (soundSource != null && soundSource.IsPlaying)
            {
                soundSource.Stop(); // Остановить звук
                soundSource.Time = 0;
            }
        }
    }
}
