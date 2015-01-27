﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace ABS
{
    public class ABSModule : PartModule
    {
        float timeSinceToggle = 0;
        float timeSinceKeyDown = 0;
        Boolean running = false;
        Boolean runUntilStop = false;
        Boolean keyDown = false;
        String currentText = "0.1";
        double currentRate = 0.1;
        protected Rect windowPos;
        private static Texture2D texture;
        private ApplicationLauncherButton brakeButton;

        //IButton btn;

        private float powerRatio;


        [KSPField(isPersistant = false)]
        public float PowerConsumption;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Status")]
        public string Status;

        public override string GetInfo()
        {
            string i = base.GetInfo();
            print(i);
            i += "Power Consumption: " + PowerConsumption + "/sec";
            return i;
        }

        public override void OnStart(StartState state)
        {

            print("ABS: Hello Kerbin!");
            //btn = InitButton();

            if (texture == null)
            {
                texture = new Texture2D(36, 36, TextureFormat.RGBA32, false);
                texture.LoadImage(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "StockToolbar.png")));
            }

            if ((windowPos.x == 0) && (windowPos.y == 0))
            {
                windowPos = new Rect(Screen.width / 2, Screen.height / 2, 100, 10);
            }

            if (this.brakeButton == null && HighLogic.LoadedScene == GameScenes.FLIGHT)
            {
                this.brakeButton = ApplicationLauncher.Instance.AddModApplication(
                    this.Activate,
                    this.Deactivate,
                    null,
                    null,
                    null,
                    null,
                    ApplicationLauncher.AppScenes.ALWAYS,
                    texture);
            } 
        }

        public override void OnFixedUpdate()
        {
            if (running)
            {
                var energyRequest = PowerConsumption * TimeWarp.fixedDeltaTime;
                var energyDrawn = part.RequestResource("ElectricCharge", energyRequest);
                powerRatio = energyDrawn / energyRequest;
            }
        }

        public override void OnUpdate()
        {
            if ((GameSettings.BRAKES.GetKeyDown() && GameSettings.MODIFIER_KEY.GetKeyDown()) || (GameSettings.BRAKES.GetKey() && GameSettings.MODIFIER_KEY.GetKeyDown()) || (GameSettings.BRAKES.GetKeyDown() && GameSettings.MODIFIER_KEY.GetKey()))
            {
                print("ABS KEYDOWN");
                keyDown = true;
                timeSinceKeyDown = 0;
            }

            if (keyDown)
            {
                timeSinceKeyDown += TimeWarp.deltaTime;
            }

            if ((GameSettings.BRAKES.GetKeyUp() || GameSettings.MODIFIER_KEY.GetKeyUp()) && keyDown)
            {
                print("ABS KEYUP");
                keyDown = false;
                if (timeSinceKeyDown <= 1)
                {
                    print("ABS PRESS");
                    if (runUntilStop)
                    {
                        print("ABS UNTOGGLE");
                        this.brakeButton.SetFalse();
                        //Deactivate();
                    }
                    else
                    {
                        this.brakeButton.SetTrue();
                        //Activate();

                    }
                }
            }



            if (GameSettings.BRAKES.GetKey() && GameSettings.MODIFIER_KEY.GetKey())
            {
                print("ABS KEYS DOWN");
                running = true;
            }
            else if (!runUntilStop)
            {
                running = false;
            }




            if (running)
            {
                if (powerRatio > 0)
                {
                    Status = "Active";
                    timeSinceToggle += TimeWarp.deltaTime;
                    print("ABS: " + timeSinceToggle.ToString());
                    if (timeSinceToggle >= currentRate)
                    {
                        vessel.ActionGroups.ToggleGroup(KSPActionGroup.Brakes);
                        print("ABS: Toggle Brakes");
                        timeSinceToggle = 0;
                    }
                }
                else
                {
                    Status = "Out of Power!";
                }
            }
            else
            {
                Status = "Idle";
            }

            if (runUntilStop && vessel.GetSrfVelocity().magnitude <= 1)
            {
                print("ABS: TOO SLOW");
                //print("ABS: SPEED: " + vessel.GetSrfVelocity().magnitude.ToString());
                this.brakeButton.SetFalse();
                //Deactivate();
                vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, true);
            }
        }

        private bool GetPower(Part part, float PowerConsumption)
        {
            if (TimeWarp.deltaTime != 0)
            {
                float amount = part.RequestResource("ElectricCharge", PowerConsumption * TimeWarp.deltaTime);
                return amount != 0;
            }
            else
            {
                return true;
            }
        }

        [KSPEvent(guiActive = true, guiName = "Activate ABS Stop")]
        public void Activate()
        {
            print("ABS: ABS Started");
            running = true;
            runUntilStop = true;
            Events["Activate"].active = false;
            Events["Deactivate"].active = true;
            //SetBarIcon(btn);
        }

        [KSPEvent(guiActive = true, guiName = "Deactivate ABS Stop", active = false)]
        public void Deactivate()
        {
            print("ABS: ABS Stopped");
            running = false;
            runUntilStop = false;
            Events["Activate"].active = true;
            Events["Deactivate"].active = false;
            //SetBarIcon(btn);
        }
        [KSPEvent(guiActive = true, guiName = "Edit ABS Settings")]
        private void openGUI()
        {
            RenderingManager.AddToPostDrawQueue(3, new Callback(drawGUI));
        }

        private void WindowGUI(int windowID)
        {
            GUIStyle sty = new GUIStyle(GUI.skin.button);
            GUIStyle sty2 = new GUIStyle(GUI.skin.textField);

            sty.normal.textColor = sty.focused.textColor = Color.white;
            sty.hover.textColor = sty.active.textColor = Color.yellow;
            sty.onNormal.textColor = sty.onFocused.textColor = sty.onHover.textColor = sty.onActive.textColor = Color.green;
            sty.padding = new RectOffset(8, 8, 8, 8);

            if (!Double.TryParse(currentText, out currentRate))
            {
                currentRate = 0.1;
            }

            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label("ABS Cycle Rate");
            currentText = GUILayout.TextField(currentText, GUILayout.MinWidth(30.0F));
            GUILayout.Label("s");
            GUILayout.EndHorizontal();
            if (GUILayout.Button("CLOSE", sty, GUILayout.ExpandWidth(true)))
            {
                closeGUI();
            }
            GUILayout.EndVertical();

            GUI.DragWindow();
        }

        private void drawGUI()
        {
            GUI.skin = HighLogic.Skin;
            windowPos = GUILayout.Window(1, windowPos, WindowGUI, "ABS Options", GUILayout.MinWidth(100));
        }

        private void closeGUI()
        {
            RenderingManager.RemoveFromPostDrawQueue(3, new Callback(drawGUI));
        }
    }
}
