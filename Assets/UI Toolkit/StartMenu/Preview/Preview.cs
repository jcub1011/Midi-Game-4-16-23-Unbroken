using MainStartMenu;
using Melanchall.DryWetMidi.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Preview : MonoBehaviour, IDocHandler, IRunwayParamsInput
{
    #region Constants
    const string SLIDER = "TimeSlider";
    const string BACK_BUTTON = "BackButton";
    const string PREVIEW_RUNWAY_NAME = "PreviewRunway";
    #endregion

    #region Properties
    PreviewRunway _runway;
    #endregion

    #region Interface
    public void OnShow()
    {
        Debug.Log("Displaying preview.");
    }

    public void OnShow(List<NoteEvtData> notes, float endTime, float strikeBarHeight = 0.2f,
        float msLeadup = 4000f, float time = 0f)
    {
        // Init interactive elements.
        var root = DocHandler.GetRoot(Documents.Preview);

        var slider = root.Q(SLIDER) as Slider;
        slider.value = time;
        slider.lowValue = 0;
        slider.highValue = endTime + msLeadup;

        slider.RegisterValueChangedCallback(UpdatePlaybackTime);

        // Init Preview Runway.
        _runway = UnityEngine.GameObject.Find(PREVIEW_RUNWAY_NAME).GetComponent<PreviewRunway>();


        _runway.Initalize(notes, strikeBarHeight, msLeadup, time);
    }

    public void OnHide()
    {
        Debug.Log("Hiding preview.");
        _runway.Unload();
    }

    public void OnDocAdd()
    {
        Debug.Log("Preview panel added.");

        var backButton = DocHandler.GetRoot(Documents.Preview).Q(BACK_BUTTON) as Button;
        backButton.clicked += DocHandler.ReturnToPrev;
    }

    public void OnDocRemove()
    {
        Debug.Log("Preview panel removed.");
    }
    #endregion

    #region Methods
    void UpdatePlaybackTime(ChangeEvent<float> evt)
    {
        Debug.Log($"New Time: {evt.newValue}");
        _runway.UpdateTime(evt.newValue);
    }
    #endregion
}
