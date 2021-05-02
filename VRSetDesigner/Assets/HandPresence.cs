using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class HandPresence : MonoBehaviour
{

    public InputDeviceCharacteristics controllerCharacteristics;
    public List<GameObject> controllerPrefabs;
    private InputDevice targetDevice;
    private GameObject spawnedController;

    // Start is called before the first frame update
    void Start()
    {
        // Gets a list of all connected devices
        List<InputDevice> devices = new List<InputDevice>();
        // FIXME: remove later, for debugging
        foreach (var item in devices) {
            Debug.Log(item.name + " " + item.characteristics);
        }

        // Changes the devices list to just include right-hand controllers
        // InputDeviceCharacteristics rightControllerCharacteristics = InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller;
        
        InputDevices.GetDevicesWithCharacteristics(controllerCharacteristics, devices);

        // FIXME: remove later, for debugging
        foreach (var item in devices) {
            // make sure this only prints once, then we can just do:
            // targetDevice = devices[0];
            Debug.Log(item.name + " " + item.characteristics);
            targetDevice = item;
            GameObject prefab = controllerPrefabs.Find(controller => controller.name == targetDevice.name);
            if(prefab) {
                spawnedController = Instantiate(prefab, transform);
            } else {
                Debug.LogError("Did not find corresponding controller model");
                // just set it to the right controller
                spawnedController = Instantiate(controllerPrefabs[0], transform);
            }
            Debug.Log("BOOOOOOP" + spawnedController.name);
        }
    }

    // Update is called once per frame
    void Update()
    {
        /**
         * The input features work like this:
         * Boolean: buttons are either clicked or not
         * Float: axis values are between 0 and 1, like for the trigger based on pressure
         * Vector2: movement on two axis values like for the touchpad moving between 4 quadrants
         */
        targetDevice.TryGetFeatureValue(CommonUsages.primaryButton, out bool primaryButtonValue);
        if(primaryButtonValue) {
            Debug.Log("Pressing Primary Button!");
        }

        targetDevice.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue);
        if(triggerValue > 0.1f) {
            Debug.Log("Trigger pressed " + triggerValue + "!");
        }

        targetDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 primary2DAxisValue);
        if(primary2DAxisValue != Vector2.zero) {
            Debug.Log("Primary Touchpad " + primary2DAxisValue);
        }
    }
}
