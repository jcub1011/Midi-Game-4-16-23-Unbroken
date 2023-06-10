using MainStartMenu;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Preview : MonoBehaviour, IDocHandler
{
    #region Interface
    public void OnShow()
    {
        Debug.Log("Displaying preview.");
    }

    public void OnHide()
    {
        Debug.Log("Hiding preview.");
    }

    public void OnDocAdd()
    {
        Debug.Log("Preview panel added.");
    }

    public void OnDocRemove()
    {
        Debug.Log("Preview panel removed.");
    }
    #endregion
}
