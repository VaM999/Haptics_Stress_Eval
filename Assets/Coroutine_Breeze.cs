/* Author: Vamsy Malladi
 * Goal: To simulate sea breeze haptic sensation for the pre study.
 * Description:
 *      1. This script is attached to the "Haptics source" game object in the scene "Pre_Study"
 *      2. "Gentle breeze" from Beaufort Scale has the range 12 - 19 kmph. 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 */


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Ultrahaptics;
using UnityEngine.UI;
using Leap;
using Random = UnityEngine.Random;

public class Coroutine_Breeze : MonoBehaviour
{
    AmplitudeModulationEmitter _emitter;
    Alignment _alignment;
    Leap.Controller _leap;
    WaitForSeconds delay;

    const float y_offset = 0.005f;
    const float y_min = -0.045f;
    const float y_max = 0.045f;
    float y;

    public Text Control_point_1_x;
    public Text Control_point_1_y;
    public Text Control_point_1_z;

    public Text Control_point_2_x;
    public Text Control_point_2_y;
    public Text Control_point_2_z;

    public Text Control_point_3_x;
    public Text Control_point_3_y;
    public Text Control_point_3_z;

    public Text Control_point_4_x;
    public Text Control_point_4_y;
    public Text Control_point_4_z;

    // Start is called before the first frame update
    void Start()
    {
        // Initialize the emitter
        _emitter = new AmplitudeModulationEmitter();
        _emitter.initialize();
        _leap = new Leap.Controller();
        _alignment = _emitter.getDeviceInfo().getDefaultAlignment();
        y = y_max;
        delay = new WaitForSeconds(5);
    }


    // Converts a Leap Vector directly to a UH Vector3
    Ultrahaptics.Vector3 LeapToUHVector(Leap.Vector vec)
    {
        return new Ultrahaptics.Vector3(vec.x, vec.y, vec.z);
    }

    void UI_build(Ultrahaptics.Vector3 palm_x, Ultrahaptics.Vector3 palm_y, Ultrahaptics.Vector3 palm_center, float offset)
    {
        Control_point_1_x.text = "" + (0.02f * palm_x).x;
        Control_point_1_y.text = "" + (palm_y * offset).y;
        Control_point_1_z.text = "" + palm_center.z;

        Control_point_2_x.text = "" + (0.04f * palm_x).x;
        Control_point_2_y.text = "" + (palm_y * offset).y;
        Control_point_2_z.text = "" + palm_center.z;

        Control_point_3_x.text = "" + (0f * palm_x).x;
        Control_point_3_y.text = "" + (palm_y * offset).y;
        Control_point_3_z.text = "" + palm_center.z;

        Control_point_4_x.text = "" + (-0.02f * palm_x).x;
        Control_point_4_y.text = "" + (palm_y * offset).y;
        Control_point_4_z.text = "" + palm_center.z;
    }

    IEnumerator breeze(Leap.Frame frame)
    {
        // wind_speed = Random.range(12.0, 19.0); This is commented out as this approach of taking Beaufort scale's wind speed is scrapped.
        // This approach results in wind speeds in the order of 3 meters/second which would be too high to feel the haptic sensation.

        // The Leap Motion can see a hand, so get its palm position
        Leap.Vector leapPalmPosition = frame.Hands[0].PalmPosition;
        Leap.Vector leapPalmNormal = frame.Hands[0].PalmNormal;
        Leap.Vector leapPalmDirection = frame.Hands[0].Direction;

        Ultrahaptics.Vector3 device_palm_normal = new Ultrahaptics.Vector3(-leapPalmNormal.x, -leapPalmNormal.y, -leapPalmNormal.z);

        // Text UI element to display x, y, z co-ordinates of the center of the palm
        // normal_vector_display.text = "Co-ordinates x = " + device_palm_normal.x + "y = " + device_palm_normal.y + "z = " + device_palm_normal.z;

        // Convert to our vector class, and then convert to our coordinate space
        Ultrahaptics.Vector3 device_palm_position = _alignment.fromTrackingPositionToDevicePosition(LeapToUHVector(leapPalmPosition));
        device_palm_normal = _alignment.fromTrackingPositionToDevicePosition(device_palm_normal).normalize();
        Ultrahaptics.Vector3 device_palm_direction = _alignment.fromTrackingPositionToDevicePosition(LeapToUHVector(leapPalmDirection)).normalize();

        // Converting the above device space vectors to unit vectors on the palm of the hand.
        Ultrahaptics.Vector3 palm_z = device_palm_normal;
        Ultrahaptics.Vector3 palm_y = device_palm_direction;
        Ultrahaptics.Vector3 palm_x = palm_y.cross(palm_z).normalize();

        while (y >= y_min)
        {

            // Create a control point object using this position, 
            // with full intensity, at 200Hz
            Ultrahaptics.Vector3 control_point1 = device_palm_position + (0.02f * palm_x) + (palm_y * y);
            Ultrahaptics.Vector3 control_point2 = device_palm_position + (0.04f * palm_x) + (palm_y * y);
            Ultrahaptics.Vector3 control_point3 = device_palm_position + (0f * palm_x) + (palm_y * y);
            Ultrahaptics.Vector3 control_point4 = device_palm_position + (-0.02f * palm_x) + (palm_y * y);

            //Displaying the vectors on the UI canvas
            UI_build(palm_x, palm_y, device_palm_position, y);

            AmplitudeModulationControlPoint point_1 = new AmplitudeModulationControlPoint(control_point1, 1.0f, 200.0f);
            AmplitudeModulationControlPoint point_2 = new AmplitudeModulationControlPoint(control_point2, 1.0f, 200.0f);
            AmplitudeModulationControlPoint point_3 = new AmplitudeModulationControlPoint(control_point3, 1.0f, 200.0f);
            AmplitudeModulationControlPoint point_4 = new AmplitudeModulationControlPoint(control_point4, 1.0f, 200.0f);

            // Output this point
            _emitter.update(new List<AmplitudeModulationControlPoint> { point_1, point_2, point_3, point_4 });

            y = y - y_offset;

            //if (y < y_min)
            //{
            //    y = y_max;
            //}
            yield return delay;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (_leap.IsConnected)
        {
            var frame = _leap.Frame();
            if (frame.Hands.Count > 0)
            {
                if (y < y_min)
                {
                    y = y_max;
                    Debug.Log("y is " + y);
                    StopCoroutine("breeze");
                }
                StartCoroutine(breeze(frame));
            }
            else
            {
                Debug.LogWarning("No hands detected");
                _emitter.stop();
                StopCoroutine("breeze");
            }
        }
        else
        {
            Debug.LogWarning("No Leap connected");
            _emitter.stop();
            StopCoroutine("breeze");
        }
    }
    // Ensure the emitter is stopped on exit
    void OnDisable()
    {
        StopCoroutine("breeze");
        _emitter.stop();
    }

    // Ensure the emitter is immediately disposed when destroyed
    void OnDestroy()
    {
        StopCoroutine("breeze");
        _emitter.Dispose();
        _emitter = null;
        _alignment.Dispose();
        _alignment = null;
    }
}
