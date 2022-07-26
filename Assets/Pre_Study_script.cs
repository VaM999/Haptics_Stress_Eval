/* Author: Vamsy Malladi
 * Goal: To simulate sea breeze haptic sensation for the pre study.
 * Description:
 *      Simulation: The generation of Control points and moving them along the palm with a y_axis offset of 0.75cm. 
 *                  The simulation can be thought of as generating a line with 4 points (Control points) and translating it parallely 
 *                  by a distance of 0.75 cm with an added delay of 0.1 seconds for every parallel translation.
 *      1. This script is attached to the "Haptics source" game object in the scene "Pre_Study".
 *      2. Direction and Speed are the 2 properties of Wind amongst others that is taken into account for this simulation.
 *      3. "Gentle breeze" from Beaufort Scale has the range 12 - 19 kmph. This could not be replicated in this simulation as 
 *         moving control points at this speed range results in crackling/sputtering sounds. This is scaled down to travel 13 cm of the palm in 1.35 seconds.
 *      4. The sensation moves from the fingers towards the bottom of the palm as this is inline with the fact that "sea breeze" blows from sea onto the land.
 *         (This is with the assumption that the user positions their hand such that the fingers are in the same direction as the leap motion camera)
 *      5. The simulation is present in the Coroutine "breeze".
 *      6. After every simulation, a delay of 0.5 seconds is added before the next run of simulation starts.
 *      7. A simulation is restarted if the simulation that was already running ends (or) if the hand moves up or down by 3 cm.
 *      8. Simulations are stopped if the Leapmotion camera doesn't detect any hand. In case of multiple hands in the frame, 
 *         only one hand is taken as the focal point for the simulation.
 *      9. The scene contains a canvas that displays x, y, z co-ordinates of all 4 control points and the FPS of the scene in the top right corner.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Ultrahaptics;
using UnityEngine.UI;
using Leap;
using Random = UnityEngine.Random;

public class Pre_Study_script : MonoBehaviour
{
    AmplitudeModulationEmitter _emitter;
    Alignment _alignment;
    Leap.Frame _frame;
    Leap.Controller _leap;
    WaitForSeconds delay_1;

    const float translation_offset = -0.0075f;
    const float y_min = -0.05f;
    readonly float[] x_offsets = { -0.025f, 0f, 0.025f, 0.05f };
    float y_max = 0.08f;
    float y;
    bool first_run;
    float z;
    float current_z;
 
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

    // Converts a Leap Vector directly to a UH Vector3
    Ultrahaptics.Vector3 LeapToUHVector(Leap.Vector vec)
    {
        return new Ultrahaptics.Vector3(vec.x, vec.y, vec.z);
    }

    void UI_build(Ultrahaptics.Vector3 cp1, Ultrahaptics.Vector3 cp2, Ultrahaptics.Vector3 cp3, Ultrahaptics.Vector3 cp4)
    {
        Control_point_1_x.text = "" + cp1.x;
        Control_point_1_y.text = "" + cp1.y;
        Control_point_1_z.text = "" + cp1.z;

        Control_point_2_x.text = "" + cp2.x;
        Control_point_2_y.text = "" + cp2.y;
        Control_point_2_z.text = "" + cp2.z;

        Control_point_3_x.text = "" + cp3.x;
        Control_point_3_y.text = "" + cp3.y;
        Control_point_3_z.text = "" + cp3.z;

        Control_point_4_x.text = "" + cp4.x;
        Control_point_4_y.text = "" + cp4.y;
        Control_point_4_z.text = "" + cp4.z;
    }

    IEnumerator breeze(Leap.Frame leap_frame)
    {
        if (y < y_min)
        {
            y = y_max;
            yield return delay_1;
        }

        // The Leap Motion can see a hand, so get its palm position
        Leap.Vector leapPalmPosition = leap_frame.Hands[0].PalmPosition;
        Leap.Vector leapPalmNormal = leap_frame.Hands[0].PalmNormal;
        Leap.Vector leapPalmDirection = leap_frame.Hands[0].Direction;

        Ultrahaptics.Vector3 device_palm_normal = new Ultrahaptics.Vector3(-leapPalmNormal.x, -leapPalmNormal.y, -leapPalmNormal.z);

        // Convert to our vector class, and then convert to our coordinate space
        Ultrahaptics.Vector3 device_palm_position = _alignment.fromTrackingPositionToDevicePosition(LeapToUHVector(leapPalmPosition));
        device_palm_normal = _alignment.fromTrackingPositionToDevicePosition(device_palm_normal).normalize();
        Ultrahaptics.Vector3 device_palm_direction = _alignment.fromTrackingPositionToDevicePosition(LeapToUHVector(leapPalmDirection)).normalize();

        // Converting the above device space vectors to unit vectors on the palm of the hand.
        Ultrahaptics.Vector3 palm_z = device_palm_normal;
        Ultrahaptics.Vector3 palm_y = device_palm_direction;
        Ultrahaptics.Vector3 palm_x = palm_y.cross(palm_z).normalize();

        AmplitudeModulationControlPoint point_1;
        AmplitudeModulationControlPoint point_2;
        AmplitudeModulationControlPoint point_3;
        AmplitudeModulationControlPoint point_4;

        while (y >= y_min)
        {

            Ultrahaptics.Vector3 control_point1 = device_palm_position + (x_offsets[0] * palm_x) + (palm_y * y);
            Ultrahaptics.Vector3 control_point2 = device_palm_position + (x_offsets[1] * palm_x) + (palm_y * y);
            Ultrahaptics.Vector3 control_point3 = device_palm_position + (x_offsets[2] * palm_x) + (palm_y * y);
            Ultrahaptics.Vector3 control_point4 = device_palm_position + (x_offsets[3] * palm_x) + (palm_y * y);


            //Displaying the vectors on the UI canvas
            UI_build(control_point1, control_point2, control_point3, control_point4);

            point_1 = new AmplitudeModulationControlPoint(control_point1, 1.0f, 200.0f);
            point_2 = new AmplitudeModulationControlPoint(control_point2, 1.0f, 200.0f);
            point_3 = new AmplitudeModulationControlPoint(control_point3, 1.0f, 200.0f);
            point_4 = new AmplitudeModulationControlPoint(control_point4, 1.0f, 200.0f);

            // Output this point
            _emitter.update(new List<AmplitudeModulationControlPoint> { point_1, point_2, point_3, point_4 });

            y = y + (translation_offset);
        }
        point_1 = new AmplitudeModulationControlPoint(device_palm_position + (0.022f * palm_x), 0.0f, 200.0f);
        point_2 = new AmplitudeModulationControlPoint(device_palm_position + (0.045f * palm_x), 0.0f, 200.0f);
        point_3 = new AmplitudeModulationControlPoint(device_palm_position + (0f * palm_x), 0.0f, 200.0f);
        point_4 = new AmplitudeModulationControlPoint(device_palm_position + (-0.022f * palm_x), 0.0f, 200.0f);

        // Output this point
        _emitter.update(new List<AmplitudeModulationControlPoint> { point_1, point_2, point_3, point_4 });
        first_run = true;
    }


    // Start is called before the first frame update
    void Start()
    {
        // Initialize the emitter
        _emitter = new AmplitudeModulationEmitter();
        _emitter.initialize();
        _leap = new Leap.Controller();
        _alignment = _emitter.getDeviceInfo().getDefaultAlignment();
        y = y_max;
        delay_1 = new WaitForSeconds(0.5f);
        first_run = true;
        z = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        if (_leap.IsConnected)
        {
            _frame = _leap.Frame();
            if (_frame.Hands.Count > 0)
            {
                current_z = _alignment.fromTrackingPositionToDevicePosition(LeapToUHVector(_frame.Hands[0].PalmPosition)).z;
                if (first_run == true || (Mathf.Abs(z - current_z) > 0.03f))  // Latest addition. Remove it if it doesn't work
                {
                    first_run = false;
                    StopAllCoroutines();
                    StartCoroutine(breeze(_frame));
                    z = _alignment.fromTrackingPositionToDevicePosition(LeapToUHVector(_frame.Hands[0].PalmPosition)).z;
                }
            }
            else
            {
                Debug.LogWarning("No hands detected");
                _emitter.stop();
                StopCoroutine("breeze");
                first_run = true;
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