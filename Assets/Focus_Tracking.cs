using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Ultrahaptics;
using Leap;

public class Focus_Tracking : MonoBehaviour
{
    AmplitudeModulationEmitter _emitter;
    Alignment _alignment;
    Leap.Controller _leap;

    // Start is called before the first frame update
    void Start()
    {
        // Initialize the emitter
        _emitter = new AmplitudeModulationEmitter();
        _emitter.initialize();
        _leap = new Leap.Controller();

        _alignment = _emitter.getDeviceInfo().getDefaultAlignment();
    }

    // Converts a Leap Vector directly to a UH Vector3
    Ultrahaptics.Vector3 LeapToUHVector(Leap.Vector vec)
    {
        return new Ultrahaptics.Vector3(vec.x, vec.y, vec.z);
    }


    // Update is called once per frame
    void Update()
    {

        if (_leap.IsConnected)
        {
            var frame = _leap.Frame();
            if (frame.Hands.Count > 0)
            {
                // The Leap Motion can see a hand, so get its palm position
                Leap.Vector leapPalmPosition = frame.Hands[0].PalmPosition;

                // Convert to our vector class, and then convert to our coordinate space
                Ultrahaptics.Vector3 uhPalmPosition = _alignment.fromTrackingPositionToDevicePosition(LeapToUHVector(leapPalmPosition));

                // Create a control point object using this position, 
                // with full intensity, at 200Hz
                AmplitudeModulationControlPoint point = new AmplitudeModulationControlPoint(uhPalmPosition, 1.0f, 200.0f);

                // Output this point
                _emitter.update(new List<AmplitudeModulationControlPoint> { point });
            }
            else
            {
                Debug.LogWarning("No hands detected");
                _emitter.stop();
            }
        }
        else
        {
            Debug.LogWarning("No Leap connected");
            _emitter.stop();
        }
    }
    // Ensure the emitter is stopped on exit
    void OnDisable()
    {
        _emitter.stop();
    }

    // Ensure the emitter is immediately disposed when destroyed
    void OnDestroy()
    {
        _emitter.Dispose();
        _emitter = null;
        _alignment.Dispose();
        _alignment = null;
    }
}
