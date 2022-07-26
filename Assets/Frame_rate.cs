using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Frame_rate : MonoBehaviour
{
    public Text fpsDisplay;

    // Start is called before the first frame update
    void Start()
    {
    }

// Update is called once per frame
void Update()
    {
        float fps = 1 / Time.unscaledDeltaTime;
        fpsDisplay.text = "" + fps;
    }
}
