using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Ultrahaptics;
using UnityEngine.UI;
using Leap;
using Random = UnityEngine.Random;

public class Breeze_test : MonoBehaviour
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
    float y_max = 0.08f;
    float y;
    bool first_run;
    float z;
    float current_z;
    float angle;
    float runs;
  

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

        //Debug.Log("Control point 1: " + cp1.x + " " + cp1.y + " " + cp1.z);
        //Debug.Log("Control point 2: " + cp2.x + " " + cp2.y + " " + cp2.z);
        //Debug.Log("Control point 3: " + cp3.x + " " + cp2.y + " " + cp3.z);
        //Debug.Log("Control point 4: " + cp4.x + " " + cp4.y + " " + cp4.z);
    }

    float RandomGaussian(float minValue = 0.0f, float maxValue = 0.0f) // Marsaglia method for Normal distribution
    {
        float u, v, S;

        do
        {
            u = 2.0f * UnityEngine.Random.value - 1.0f;
            v = 2.0f * UnityEngine.Random.value - 1.0f;
            S = u * u + v * v;
        }
        while (S >= 1.0f);

        // Standard Normal Distribution
        float std = u * Mathf.Sqrt(-2.0f * Mathf.Log(S) / S);

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
        
        if(runs == 1)  // Change the direction of the breeze every 3 top to bottom flows
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
        // angle = 0; // Remove this later.
        angle = angle * Mathf.PI / 180; // Converting from degrees to radians
        // float slope = Mathf.Tan(angle);
        

        // The Leap Motion can see a hand, so get its palm position
        Leap.Vector leapPalmPosition = leap_frame.Hands[0].PalmPosition;
        Leap.Vector leapPalmNormal = leap_frame.Hands[0].PalmNormal;
        Leap.Vector leapPalmDirection = leap_frame.Hands[0].Direction;

        // Leap.Finger leapFinger = leap_frame.Hands[0].Fingers[2];
        // Leap.Vector leapFingerTipPosition = leapFinger.TipPosition;
        // Leap.Vector leapWristPosition = leap_frame.Hands[0].WristPosition;
        // y_max = leapPalmPosition.z - leapFingerTipPosition.z;
        // y_min = leapPalmPosition.z - leapWristPosition.z;
        // y_max = leapFinger.Length;

        Ultrahaptics.Vector3 device_palm_normal = new Ultrahaptics.Vector3(-leapPalmNormal.x, -leapPalmNormal.y, -leapPalmNormal.z);

        // Convert to our vector class, and then convert to our coordinate space
        Ultrahaptics.Vector3 device_palm_position = _alignment.fromTrackingPositionToDevicePosition(LeapToUHVector(leapPalmPosition));
        device_palm_normal = _alignment.fromTrackingPositionToDevicePosition(device_palm_normal).normalize();
        Ultrahaptics.Vector3 device_palm_direction = _alignment.fromTrackingPositionToDevicePosition(LeapToUHVector(leapPalmDirection)).normalize();
        
        // Ultrahaptics.Vector3 device_fingertip_position = _alignment.fromTrackingPositionToDevicePosition(LeapToUHVector(leapFingerTipPosition));
        // y_max = device_fingertip_position.x - device_palm_position.x;

        // Converting the above device space vectors to unit vectors on the palm of the hand.
        Ultrahaptics.Vector3 palm_z = device_palm_normal;
        Ultrahaptics.Vector3 palm_y = device_palm_direction;
        Ultrahaptics.Vector3 palm_x = palm_y.cross(palm_z).normalize();

        AmplitudeModulationControlPoint point_1;
        AmplitudeModulationControlPoint point_2;
        AmplitudeModulationControlPoint point_3;
        AmplitudeModulationControlPoint point_4;

        // The following lines are used to test if the the parallel translation of 45 degrees line segment works or not        
        // device_palm_position.x = 0f;
        // device_palm_position.y = 0f;   
        // device_palm_position.z = 0.15f;

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

            //Displaying the vectors on the UI canvas
            UI_build(control_point1, control_point2, control_point3, control_point4);

            point_1 = new AmplitudeModulationControlPoint(control_point1, 1.0f, 200.0f);
            point_2 = new AmplitudeModulationControlPoint(control_point2, 1.0f, 200.0f);
            point_3 = new AmplitudeModulationControlPoint(control_point3, 1.0f, 200.0f);
            point_4 = new AmplitudeModulationControlPoint(control_point4, 1.0f, 200.0f);

            // Output this point
            _emitter.update(new List<AmplitudeModulationControlPoint> {point_1, point_2, point_3, point_4});

            y = y + (translation_offset/ Mathf.Cos(angle));
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
        _leap = new Leap.Controller();
        _alignment = _emitter.getDeviceInfo().getDefaultAlignment();
        y = y_max;
        delay_1 = new WaitForSeconds(0.75f);
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
            // Debug.Log("Hands count " + _frame.Hands.Count);
            if (_frame.Hands.Count > 0)
            {
                current_z = _alignment.fromTrackingPositionToDevicePosition(LeapToUHVector(_frame.Hands[0].PalmPosition)).z;
                if (first_run == true || (Mathf.Abs(z - current_z)> 0.03f))  // Latest addition. Remove it if it doesn't work
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
