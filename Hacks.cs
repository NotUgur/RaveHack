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


        public bool noRecoil = false;
        public bool noSpread = false;
        public bool unlimitedAmmo = false;


        public Color espColor = Color.red;

        public bool antiRagdoll = false;

        public bool oneShotKill = false;


        private GUIStyle labelStyle;

        public bool godMode = false;
        public bool rapidFire = false;
        private bool showMenu = true;

        private float defaultProjectileDamage = 70f;
        private float defaultProjectileBalanceDamage = 60f;
        private float defaultProjectileGravity = 1f;
        private float defaultProjectileSpeed = 300f;
        private float defaultWeaponCooldown = 0.2f;
        private float defaultWeaponReload = 2f;

        private float fpsUpdate = 0f;
        private int fpsFrames = 0;
        private float lastFps = 0f;

        public bool enableFlyHack = false;
        public float flySpeed = 50f;

        public bool enableNoClip = false;
        public float noClipSpeed = 50f;

        void Start()
        {
            labelStyle = new GUIStyle();
            labelStyle.normal.textColor = Color.white;
            labelStyle.fontSize = 12;

            var proj = FindObjectOfType<Projectile>();
            if (proj != null)
            {
                defaultProjectileDamage = proj.configuration.damage;
                defaultProjectileBalanceDamage = proj.configuration.balanceDamage;
                defaultProjectileGravity = proj.configuration.gravityMultiplier;
                defaultProjectileSpeed = proj.configuration.speed;
            }
        }

        void Update()
        {

            fpsFrames++;
            fpsUpdate += Time.unscaledDeltaTime;
            if (fpsUpdate > 0.5f)
            {
                lastFps = fpsFrames / fpsUpdate;
                fpsFrames = 0;
                fpsUpdate = 0f;
            }


            if (Input.GetKeyDown(KeyCode.Insert))
            {
                showMenu = !showMenu;
                if (FpsActorController.instance != null)
                    FpsActorController.instance.unlockCursorRavenscriptOverride = showMenu;
                Cursor.visible = showMenu;
                Cursor.lockState = showMenu ? CursorLockMode.None : CursorLockMode.Locked;
            }

            var player = LocalPlayer.actor;

            if (enableSpeedHack && player != null && player.controller != null)
                player.speedMultiplier = InjectedSpeedMultiplier;

            if (enableHealthHack && player != null)
                player.health = InjectedHealthHack;

            if (player != null)
            {
                foreach (var col in player.GetComponentsInChildren<Collider>())
                {
                    if (godMode)
                        col.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
                    else
                        col.gameObject.layer = LayerMask.NameToLayer("Default");
                }
            }

            if (player != null && player.weapons != null)
            {
                var weapon = player.activeWeapon;
                var config = weapon.configuration;
                if (noRecoil)
                {
                    config.kickback = 0f;
                    config.randomKick = 0f;
                    config.snapMagnitude = 0f;
                }
                if (noSpread)
                {
                    config.spread = 0f;
                    config.followupSpreadGain = 0f;
                    config.followupMaxSpreadHip = 0f;
                    config.followupMaxSpreadAim = 0f;
                }
                if (unlimitedAmmo)
                {
                    config.ammo = 999;
                    config.spareAmmo = 999;
                    weapon.ammo = 999;
                    weapon.spareAmmo = 999;
                }

                if (rapidFire)
                {
                    config.cooldown = 0.00f;
                    config.reloadTime = 0.00f;
                }
                else
                {
                    config.cooldown = defaultWeaponCooldown;
                    config.reloadTime = defaultWeaponReload;
                }
            }

            if (antiRagdoll && player != null && player.ragdoll != null)
            {
                if (player.ragdoll.state == ActiveRaggy.State.Ragdoll)
                {
                    player.ragdoll.ragdollObject.SetActive(false);
                    player.ragdoll.state = ActiveRaggy.State.Animate;
                }
            }

            if (player != null && player.activeWeapon != null)
            {
                var weapon = player.activeWeapon;
                foreach (var proj in FindObjectsOfType<Projectile>())
                {
                    if (proj.sourceWeapon == weapon)
                    {
                        if (oneShotKill)
                        {
                            proj.configuration.damage = 9999f;
                            proj.configuration.balanceDamage = 9999f;
                        }
                        else
                        {
                            proj.configuration.damage = defaultProjectileDamage;
                            proj.configuration.balanceDamage = defaultProjectileBalanceDamage;
                        }
                    }
                }
            }

            if (enableFlyHack && player != null && player.controller is FpsActorController fpsFly)
            {
                Vector3 flyDir = Vector3.zero;
                if (Input.GetKey(KeyCode.W)) flyDir += fpsFly.fpCamera.transform.forward;
                if (Input.GetKey(KeyCode.S)) flyDir -= fpsFly.fpCamera.transform.forward;
                if (Input.GetKey(KeyCode.A)) flyDir -= fpsFly.fpCamera.transform.right;
                if (Input.GetKey(KeyCode.D)) flyDir += fpsFly.fpCamera.transform.right;
                if (Input.GetKey(KeyCode.Space)) flyDir += Vector3.up;
                if (Input.GetKey(KeyCode.LeftControl)) flyDir += Vector3.down;

                if (flyDir != Vector3.zero)
                {
                    flyDir.Normalize();
                    fpsFly.Move(flyDir * flySpeed * Time.deltaTime);
                }
            }

            if (enableNoClip && player != null && player.controller is FpsActorController fpsNoClip)
            {
                fpsNoClip.controller.enabled = false;

                Vector3 move = Vector3.zero;
                if (Input.GetKey(KeyCode.W)) move += fpsNoClip.fpCamera.transform.forward;
                if (Input.GetKey(KeyCode.S)) move -= fpsNoClip.fpCamera.transform.forward;
                if (Input.GetKey(KeyCode.A)) move -= fpsNoClip.fpCamera.transform.right;
                if (Input.GetKey(KeyCode.D)) move += fpsNoClip.fpCamera.transform.right;
                if (Input.GetKey(KeyCode.Space)) move += Vector3.up;
                if (Input.GetKey(KeyCode.LeftControl)) move += Vector3.down;

                if (move != Vector3.zero)
                {
                    move.Normalize();
                    fpsNoClip.transform.position += move * noClipSpeed * Time.deltaTime;
                }
            }
            else if (player != null && player.controller is FpsActorController fpsNoClipRestore)
            {
                if (!fpsNoClipRestore.controller.enabled)
                    fpsNoClipRestore.controller.enabled = true;
            }
        }

        void OnGUI()
        {
            GUIStyle watermarkStyle = new GUIStyle(GUI.skin.label);
            watermarkStyle.fontSize = 18;
            watermarkStyle.fontStyle = FontStyle.Bold;
            watermarkStyle.normal.textColor = Color.white;
            watermarkStyle.alignment = TextAnchor.UpperCenter;
            GUI.Label(new Rect(Screen.width / 2 - 200, 10, 400, 30), $"RaveHack v1 Menu: INSERT   FPS: {(int)lastFps}", watermarkStyle);

            if (showMenu)
            {
                DrawGUI();
                GUILayout.EndScrollView();
            }

            DrawESP();
        }

        void DrawGUI()
        {
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = MakeTex(2, 2, new Color(0.13f, 0.13f, 0.13f, 0.95f));
            boxStyle.border = new RectOffset(10, 10, 10, 10);
            boxStyle.padding = new RectOffset(12, 12, 12, 12);

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 20;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.normal.textColor = new Color(0.9f, 0.3f, 0.3f);
            titleStyle.alignment = TextAnchor.MiddleCenter;

            GUIStyle sectionStyle = new GUIStyle(GUI.skin.label);
            sectionStyle.fontSize = 14;
            sectionStyle.fontStyle = FontStyle.Bold;
            sectionStyle.normal.textColor = new Color(0.7f, 0.7f, 1f);
            sectionStyle.margin = new RectOffset(0, 0, 8, 4);

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 13;
            buttonStyle.fixedHeight = 28;
            buttonStyle.margin = new RectOffset(0, 0, 2, 2);

            GUILayout.BeginArea(new Rect(10, 10, 320, 650), boxStyle);
            GUILayout.Label("RaveHack", titleStyle);
            GUILayout.Space(8);

            GUILayout.Label("General", sectionStyle);
            enableESP = GUILayout.Toggle(enableESP, "Enable ESP");
            enableSpeedHack = GUILayout.Toggle(enableSpeedHack, "Speed Hack");
            if (enableSpeedHack)
            {
                InjectedSpeedMultiplier = GUILayout.HorizontalSlider(InjectedSpeedMultiplier, 1f, 10f);
                GUILayout.Label($"Speed Multiplier: {InjectedSpeedMultiplier:F1}");
            }
            enableHealthHack = GUILayout.Toggle(enableHealthHack, "Infinite Health");
            antiRagdoll = GUILayout.Toggle(antiRagdoll, "Anti-Ragdoll");
            godMode = GUILayout.Toggle(godMode, "God Mode");
            oneShotKill = GUILayout.Toggle(oneShotKill, "One Shot Kill");
            unlimitedAmmo = GUILayout.Toggle(unlimitedAmmo, "Unlimited Ammo");
            noRecoil = GUILayout.Toggle(noRecoil, "No Recoil");
            noSpread = GUILayout.Toggle(noSpread, "No Spread");
            rapidFire = GUILayout.Toggle(rapidFire, "Rapid Fire");
            enableFlyHack = GUILayout.Toggle(enableFlyHack, "Fly");
            if (enableFlyHack)
            {
                flySpeed = GUILayout.HorizontalSlider(flySpeed, 5f, 100f);
                GUILayout.Label($"Fly Speed: {flySpeed:F1}");
            }
            enableNoClip = GUILayout.Toggle(enableNoClip, "No-Clip");

            GUILayout.Space(8);
            GUILayout.Label("ESP Settings", sectionStyle);
            showBoxESP = GUILayout.Toggle(showBoxESP, "Box ESP");
            showNameESP = GUILayout.Toggle(showNameESP, "Name ESP");
            showDistanceESP = GUILayout.Toggle(showDistanceESP, "Distance ESP");
            teamCheck = GUILayout.Toggle(teamCheck, "Team Check");
            GUILayout.Label("ESP Color:");
            espColor = RGBSlider(espColor);
            maxDistance = GUILayout.HorizontalSlider(maxDistance, 50f, 3000f);
            GUILayout.Label($"ESP Distance: {Mathf.RoundToInt(maxDistance)}m");


            GUILayout.EndArea();
        }



        Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i)
                pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
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
