using BepInEx;
using System;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using System.Reflection;
using GorillaLocomotion;
using System.Collections.Generic;
using UnityEngine.XR;
using UnityEngine.Audio;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Threading;
using System.Threading.Tasks;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using System.Collections;
using Photon.Pun;
using System.IO;

namespace DashMonke
{
    [BepInPlugin("org.pepper.MonkeDash.pepperclient", "MonkeDash", "2.0.0.1")]
    [BepInProcess("Gorilla Tag.exe")]
    public class Patching : BaseUnityPlugin
    {
        private void Awake() => new Harmony("com.pepper.MonkeDash.pepperclient").PatchAll(Assembly.GetExecutingAssembly());

        [HarmonyPatch(typeof(GorillaLocomotion.Player))]
        [HarmonyPatch("Update")]

        private class Dash
        {

            //HeadDirection for aiming
                private static Vector3 HeadDirection;

            //Floats to control the dash
                private static float DashSpeed = 20;
                private static float DashExit = 12;
                private static float MaxTime = 0.1f;
                private static float Timer = 0;

            //Dash Bools
                private static bool CanDash = true;
                private static bool Dashing = false;
                private static bool OnDash = false;

                private static bool OnStart;

                private static bool AButton;

                private static bool ControlledMode = false;

            //Config variables
                private static ConfigEntry<float> MT;
                private static ConfigEntry<float> DS;
                private static ConfigEntry<float> DE;
                private static ConfigEntry<bool> Cntrl;
                private static ConfigEntry<bool> Show;

                private static ConfigEntry<string> ND;
                private static ConfigEntry<string> CD;
                private static ConfigEntry<string> DC;

            //Indicator Settings
                private static GameObject Indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                private static bool Indicate = true;

                private static Color DashingCol = Color.white;
                private static Color NoDashCol = Color.red;
                private static Color CanDashCol = Color.blue;
            private static void Postfix(GorillaLocomotion.Player __instance)
            {
                if (!OnStart)
                {
                    var file = new ConfigFile(Path.Combine(Paths.ConfigPath, "DashMonke.cfg"), true);
                    MT = file.Bind("DashMonke-Settings", "DashTime", 0.1f, "The ammount of time the dash lasts");
                    MaxTime = MT.Value;
                    DS = file.Bind("DashMonke-Settings", "DashSpeed", 20f, "How fast you go during the dash");
                    DashSpeed = DS.Value;
                    DE = file.Bind("DashMonke-Settings", "ExitSpeed", 12f, "How fast you go at the end of the dash");
                    DashExit = DE.Value;
                    Cntrl = file.Bind("DashMonke-Settings", "Controll-Mode", false, "If enabled allows you to change the direction of a dash during it");
                    ControlledMode = Cntrl.Value;
                    Show = file.Bind("DashMonke-Settings", "Show-Indicator", true, "Shows a sphere in the center of your vision to help you aim - as well as telling you when you can dash");
                    Indicate = Show.Value;
                    CD = file.Bind("Indicator-settings", "Can-Dash-Colour", "000-000-255", "The color the indicator will be when you can dash");
                    CanDashCol = StringToCol(CD.Value);
                    ND = file.Bind("Indicator-settings", "Can-Not-Dash-Colour", "255-000-000", "The color the indicator will be when you can't dash");
                    NoDashCol = StringToCol(ND.Value);
                    DC = file.Bind("Indicator-settings", "Dashing-Colour", "255-255-255", "The color the indicator will be when you are dashing");
                    DashingCol = StringToCol(DC.Value);
                    OnStart = true;
                }

                List<InputDevice> list = new List<InputDevice>();
                InputDevices.GetDevices(list);

                for (int i = 0; i < list.Count; i++) //Get input
                {
                    if (list[i].characteristics.HasFlag(InputDeviceCharacteristics.Left))
                    {
                    }
                    if (list[i].characteristics.HasFlag(InputDeviceCharacteristics.Right))
                    {
                        list[i].TryGetFeatureValue(CommonUsages.primaryButton, out AButton);
                    }
                }

                if (!PhotonNetwork.CurrentRoom.IsVisible)
                {
                    Indicator.GetComponent<Collider>().enabled = false;

                    if (Indicate)
                    {
                        Indicator.GetComponent<Renderer>().forceRenderingOff = false;

                        var layers = (1 << 9);
                        RaycastHit IndicatorRay;
                        if(Physics.Raycast(__instance.headCollider.transform.position, __instance.headCollider.transform.forward, out IndicatorRay, 2f, layers))
                        {
                            Indicator.transform.localScale = new Vector3(0.075f, 0.075f, 0.075f) * Vector3.Distance(__instance.headCollider.transform.position, IndicatorRay.point);
                            Indicator.transform.position = IndicatorRay.point;
                        }
                        else
                        {
                            Indicator.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
                            Indicator.transform.position = __instance.headCollider.transform.position + (__instance.headCollider.transform.forward * 2);
                        }
                    }
                    else
                    {
                        Indicator.GetComponent<Renderer>().forceRenderingOff = true;
                    }

                    if (AButton && !Dashing && CanDash && !__instance.disableMovement)
                    {
                        Dashing = true;
                    }

                    if (Dashing)
                    {
                        Indicator.GetComponent<Renderer>().material.color = DashingCol;

                        CanDash = false;

                        if (!OnDash || ControlledMode)
                        {
                            HeadDirection = __instance.headCollider.transform.forward;
                            OnDash = true;
                        }

                        __instance.bodyCollider.attachedRigidbody.velocity = HeadDirection * DashSpeed;

                        Timer += Time.deltaTime;

                        if (Timer > MaxTime)
                        {
                            Dashing = false;
                        }
                    }
                    else
                    {
                        if (OnDash)
                        {
                            __instance.bodyCollider.attachedRigidbody.velocity = HeadDirection * DashExit;
                            Timer = 0;
                            OnDash = false;
                        }

                        if (__instance.disableMovement)
                        {
                            Dashing = false;
                            CanDash = false;
                            Indicator.GetComponent<Renderer>().material.color = NoDashCol;
                        }
                        else if (CanDash)
                        {
                            Indicator.GetComponent<Renderer>().material.color = CanDashCol;
                        }
                        else
                        {
                            Indicator.GetComponent<Renderer>().material.color = NoDashCol;
                        }
                    }

                    if (__instance.wasLeftHandTouching || __instance.wasRightHandTouching)
                    {
                        CanDash = true;
                    }
                }
                else
                {
                    Indicator.GetComponent<Renderer>().forceRenderingOff = true;
                }
            }

            public static Color StringToCol(string col)
            {
                Color ReturnCol = Color.black;
                char seperator = col[3];

                var split = col.Split(seperator);

                float R = float.Parse(split[0]);
                float G = float.Parse(split[1]);
                float B = float.Parse(split[2]);

                ReturnCol = new Color(R / 255.0f, G / 255.0f, B / 255.0f);

                Console.WriteLine(col + " - " + ReturnCol);

                return ReturnCol;
            }
        }
    }
}
