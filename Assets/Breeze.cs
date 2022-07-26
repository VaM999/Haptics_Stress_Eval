/* Author: Vamsy Malladi
 * Goal: To simulate sea breeze haptic sensation that complements the 360 video (360_Beach) that gets played in the VR scene Beach.
 * Description:
 *     Simulation: The generation of Control points and moving them along the palm. The simulation can be thought of as generating a line with 4 points (Control points) 
 *                 at an angle (this angle is assumed to follow a normal distribution with mean 0 degrees) and translating it parallely by a distance of 0.75 cm 
 *                 with an added delay of 0.1 seconds for every parallel translation. The angle corresponds to the angle made with x-axis of a preprocessed UH co-ordinate system.
 *      1. This script is attached to the "Sensation_Source" game object in the scene "Beach".
 *      2. Direction and Speed are the 2 properties of Wind amongst others that is taken into account for this simulation.
 *      3. "Gentle breeze" from Beaufort Scale has the range 12 - 19 kmph. This could not be replicated in this simulation as 
 *         moving control points at this speed range results in crackling/sputtering sounds. This is scaled down to travel 13 cm of the palm in 1.35 seconds.
 *         This is decided by trying out different speeds.
 *      4. The sensation moves from the fingers towards the bottom of the palm as this is inline with the fact that "sea breeze" blows from sea onto the land.
 *         (This is with the assumption that the user positions their hand such that the fingers are in the same direction as the leap motion camera)
 *      5. The UH co-ordinate system is preprocessed such that the y-axis corresponds to the direction of the palm and the 
 *         x-axis is the direction of the vector that is the cross product of y-axis and the z-axis (which here is the normal of the palm)
 *      5. The simulation is present in the Coroutine "breeze".
 *      6. After every simulation, a delay of 0.5 seconds is added before the next run of simulation starts.
 *      7. Simulations are restarted if the simulation that was already running ends (or) if the hand moves up or down by 3 cm.
 *      8. Simulations are stopped if the Leapmotion camera doesn't detect any hand. In case of multiple hands in the frame, 
 *         only one hand is taken as the focal point for the simulation.
 *      9. Random Gaussian number (angle with x-axis) is generated using Marsaglia polar method. The output of this is clamped 
 *         such that the value generated is within 3 "sigmas" of the mean value. (3 sigma rule - 99.7 percent of all generated values lie in this interval)
 *      10.The parallel translation of the line is handled using Linear Algebra's fundamentals in co-ordinate form.
 * Ultrahaptics & LeapMotion:
 *      1. The whole simulation uses Amplitude Modulation and emits the Control points at the highest intensity at frequency of 200 Hz.
 *      2. The simulation tracks the center of the user's palm and this acts like the origin for the co-ordinate system in which control points are moved.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Ultrahaptics;
using UnityEngine.UI;
using Leap;
using Random = UnityEngine.Random;

public class Breeze : MonoBehaviour
{
    AmplitudeModulationEmitter _emitter;
    Alignment _alignment;
    Leap.Frame _frame;
    Leap.Controller _leap;
    WaitForSeconds delay_1;
    WaitForSeconds delay_2;

    const float translation_offset = -0.0075f;
    const float y_min = -0.05f;
    readonly float[] x_offsets = { -0.025f, 0f, 0.025f, 0.05f };
    const float y_max = 0.08f;
    float y;
    bool first_run;
    float z;
    float current_z;
    float angle;
    float runs;

    // Converts a Leap Vector directly to a UH Vector3
    Ultrahaptics.Vector3 LeapToUHVector(Leap.Vector vec)
    {
        return new Ultrahaptics.Vector3(vec.x, vec.y, vec.z);
    }

    float RandomGaussian(float minValue = 0.0f, float maxValue = 0.0f) // Marsaglia method for Normal distribution
    {
        float u, v, S;

        do
        {
            u = 2.0f * UnityEngine.Random.value - 1.0f;  // UnityEngine.Random.value returns a random float between 0 and 1. 
            v = 2.0f * UnityEngine.Random.value - 1.0f;  
            S = u * u + v * v;
        }
        while (S >= 1.0f);

        // Standard Normal Distribution
        float std = u * Mathf.Sqrt(-2.0f * Mathf.Log(S) / S);  // std is a standard normal random variable's value.

        // Normal Distribution centered between the min and max value
        // and clamped following the "three-sigma rule"
        float mean = (minValue + maxValue) / 2.0f;
        float sigma = (maxValue - mean) / 3.0f;
        return Mathf.Clamp(std * sigma + mean, minValue, maxValue);
    }

    IEnumerator breeze(Leap.Frame leap_frame)
    {
        if (y < y_min)
        {
            y = y_max;
            yield return delay_1;
        }

        if (runs == 2)  // Change the direction of the breeze every 3 top-to-bottom-flows
        {
            runs = 0;
            angle = RandomGaussian(-45, 45);
        }
        else
        {
            angle = 0;
            runs++;
        }
        Debug.Log("Angle = " + angle);
        angle = angle * Mathf.PI / 180; // Converting from degrees to radians
                                        
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

        Ultrahaptics.Vector3 control_point1 = device_palm_position + (x_offsets[0] * palm_x) + (palm_y * y_max) + (x_offsets[0] * palm_y * Mathf.Sin(angle));
        Ultrahaptics.Vector3 control_point2 = device_palm_position + (x_offsets[1] * palm_x) + (palm_y * y_max) + (x_offsets[1] * palm_y * Mathf.Sin(angle));
        Ultrahaptics.Vector3 control_point3 = device_palm_position + (x_offsets[2] * palm_x) + (palm_y * y_max) + (x_offsets[2] * palm_y * Mathf.Sin(angle));
        Ultrahaptics.Vector3 control_point4 = device_palm_position + (x_offsets[3] * palm_x) + (palm_y * y_max) + (x_offsets[3] * palm_y * Mathf.Sin(angle));

        float cp1_cp2 = Mathf.Sqrt(Mathf.Pow(control_point2.x - control_point1.x, 2) + Mathf.Pow(control_point2.y - control_point1.y, 2));
        float cp3_cp4 = Mathf.Sqrt(Mathf.Pow(control_point3.x - control_point4.x, 2) + Mathf.Pow(control_point3.y - control_point4.y, 2));

        float cp1_cp2_dx = (translation_offset / cp1_cp2) * (control_point1.y - control_point2.y);
        float cp1_cp2_dy = (translation_offset / cp1_cp2) * (control_point2.x - control_point1.x);

        float cp3_cp4_dx = (translation_offset / cp3_cp4) * (control_point3.y - control_point4.y);
        float cp3_cp4_dy = (translation_offset / cp3_cp4) * (control_point4.x - control_point3.x);

        while (y >= y_min)
        {
            yield return delay_2;

            point_1 = new AmplitudeModulationControlPoint(control_point1, 1.0f, 200.0f);
            point_2 = new AmplitudeModulationControlPoint(control_point2, 1.0f, 200.0f);
            point_3 = new AmplitudeModulationControlPoint(control_point3, 1.0f, 200.0f);
            point_4 = new AmplitudeModulationControlPoint(control_point4, 1.0f, 200.0f);

            // Output this point
            _emitter.update(new List<AmplitudeModulationControlPoint> { point_1, point_2, point_3, point_4 });

            y = y + (translation_offset / Mathf.Cos(angle));

            control_point1.x = control_point1.x + cp1_cp2_dx;
            control_point2.x = control_point2.x + cp1_cp2_dx;
            control_point3.x = control_point3.x + cp3_cp4_dx;
            control_point4.x = control_point4.x + cp3_cp4_dx;

            control_point1.y = control_point1.y + cp1_cp2_dy;
            control_point2.y = control_point2.y + cp1_cp2_dy;
            control_point3.y = control_point3.y + cp3_cp4_dy;
            control_point4.y = control_point4.y + cp3_cp4_dy;

        }
        yield return delay_2;

        point_1 = new AmplitudeModulationControlPoint(device_palm_position + (0.02f * palm_x), 0.0f, 200.0f);
        point_2 = new AmplitudeModulationControlPoint(device_palm_position + (0.04f * palm_x), 0.0f, 200.0f);
        point_3 = new AmplitudeModulationControlPoint(device_palm_position + (0f * palm_x), 0.0f, 200.0f);
        point_4 = new AmplitudeModulationControlPoint(device_palm_position + (-0.02f * palm_x), 0.0f, 200.0f);

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

        // Initialize the leap controller
        _leap = new Leap.Controller();

        // Get the Coordinate system's alignment
        _alignment = _emitter.getDeviceInfo().getDefaultAlignment();
        
        // Start the breeze from the top of the palm.
        y = y_max;
        delay_1 = new WaitForSeconds(0.5f);
        delay_2 = new WaitForSeconds(0.1f);
        first_run = true;
        z = 0f;
        runs = 0;
        angle = 0;
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
                if (first_run == true || (Mathf.Abs(z - current_z) > 0.03f))  // Call the coroutine if the hand moves up or down by 3 cm
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
