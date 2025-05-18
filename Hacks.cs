using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

namespace RaveHack
{
    public class Hacks : MonoBehaviour
    {
        public float maxDistance = 1000f;
        public bool showBoxESP = true;
        public bool showNameESP = true;
        public bool showDistanceESP = true;
        public bool enableESP = true;

        public bool teamCheck = true;

        public bool enableSpeedHack = false;
        public float InjectedSpeedMultiplier = 5f;

        public bool enableHealthHack = false;
        public float InjectedHealthHack = 50000f;


        public float flySpeed = 10f;

        public Color espColor = Color.red;

        private GUIStyle labelStyle;

        void Start()
        {
            labelStyle = new GUIStyle();
            labelStyle.normal.textColor = Color.white;
            labelStyle.fontSize = 12;
        }

        void Update()
        {
            var player = LocalPlayer.actor;

            if (enableSpeedHack && player != null && player.controller != null)
                player.speedMultiplier = InjectedSpeedMultiplier;

            if (enableHealthHack && player != null)
                player.health = InjectedHealthHack;
        }

        void OnGUI()
        {
            DrawGUI();
            DrawESP();
        }



        private Vector2 scrollPos;

        void DrawGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 260, 500), GUI.skin.box);
            scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Width(250), GUILayout.Height(480));

            GUILayout.Label("RaveHack");

            enableESP = GUILayout.Toggle(enableESP, "Enable ESP");

            if (enableESP)
            {
                showBoxESP = GUILayout.Toggle(showBoxESP, "Box ESP");
                showNameESP = GUILayout.Toggle(showNameESP, "Name ESP");
                showDistanceESP = GUILayout.Toggle(showDistanceESP, "Distance ESP");
                teamCheck = GUILayout.Toggle(teamCheck, "Team Check");

                GUILayout.Space(10);
                GUILayout.Label("ESP Color:");
                espColor = RGBSlider(espColor);

                GUILayout.Space(10);
                maxDistance = GUILayout.HorizontalSlider(maxDistance, 50f, 3000f);
                GUILayout.Label($"ESP Distance: {Mathf.RoundToInt(maxDistance)}m");

            }





            GUILayout.Space(10);
            enableSpeedHack = GUILayout.Toggle(enableSpeedHack, "Speed Hack");
            if (enableSpeedHack)
            {
                InjectedSpeedMultiplier = GUILayout.HorizontalSlider(InjectedSpeedMultiplier, 1f, 10f);
                GUILayout.Label($"Speed Multiplier: {InjectedSpeedMultiplier:F1}");
            }

            enableHealthHack = GUILayout.Toggle(enableHealthHack, "Infinite Health");

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }


        Color RGBSlider(Color c)
        {
            c.r = GUILayout.HorizontalSlider(c.r, 0, 1);
            c.g = GUILayout.HorizontalSlider(c.g, 0, 1);
            c.b = GUILayout.HorizontalSlider(c.b, 0, 1);
            return c;
        }

        void DrawESP()
        {
            if (!enableESP) return;

            var localActor = ActorManager.instance?.player;
            if (localActor == null) return;

            foreach (var actor in ActorManager.instance.actors)
            {
                if (actor == null || actor.dead || actor == localActor) continue;
                if (teamCheck && actor.team == localActor.team) continue;

                float distance = Vector3.Distance(localActor.transform.position, actor.transform.position);
                if (distance > maxDistance) continue;

                Vector3 bottom = actor.transform.position;
                Vector3 top = bottom + Vector3.up * 2.0f;

                Vector3 screenBottom = Camera.main.WorldToScreenPoint(bottom);
                Vector3 screenTop = Camera.main.WorldToScreenPoint(top);

                if (screenBottom.z < 0 || screenTop.z < 0) continue;

                screenBottom.y = Screen.height - screenBottom.y;
                screenTop.y = Screen.height - screenTop.y;

                float height = screenBottom.y - screenTop.y;
                float width = height / 2;

                if (showBoxESP)
                    DrawBox(screenTop.x - width / 2, screenTop.y, width, height, espColor);

                if (showNameESP)
                {
                    var namePos = new Vector2(screenTop.x, screenTop.y - 15);
                    DrawLabel(namePos, actor.name, espColor);
                }

                if (showDistanceESP)
                {
                    var distPos = new Vector2(screenBottom.x, screenBottom.y + 5);
                    DrawLabel(distPos, $"{distance:F1}m", espColor);
                }
            }
        }

        void DrawLabel(Vector2 position, string text, Color color)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.normal.textColor = color;
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = 12;
            GUI.Label(new Rect(position.x - 50, position.y, 100, 20), text, style);
        }



        void DrawBox(float x, float y, float w, float h, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(x, y, w, 1), Texture2D.whiteTexture); // Top
            GUI.DrawTexture(new Rect(x, y + h, w, 1), Texture2D.whiteTexture); // Bottom
            GUI.DrawTexture(new Rect(x, y, 1, h), Texture2D.whiteTexture); // Left
            GUI.DrawTexture(new Rect(x + w, y, 1, h), Texture2D.whiteTexture); // Right
            GUI.color = old;
        }

    }
}

