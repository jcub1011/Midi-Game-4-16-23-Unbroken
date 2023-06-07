using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MainStartMenu;

public class SongSettings : MonoBehaviour, IDocHandler
{
    #region Interface
    public void OnShow()
    {
        Debug.Log("Displaying song settings.");
    }
    public void OnHide()
    {
        Debug.Log("Hiding song settings.");
    }
    #endregion
}
