using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.Reflection;
using GorillaLocomotion;
using System.Collections.Generic;
using UnityEngine.XR;
using UnityEngine.Audio;
using Photon.Pun;

namespace MonkeBack
{
    [BepInPlugin("org.pepper.dash", "dash", "1.0.0.0")]
    [BepInProcess("Gorilla Tag.exe")]
    public class MonkePlugin : BaseUnityPlugin
    {
        private void Awake() => new Harmony("com.pepper.dash").PatchAll(Assembly.GetExecutingAssembly());

        [HarmonyPatch(typeof(Player))]
        [HarmonyPatch("Update")]
        private class Dash
        {
            private static GameObject Dashstart = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            private static bool RA;
            private static bool dashing;
            private static float timer;
            private static float max = 0.2f;
            private static float maxtime = 0.14f;
            private static GameObject dashTar = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            private static GameObject target = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            private static GameObject piv = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            private static GameObject faketarget = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            private static bool onfloor = false;

            private static Vector3 vdir;

            private static void Postfix(Player __instance)
            {
                if (1 != 2)
                {
                    List<InputDevice> list = new List<InputDevice>();
                    InputDevices.GetDevices(list);

                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i].characteristics.HasFlag(InputDeviceCharacteristics.Left))
                        {
                        }
                        if (list[i].characteristics.HasFlag(InputDeviceCharacteristics.Right))
                        {
                            list[i].TryGetFeatureValue(CommonUsages.primaryButton, out RA);
                        }
                    }

                    piv.GetComponent<Collider>().enabled = false;
                    piv.GetComponent<Renderer>().forceRenderingOff = true;
                    dashTar.GetComponent<Collider>().enabled = false;
                    target.GetComponent<Collider>().enabled = false;
                    target.GetComponent<Renderer>().forceRenderingOff = true;
                    dashTar.GetComponent<Renderer>().forceRenderingOff = true;
                    faketarget.GetComponent<Collider>().enabled = false;

                    Dashstart.GetComponent<Collider>().enabled = false;
                    Dashstart.GetComponent<Renderer>().forceRenderingOff = true;

                    faketarget.transform.parent = __instance.headCollider.transform;
                    faketarget.transform.localPosition = new Vector3(0, 0, 2.5f);

                    piv.transform.position = __instance.transform.position;
                    piv.transform.eulerAngles = __instance.headCollider.transform.eulerAngles;

                    dashTar.transform.parent = piv.transform;

                    dashTar.transform.localPosition = new Vector3(0, 0, 6);
                    faketarget.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

                    if (RA)
                    {
                        if (onfloor)
                        {
                            if (!dashing)
                            {
                                Dashstart.transform.position = __instance.transform.position;
                                dashing = true;
                                target.transform.position = dashTar.transform.position;

                                vdir = (target.transform.position - Dashstart.transform.position).normalized;

                            }
                            onfloor = false;
                        }
                    }

                    if (__instance.wasLeftHandTouching || __instance.wasRightHandTouching)
                    {
                        onfloor = true;
                    }

                    if (dashing)
                    {

                        faketarget.GetComponent<Renderer>().material.color = Color.white;
                        timer += Time.deltaTime;

                        __instance.bodyCollider.attachedRigidbody.velocity = new Vector3(0, 0, 0);

                        __instance.transform.position = Vector3.Lerp(Dashstart.transform.position, target.transform.position, timer / max);

                        if (__instance.IsHandTouching(forLeftHand: true) || __instance.IsHandTouching(forLeftHand: false) || timer > maxtime)
                        {
                            __instance.bodyCollider.attachedRigidbody.velocity = vdir * 9.5f;
                            dashing = false;
                        }
                    }
                    else
                    {
                        timer = 0f;

                        if (onfloor)
                        {
                            faketarget.GetComponent<Renderer>().material.color = Color.cyan;
                        }
                        else
                        {
                            faketarget.GetComponent<Renderer>().material.color = Color.red;
                        }
                    }

                    faketarget.GetComponent<Renderer>().forceRenderingOff = false;
                }
                else
                {
                    faketarget.GetComponent<Renderer>().forceRenderingOff = true;
                }
            }
        }
    }
}
